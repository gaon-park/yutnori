using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPun
{
    public static LobbyManager instance { get; private set; }

    public TMP_InputField inputField;
    public Transform LobbyScrollContent;

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
        PhotonNetwork.IsMessageQueueRunning = true;
    }

    [PunRPC]
    public void UpdatePlayerCount(string roomName, int count)
    {
        if (LobbyScrollContent == null) return;
        foreach (TMP_Text txt in LobbyScrollContent.Find(roomName).GetComponentsInChildren<TMP_Text>())
        {
            if (txt.name.Equals(roomName)) continue;
            txt.text = "(" + count + "/" + MAX_PLAYER + ")";
        }

        LobbyScrollContent.Find(roomName).GetComponentInChildren<Button>().enabled = count < MAX_PLAYER;
    }

    public void OnClickCreate()
    {
        if (inputField.text.Length == 0) return;
        NetworkManager.instance.JoinOrCreateRoom(inputField.text);
        inputField.text = "";
    }

    public void OnClickJoin()
    {
        NetworkManager.instance.JoinOrCreateRoom(EventSystem.current.currentSelectedGameObject.name);
    }
}
