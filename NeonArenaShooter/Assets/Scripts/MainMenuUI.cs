using System.Collections;
using System.Collections.Generic;
using kcp2k;
using Mirror;
using TMPro;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    private static MainMenuUI instance = null;

    public static MainMenuUI Instance => instance;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject joinCustomMenu;
    [SerializeField] private GameObject hostMenu;

    [SerializeField] private TMP_InputField ipInput, portInput;
    [SerializeField] private TMP_InputField hostPortInput;
    [SerializeField] private string playerName = "Player";

    [Space]
    [SerializeField] private GameObject currentMenu;

    public string PlayerName
    {
        get => playerName;
        set => playerName = value;
    }

    private void Awake()
    {
        if (!instance)
            instance = this;

        currentMenu = mainMenu;
    }

    public void CloseGame()
    {
        Application.Quit();
    }

    public void ShowMainMenu()
    {
        currentMenu.SetActive(false);
        currentMenu = mainMenu;
        currentMenu.SetActive(true);
    }
    
    public void ShowJoinCustomMenu()
    {
        currentMenu.SetActive(false);
        currentMenu = joinCustomMenu;
        currentMenu.SetActive(true);
    }
    
    public void ShowHostMenu()
    {
        currentMenu.SetActive(false);
        currentMenu = hostMenu;
        currentMenu.SetActive(true);
    }

    public void JoinCustomServer()
    {
        NetworkManager.singleton.networkAddress = ipInput.text;
        if (ushort.TryParse(portInput.text, out ushort val))
        {
            (Transport.activeTransport as KcpTransport).Port = val;
        }
        else
        {
            Debug.Log("Invalid port!");
            return;
        }

        PlayerController.insertedName = playerName;
        NetworkManager.singleton.StartClient();
    }

    public void HostCustomServer()
    {
        if (ushort.TryParse(hostPortInput.text, out ushort val))
        {
            (Transport.activeTransport as KcpTransport).Port = val;
        }
        else
        {
            Debug.Log("Invalid port!");
            return;
        }
        
        PlayerController.insertedName = playerName;
        NetworkManager.singleton.StartHost();
    }
}
