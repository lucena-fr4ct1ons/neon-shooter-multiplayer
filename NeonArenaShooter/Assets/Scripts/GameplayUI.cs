using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : MonoBehaviour
{
    private static GameplayUI instance = null;

    public static GameplayUI Instance => instance;

    [SerializeField] private GameObject koVisual;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TextMeshProUGUI latencyDisplay;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private GameObject eliminationTextPrefab;
    [SerializeField] private Transform eliminationList;
    [SerializeField] private Resolution[] resolutions;
    
    [SerializeField] private Toggle fullscreenState;
    [SerializeField] private TMP_Dropdown resolutionList;

    private void Awake()
    {
        if (!instance)
            instance = this;
        
        fullscreenState.SetIsOnWithoutNotify(Screen.fullScreen);

        resolutions = Screen.resolutions;
        resolutionList.ClearOptions();
        List<string> temp = new List<string>();
        int currentResolution = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            temp.Add(resolutions[i].ToString());

            if (Screen.currentResolution.Equals(resolutions[i]))
            {
                currentResolution = i;
            }
        }
        
        resolutionList.AddOptions(temp);
        resolutionList.SetValueWithoutNotify(currentResolution);
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

    public bool ShowPauseMenu()
    {
        pausePanel.SetActive(!pausePanel.activeSelf);
        return pausePanel.activeSelf;
    }

    public void SetFullscreen(bool state)
    {
        Screen.fullScreen = state;
    }

    public void SetResolution(int option)
    {
        Screen.SetResolution(resolutions[option].width, resolutions[option].height, Screen.fullScreen);
    }

    public void UpdateHealth(int val)
    {
        healthText.SetText($"{Mathf.Clamp(val, 0, 9999)}");
    }
}
