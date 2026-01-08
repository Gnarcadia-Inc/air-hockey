using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLiftClientStarter : MonoBehaviour
{
    public GameObject gameLiftClient;

    void Awake()
    {
        GameLiftClient existingClient = FindObjectOfType<GameLiftClient>();

        if (existingClient != null)
        {
            Destroy(existingClient.gameObject);
        }

        Instantiate(gameLiftClient, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
    }
}
