using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<Transform> spawnPoints;

    private static GameManager instance;

    public static GameManager Instance => instance;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public Vector3 GetRandomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Count)].position;
    }
}