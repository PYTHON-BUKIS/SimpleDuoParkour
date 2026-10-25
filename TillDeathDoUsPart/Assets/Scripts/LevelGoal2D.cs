using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LevelGoal2D : NetworkBehaviour
{
    [SerializeField] private string levelCompleteMessage = "Level Complete!";
    private bool isCompleted = false;
    private HashSet<ulong> playersInGoal = new HashSet<ulong>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || isCompleted) return;

        if (collision.CompareTag("Player"))
        {
            NetworkObject netObj = collision.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                playersInGoal.Add(netObj.OwnerClientId);
                CheckLevelCompletion();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsServer || isCompleted) return;

        if (collision.CompareTag("Player"))
        {
            NetworkObject netObj = collision.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                playersInGoal.Remove(netObj.OwnerClientId);
            }
        }
    }

    private void CheckLevelCompletion()
    {
        int totalPlayers = 0;
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (netObj != null && netObj.CompareTag("Player"))
            {
                totalPlayers++;
            }
        }


        if (totalPlayers >= 2 && playersInGoal.Count >= totalPlayers)
        {
            isCompleted = true;
            TriggerLevelCompleteClientRpc(levelCompleteMessage);
        }
    }

    [ClientRpc]
    private void TriggerLevelCompleteClientRpc(string message)
    {
        Debug.Log(message);
    }
}
