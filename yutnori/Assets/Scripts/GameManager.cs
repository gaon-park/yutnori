using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum Yut
{
    [Description("�޵�")]
    BackDo = -1,
    [Description("��")]
    Nack = 0,
    [Description("��")]
    Do = 1,
    [Description("��")]
    Gae = 2,
    [Description("��")]
    Gul = 3,
    [Description("��")]
    Yut = 4,
    [Description("��")]
    Mo = 5
}

class UserPlay
{
    public List<GameObject> markers; // ��ü �� ������Ʈ
    public List<bool> goalFlags; // ���� �����ߴ°�?
    public List<Yut> currentYuts; // �̹� �Ͽ� ������ �� �ִ� �� ���

    public TMP_Text info;
    public int actorNumber;
    public bool isReady = false;
    public TMP_Text readyTxt;

    public UserPlay(
        List<GameObject> markers, 
        List<bool> goalFlags, 
        List<Yut> currentYuts, 
        TMP_Text info, 
        int actorNumber)
    {
        this.markers = markers;
        this.goalFlags = goalFlags;
        this.currentYuts = currentYuts;
        this.info = info;
        this.actorNumber = actorNumber;
    }
}

public class GameManager : MonoBehaviourPun
{
    public static GameManager instance { get; private set; }

    public List<GameObject> players = new();
    public List<TMP_Text> playerChatTxt = new();
    public TMP_InputField inputField;

    public GameObject playingField;
    public GameObject startField;
    public GameObject throwField;
    public Button throwButton;

    private System.Random random = new();
    private string roomName;
    private int localPlayerIdx;
    private static readonly float TXT_TIME_WAIT = 3.0f;
    private static readonly int MAX_MARKER_COUNT = 4;
    private static readonly int minYut = -1;
    private static readonly int maxYut = 5;

    private List<UserPlay> userPlays = new();
    private int turn = -1; // ���� ���� playerIdx
    private bool isStarted; // ���� ����

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        roomName = PhotonNetwork.CurrentRoom.Name;
        localPlayerIdx = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        photonView.RPC("SetPlayerActive", RpcTarget.OthersBuffered, roomName, localPlayerIdx);
        SetPlayerActive(roomName, localPlayerIdx);

        throwButton.onClick.AddListener(ThrowYut);

        foreach (var player in players)
        {
            List<GameObject> markers = new();
            TMP_Text info = null;
            foreach (var img in player.GetComponentsInChildren<Image>())
            {
                if (img.gameObject.name.StartsWith("marker"))
                {
                    markers.Add(img.gameObject);
                }
                else if (img.gameObject.name.Equals("player icon"))
                {
                    Debug.Log("11");
                    info = img.gameObject.transform.Find("info").GetComponent<TMP_Text>();
                }
            }
            userPlays.Add(new(markers, new() { false, false, false, false }, new(), info, PhotonNetwork.LocalPlayer.ActorNumber));
        }

        // �غ� ��ư ����
        Button readyButton = startField.GetComponentInChildren<Button>();
        TMP_Text txt = readyButton.GetComponentInChildren<TMP_Text>();
        // ������ ���� �������� ���
        if (PhotonNetwork.MasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            txt.text = "����";
        }
        // �������� ���
        else
        {
            readyButton.enabled = true;
            txt.text = "�غ�";
        }
        userPlays[localPlayerIdx].readyTxt = txt;
    }

    public void OnClickReady()
    {
        // ������ ���� �������� ���
        if (PhotonNetwork.MasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            bool isAllReady = PhotonNetwork.CurrentRoom.PlayerCount > 1;
            foreach (var user in userPlays)
            {
                if (user.actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) continue;
                if (!user.isReady)
                {
                    isAllReady = false;
                    break;
                }
            }
            if (isAllReady)
            {
                photonView.RPC("StartGame", RpcTarget.OthersBuffered);
                StartGame();
            }
        }
        // �������� ���
        else
        {
            bool changeTo = !userPlays[localPlayerIdx].isReady;
            photonView.RPC("SetPlayerReadyStatus", RpcTarget.OthersBuffered, localPlayerIdx, changeTo);
            SetPlayerReadyStatus(localPlayerIdx, changeTo);
            userPlays[localPlayerIdx].readyTxt.text = userPlays[localPlayerIdx].isReady ? "�غ� �Ϸ�" : "�غ�";
        }
    }

    [PunRPC]
    public void SetPlayerReadyStatus(int playerIdx, bool readyStatus)
    {
        userPlays[playerIdx].isReady = readyStatus;
        userPlays[playerIdx].info.text = readyStatus ? "�غ�" : "";
    }

    [PunRPC]
    public void StartGame()
    {
        isStarted = true;
        startField.SetActive(false);
        playingField.SetActive(true);
        foreach (var user in userPlays)
        {
            user.info.text = "x4";
        }
        turn = 0; // 0�� ���� ����
    }

    [PunRPC]
    void SetPlayerActive(string room, int playerIdx)
    {
        if (!roomName.Equals(room)) return;
        players[playerIdx].SetActive(true);
    }

    void Update()
    {
        if (!inputField.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            SendButtonOnClicked();
        
        // ���� ����
        if (isStarted)
        {
            throwButton.enabled = turn == localPlayerIdx;
        }
    }

    public void ThrowYut()
    {
        photonView.RPC("RPCThrowYut", RpcTarget.OthersBuffered);
        RPCThrowYut();
    }

    public void RPCThrowYut()
    {
        int randomNumber = random.Next(minYut, maxYut + 1);
        Yut yut = (Yut)Enum.Parse(typeof(Yut), randomNumber.ToString());
        throwField.SetActive(true);
        throwField.GetComponentInChildren<TMP_Text>().text = GetEnumDescription(yut);
    }

    public void SendButtonOnClicked()
    {
        if (inputField.text.Equals(""))
            return;

        string msg = inputField.text;
        photonView.RPC("ReceiveMsg", RpcTarget.OthersBuffered, localPlayerIdx, msg);
        ReceiveMsg(localPlayerIdx, msg);
        inputField.ActivateInputField();
        inputField.text = "";

        StartCoroutine(TxtTimerCoroutine());
    }

    public IEnumerator TxtTimerCoroutine()
    {
        yield return new WaitForSeconds(TXT_TIME_WAIT);
        DestroyMsg(localPlayerIdx);
        photonView.RPC("DestroyMsg", RpcTarget.OthersBuffered, localPlayerIdx);
    }

    [PunRPC]
    public void ReceiveMsg(int idx, string msg)
    {
        playerChatTxt[idx].text = msg;
        playerChatTxt[idx].transform.parent.gameObject.SetActive(true);
    }

    [PunRPC]
    public void DestroyMsg(int idx)
    {
        playerChatTxt[idx].text = "";
        playerChatTxt[idx].transform.parent.gameObject.SetActive(false);
    }

    static string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute != null ? attribute.Description : value.ToString();
    }
}
