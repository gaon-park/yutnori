using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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
    
    #region 기록
    public List<Button> history = new();
    public List<TMP_Text> historyTxt = new();
    public List<TMP_Text> historyCount = new();
    #endregion

    private System.Random random = new();
    private string roomName;
    private int localPlayerIdx;
    private Dictionary<int, bool> isReady = new();

    private static readonly float THROW_FIELD_TIME_WAIT = 2.0f;
    private static readonly float TXT_TIME_WAIT = 3.0f;
    private static readonly int MAX_MARKER_COUNT = 4;
    private static readonly int minYut = -1;
    private static readonly int maxYut = 5;

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

        photonView.RPC("SetPlayerActive", RpcTarget.OthersBuffered, roomName, localPlayerIdx);
        SetPlayerActive(roomName, localPlayerIdx);

        // 준비 버튼 설정
        Button readyButton = startField.GetComponentInChildren<Button>();
        TMP_Text txt = readyButton.GetComponentInChildren<TMP_Text>();
        // 유저가 방의 마스터인 경우
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            txt.text = "시작";
        }
        // 참가자인 경우
        else
        {
            readyButton.interactable = true;
            txt.text = "준비";
        }
    }

    public void OnClickReady()
    {
        // 유저가 방의 마스터인 경우
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            if (isReady.Count == PhotonNetwork.CurrentRoom.PlayerCount - 1) 
            {
                photonView.RPC("StartGame", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, null);
            }
        }
        // 참가자인 경우
        else
        {
            bool ready = !isReady.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber);
            string txt = ready ? "준비" : "";
            photonView.RPC("StartGame", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, ready);
            photonView.RPC("SetInfo", RpcTarget.All, localPlayerIdx, txt);
        }
    }

    [PunRPC]
    public void StartGame(int actorNumber, bool? start)
    {
        if (actorNumber == PhotonNetwork.MasterClient.ActorNumber)
        {
            isStarted = true;
            startField.SetActive(false);
            playingField.SetActive(true);
            foreach (var p in players)
            {
                if (!p.activeSelf) continue;
                p.transform.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text = "x4";
            }
            turn = 0; // 0번 부터 시작
        }
        else
        {
            if (start == true)
                isReady.Add(actorNumber, true);
            else
                isReady.Remove(actorNumber);
        }
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

        // 방장 시작 버튼 활성/비활성
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            startField.GetComponentInChildren<Button>().interactable = PhotonNetwork.CurrentRoom.PlayerCount > 1 && isReady.Count + 1 == PhotonNetwork.CurrentRoom.PlayerCount;
        }

        // 게임 진행
        if (isStarted)
        {
            throwButton.interactable = turn == localPlayerIdx;


        }
    }

    public void OnClickThrowButton()
    {
        throwButton.interactable = false;
        int randomNumber = random.Next(minYut, maxYut + 1);
        photonView.RPC("RPCThrowYut", RpcTarget.All, randomNumber);
        
    }

    [PunRPC]
    public void RPCThrowYut(int yutNum)
    {
        Yut yut = (Yut)Enum.Parse(typeof(Yut), yutNum.ToString());
        throwField.SetActive(true);
        throwField.GetComponentInChildren<TMP_Text>().text = GetEnumDescription(yut);
        StartCoroutine(TimerCoroutine(false));

    }

    public void SendButtonOnClicked()
    {
        if (inputField.text.Equals(""))
            return;

        string msg = inputField.text;
        photonView.RPC("ReceiveMsg", RpcTarget.All, localPlayerIdx, msg);
        inputField.ActivateInputField();
        inputField.text = "";

        StartCoroutine(TimerCoroutine(true));
    }

    public IEnumerator TimerCoroutine(bool isChat)
    {
        if (isChat)
        {
            yield return new WaitForSeconds(TXT_TIME_WAIT);
            photonView.RPC("DestroyMsg", RpcTarget.All, localPlayerIdx);
        }
        else
        {
            yield return new WaitForSeconds(THROW_FIELD_TIME_WAIT);
            throwField.SetActive(false);
            throwField.GetComponentInChildren<TMP_Text>().text = "";
        }
    }

    [PunRPC]
    private void SetInfo(int idx, string info)
    {
        players[idx].transform.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text = info;
    }

    [PunRPC]
    private void ReceiveMsg(int idx, string msg)
    {
        playerChatTxt[idx].text = msg;
        playerChatTxt[idx].transform.parent.gameObject.SetActive(true);
    }

    [PunRPC]
    private void DestroyMsg(int idx)
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

    static T GetEnumValueFromDescription<T>(string description) where T : Enum
    {
        foreach (FieldInfo field in typeof(T).GetFields())
        {
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                if (attribute.Description == description)
                    return (T)field.GetValue(null);
            }
            else
            {
                if (field.Name == description)
                    return (T)field.GetValue(null);
            }
        }

        throw new ArgumentException($"No enum value found for description {description}", nameof(description));
    }
}
