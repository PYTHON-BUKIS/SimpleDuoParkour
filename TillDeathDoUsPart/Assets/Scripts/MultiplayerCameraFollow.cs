using Unity.Netcode;
using UnityEngine;

public class CameraFollow2D : NetworkBehaviour
{
    private Transform target;

    void LateUpdate()
    {
        if (target == null)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

            foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                if (netObj != null && netObj.IsOwner && netObj.CompareTag("Player"))
                {
                    target = netObj.transform;
                    break;
                }
            }
            return;
        }

        transform.position = new Vector3(target.position.x, target.position.y, -10f);
    }
}
