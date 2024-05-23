using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager instance { get; private set; }

    public GameObject roomTemplate;
    public Transform LobbyScrollContent;

    private Dictionary<string, TMP_Text> roomCountTextMap = new();
    private Dictionary<string, int> roomCountMap = new();
    private Dictionary<string, Button> roomButtonMap = new();
    private static int MAX_PLAYER = 4;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.IsMessageQueueRunning = true;
    }

    public override void OnConnected()
    {
        base.OnConnected();
        print(System.Reflection.MethodBase.GetCurrentMethod().Name);
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        print(System.Reflection.MethodBase.GetCurrentMethod().Name);

        // 로비 진입 요청
        StartCoroutine(WaitAndJoinLobby());
    }

    private IEnumerator WaitAndJoinLobby()
    {
        while (!PhotonNetwork.IsConnectedAndReady)
        {
            yield return null;
        }
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        print(System.Reflection.MethodBase.GetCurrentMethod().Name);
        //JoinOrCreateRoom();
    }

    public void JoinOrCreateRoom(string roomName)
    {
        RoomOptions roomOptions = new()
        {
            MaxPlayers = MAX_PLAYER,
            IsVisible = true // 공개방
        };

        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, null);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        print("OnRoomListUpdate");

        foreach (RoomInfo roomInfo in roomList)
        {
            if (!roomCountMap.ContainsKey(roomInfo.Name) || !roomCountTextMap.ContainsKey(roomInfo.Name) || !roomButtonMap.ContainsKey(roomInfo.Name)) 
            {
                GameObject room = Instantiate(roomTemplate, LobbyScrollContent);

                room.name = roomInfo.Name;
                TMP_Text count = room.transform.Find("count").GetComponent<TMP_Text>();
                Button button = room.transform.Find("joinButton").GetComponent<Button>();

                room.transform.Find("room name").GetComponent<TMP_Text>().text = roomInfo.Name;
                count.text = "(" + roomInfo.PlayerCount + "/" + MAX_PLAYER + ")";
                button.name = roomInfo.Name;

                roomCountMap.Add(roomInfo.Name, roomInfo.PlayerCount);
                roomCountTextMap.Add(roomInfo.Name, count);
                roomButtonMap.Add(roomInfo.Name, button);
                room.SetActive(true);
            }
            else if (roomCountMap[roomInfo.Name] != roomInfo.PlayerCount)
            {
                roomCountMap[roomInfo.Name] = roomInfo.PlayerCount;
                roomCountTextMap[roomInfo.Name].text = "(" + roomInfo.PlayerCount + "/" + MAX_PLAYER + ")";
            }

            roomButtonMap[roomInfo.Name].enabled = roomInfo.PlayerCount < MAX_PLAYER;
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        print("OnJoinRoom");
        LobbyManager.instance.photonView.RPC("UpdatePlayerCount", RpcTarget.OthersBuffered, PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CurrentRoom.PlayerCount);
        PhotonNetwork.LoadLevel("play");
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        print("OnCreatedRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        print("OnCreatedRoomFailed, " + returnCode + ", " + message);
    }
}