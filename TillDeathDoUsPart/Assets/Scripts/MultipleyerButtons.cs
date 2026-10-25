using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button clientButton;

    void Awake()
    {
        hostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); Hide(); });
        clientButton.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); Hide(); });
    }

    void Hide() => gameObject.SetActive(false);
}