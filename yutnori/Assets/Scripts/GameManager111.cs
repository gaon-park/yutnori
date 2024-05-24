using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

class Player
{
    public bool isThrowButtonClicked;
    public bool success;
    public List<int> points; // 유저가 움직일 말
    public List<Yut> curThrowHistory;

    public Player()
    {
        isThrowButtonClicked = false;
        success = false;
        points = new();
        curThrowHistory = new();
    }
}

public class GameManager111 : MonoBehaviour
{
    public GameObject field;
    public GameObject playing;
    public GameObject throwField;

    private System.Random random = new();
    private int turn = 0;
    private Player[] players = { new(), new() };
    private TMP_Text txt;

    private static readonly int minYut = -1;
    private static readonly int maxYut = 5;

    public void OnClickThrow()
    {
        players[turn].isThrowButtonClicked = true;
    }

    void Start()
    {
        Screen.SetResolution(1920, 1080, false);

        foreach (Transform child in throwField.transform)
        {
            TMP_Text txt = child.GetComponent<TMP_Text>();
            if (txt == null) continue;
            this.txt = txt;
        }
    }

    private void Awake()
    {
        Screen.SetResolution(1080, 1920, true);
        
    }

    IEnumerator ThrowYut()
    {
        while (!players[turn].isThrowButtonClicked) yield return null;

        int randomNumber = random.Next(minYut, maxYut + 1);
        Yut yut = (Yut)Enum.Parse(typeof(Yut), randomNumber.ToString());
        
        //throwField.SetActive(true);
        //txt.text = GetEnumDescription(yut);
        
        if (yut.Equals(Yut.Nack)) yield break; // 낙
        if (players[turn].points.Count == 0 && yut.Equals(Yut.BackDo)) yield break; // 필드에 있는 말이 없는데 뒷도 나왔을 때
        
        players[turn].curThrowHistory.Add(yut);
        players[turn].isThrowButtonClicked = false;
        players[turn].success = true;

        if (yut.Equals(Yut.Yut) || yut.Equals(Yut.Mo)) // 한 번 더
        {
            yield return null;
        }
    }

    void Update()
    {
        StartCoroutine(ThrowYut());
        if (players[turn].success)
        {
            Debug.Log("success: " + players[turn].success);
            throwField.SetActive(true);
            txt.text = GetEnumDescription(players[turn].curThrowHistory[^1]);
            
            //throwField.SetActive(false);
        }
        
    }

    static string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute != null ? attribute.Description : value.ToString();
    }
}
