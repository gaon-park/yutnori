using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPun
{
    public static GameManager instance { get; private set; }
    
    public List<GameObject> players = new();
    public List<TMP_Text> playerChatTxt = new();
    public TMP_InputField inputField;

    private string roomName;
    private int localPlayerIdx;
    private static readonly float txtWaitTime = 3.0f;

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
        yield return new WaitForSeconds(txtWaitTime);
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
}
