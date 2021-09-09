using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    private static GameplayUI instance = null;

    public static GameplayUI Instance => instance;

    [SerializeField] private GameObject koVisual;
    [SerializeField] private TextMeshProUGUI latencyDisplay;
    [SerializeField] private GameObject eliminationTextPrefab;
    [SerializeField] private Transform eliminationList;

    private void Awake()
    {
        if (!instance)
            instance = this;
    }

    public void DisplayKOVisual(bool state)
    {
        koVisual.SetActive(state);
    }

    private void Update()
    {
        latencyDisplay.SetText($"Ping: {Mathf.FloorToInt(((float)NetworkTime.rtt) * 1000.0f)}ms");
    }

    public void DrawElimination(string eliminater, string eliminated)
    {
        Instantiate(eliminationTextPrefab, eliminationList).GetComponent<TextMeshProUGUI>().
            SetText($"{eliminater} <color=yellow>></color> {eliminated}");
    }
}
