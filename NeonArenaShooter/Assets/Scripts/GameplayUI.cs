using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] private GameObject koVisual;

    public void DisplayKOVisual(bool state)
    {
        koVisual.SetActive(state);
    }
}
