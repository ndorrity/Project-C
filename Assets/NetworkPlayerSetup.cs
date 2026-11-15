using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("Local Disable Objects (Remote Players)")]
    [SerializeField] private GameObject cameraContainer;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private MonoBehaviour movementScript;

    [Header("Character Visuals")]
    [SerializeField] private GameObject characterModel;

    private CharacterController characterController;
    private Camera playerCamera;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (cameraContainer != null)
        {
            playerCamera = cameraContainer.GetComponentInChildren<Camera>(true);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (cameraContainer != null) cameraContainer.SetActive(true);
            if (playerCamera != null) playerCamera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;
            if (movementScript != null) movementScript.enabled = true;

            // Turn all meshes (Body, Hat, Mask) into shadows-only for local camera
            if (characterModel != null)
            {
                Renderer[] allRenderers = characterModel.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer rend in allRenderers)
                {
                    rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }

            // Delay gravity/controller slightly so scene and spawn sync first
            StartCoroutine(EnableControllerAfterSpawn());
        }
        else
        {
            // Remote player: keep cameraContainer active so the weapon mesh stays visible,
            // but disable the Camera component, AudioListener, movement, and CharacterController
            if (cameraContainer != null) cameraContainer.SetActive(true);
            if (playerCamera != null) playerCamera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            if (movementScript != null) movementScript.enabled = false;
            if (characterController != null) characterController.enabled = false;
        }
    }

    private IEnumerator EnableControllerAfterSpawn()
    {
        if (characterController != null) characterController.enabled = false;

        yield return new WaitForSeconds(0.25f);

        if (characterController != null) characterController.enabled = true;
    }
}
