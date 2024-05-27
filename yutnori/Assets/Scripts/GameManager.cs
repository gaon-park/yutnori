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
    [Description("뒷도")]
    BackDo = -1,
    [Description("낙")]
    Nack = 0,
    [Description("도")]
    Do = 1,
    [Description("개")]
    Gae = 2,
    [Description("걸")]
    Gul = 3,
    [Description("윷")]
    Yut = 4,
    [Description("모")]
    Mo = 5
}

class UserPlay
{
    public List<GameObject> markers; // 전체 말 오브젝트
    public List<bool> goalFlags; // 말이 골인했는가?
    public List<Yut> currentYuts; // 이번 턴에 움직일 수 있는 윷 결과

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

public class GameManager : MonoBehaviourPunCallbacks
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

    private Dictionary<int, UserPlay> userPlays = new();
    private int turn = -1; // 현재 턴의 playerIdx
    private bool isStarted; // 게임 시작

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

        throwButton.onClick.AddListener(ThrowYut);

        photonView.RPC("SetPlayerActive", RpcTarget.OthersBuffered, roomName, localPlayerIdx);
        SetPlayerActive(roomName, localPlayerIdx);
    }

    public void OnClickReady()
    {
        // 유저가 방의 마스터인 경우
        if (PhotonNetwork.MasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount < 2) return;
            foreach (var player in PhotonNetwork.CurrentRoom.Players)
            {
                //if (!players[player.Value.ActorNumber].activeSelf)
                //    continue;
                if (player.Value.ActorNumber == PhotonNetwork.MasterClient.ActorNumber)
                    continue;
                if (!userPlays[player.Value.ActorNumber - 1].isReady) return;
                Debug.Log("user " + player.Value.ActorNumber + ": " + userPlays[player.Value.ActorNumber - 1].isReady);
            }

            photonView.RPC("StartGame", RpcTarget.OthersBuffered);
            StartGame();
        }
        // 참가자인 경우
        else
        {
            bool changeTo = !userPlays[localPlayerIdx].isReady;
            photonView.RPC("SetPlayerReadyStatus", RpcTarget.OthersBuffered, localPlayerIdx, changeTo);
            SetPlayerReadyStatus(localPlayerIdx, changeTo);
            userPlays[localPlayerIdx].readyTxt.text = userPlays[localPlayerIdx].isReady ? "준비 완료" : "준비";
        }
    }

    [PunRPC]
    public void SetPlayerReadyStatus(int playerIdx, bool readyStatus)
    {
        userPlays[playerIdx].isReady = readyStatus;
        userPlays[playerIdx].info.text = readyStatus ? "준비" : "";
    }

    [PunRPC]
    public void StartGame()
    {
        isStarted = true;
        startField.SetActive(false);
        playingField.SetActive(true);
        foreach (var user in userPlays)
        {
            user.Value.info.text = "x4";
        }
        turn = 0; // 0번 부터 시작
    }

    [PunRPC]
    void SetPlayerActive(string room, int playerIdx)
    {
        if (!roomName.Equals(room)) return;
        players[playerIdx].SetActive(true);

        if (userPlays.ContainsKey(playerIdx)) return;

        List<GameObject> markers = new();
        TMP_Text info = null;
        foreach (var img in players[playerIdx].GetComponentsInChildren<Image>())
        {
            if (img.gameObject.name.StartsWith("marker"))
            {
                markers.Add(img.gameObject);
            }
            else if (img.gameObject.name.Equals("player icon"))
            {
                info = img.gameObject.transform.Find("info").GetComponent<TMP_Text>();
            }
        }

        // 준비 버튼 설정
        Button readyButton = startField.GetComponentInChildren<Button>();
        TMP_Text txt = readyButton.GetComponentInChildren<TMP_Text>();
        // 유저가 방의 마스터인 경우
        if (PhotonNetwork.MasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            txt.text = "시작";
        }
        // 참가자인 경우
        else
        {
            readyButton.enabled = true;
            txt.text = "준비";
        }

        userPlays.Add(playerIdx, new(markers, new() { false, false, false, false }, new() { }, info, playerIdx + 1));
        userPlays[playerIdx].readyTxt = txt;
    }

    void Update()
    {
        if (!inputField.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            SendButtonOnClicked();

        // 게임 진행
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
