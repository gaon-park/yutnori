using Photon.Pun;
using Photon.Realtime;
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
    
    private Dictionary<int, int> activeToPlayerIdx = new();

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

        // 캐릭터들 보여주기
        Debug.Log("count: " + PhotonNetwork.PlayerList.Length);
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            AddPlayer(p);
        }
        photonView.RPC("AddPlayer", RpcTarget.OthersBuffered, PhotonNetwork.LocalPlayer);
    }

    [PunRPC]
    void AddPlayer(Photon.Realtime.Player player)
    {
        int idx = activeToPlayerIdx.Count;
        players[idx].SetActive(true);
        activeToPlayerIdx.Add(player.ActorNumber, idx);
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
        photonView.RPC("ReceiveMsg", RpcTarget.OthersBuffered, PhotonNetwork.LocalPlayer.ActorNumber, msg);
        ReceiveMsg(activeToPlayerIdx[PhotonNetwork.LocalPlayer.ActorNumber], msg);
        inputField.ActivateInputField();
        inputField.text = "";
    }

    [PunRPC]
    public void ReceiveMsg(int idx, string msg)
    {
        playerChatTxt[idx].text = msg;
    }
}
