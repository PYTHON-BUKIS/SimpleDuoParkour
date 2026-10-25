using UnityEngine;
using Unity.Netcode;

public class KillPlane2D : NetworkBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float spawnOffsetY = 3f;
    private bool isResetting = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || isResetting) return;

        if (collision.CompareTag("Player"))
        {
            isResetting = true;

            Vector3 targetPos;
            if (respawnPoint != null)
            {
                targetPos = respawnPoint.position;
            }
            else
            {
                targetPos = new Vector3(transform.position.x, transform.position.y + spawnOffsetY, 0f);
            }

            ResetAllPlayersClientRpc(targetPos);

            VerletRope2D rope = FindFirstObjectByType<VerletRope2D>(); if (rope != null)
            {
                rope.ResetRope();
            }

            Invoke(nameof(ResetCooldown), 0.1f);
        }
    }

    [ClientRpc]
    private void ResetAllPlayersClientRpc(Vector3 targetPos)
    {
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (netObj != null && netObj.CompareTag("Player"))
            {
                Rigidbody2D rb = netObj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                netObj.transform.position = targetPos;
            }
        }
    }

    private void ResetCooldown()
    {
        isResetting = false;
    }
}