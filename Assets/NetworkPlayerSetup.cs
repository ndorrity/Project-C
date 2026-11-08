using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [SerializeField] private GameObject cameraContainer;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private MonoBehaviour movementScript; // Drag the asset's movement script here

    public override void OnNetworkSpawn()
    {
        // If this is not the local player, disable camera, audio, and controls
        if (!IsOwner)
        {
            if (cameraContainer != null) cameraContainer.SetActive(false);
            if (audioListener != null) audioListener.enabled = false;
            if (movementScript != null) movementScript.enabled = false;
        }
    }
}
