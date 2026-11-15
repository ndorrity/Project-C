using Unity.Netcode;
using UnityEngine;

public class NetworkCrouch : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraContainer;
    [SerializeField] private Transform characterModel; // Assign InsurgentUrban

    [Header("Crouch Settings")]
    [SerializeField] private KeyCode crouchKey = KeyCode.C; // Or KeyCode.LeftControl
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float crouchingHeight = 1.0f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Camera & Visual Offsets")]
    [SerializeField] private float standingCameraY = 1.6f;
    [SerializeField] private float crouchingCameraY = 0.85f;
    [SerializeField] private float crouchingVisualScaleY = 0.55f;

    private CharacterController characterController;
    private Vector3 originalModelScale;

    // Owner writes to this; everyone reads it to see the crouch state
    private NetworkVariable<bool> isCrouching = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterModel != null)
        {
            originalModelScale = characterModel.localScale;
        }
    }

    private void Update()
    {
        // 1. Only the local owner reads input and checks ceiling clearance
        if (IsOwner)
        {
            HandleCrouchInput();
        }

        // 2. Both owner and remote clients smoothly interpolate the visual/collider state
        ApplySmoothCrouch(isCrouching.Value);
    }

    private void HandleCrouchInput()
    {
        bool wantsToCrouch = Input.GetKey(crouchKey) || Input.GetKey(KeyCode.LeftControl);

        if (wantsToCrouch)
        {
            if (!isCrouching.Value) isCrouching.Value = true;
        }
        else if (isCrouching.Value)
        {
            // Prevent standing up if there is a low ceiling or object directly overhead
            if (CanStandUp())
            {
                isCrouching.Value = false;
            }
        }
    }

    private bool CanStandUp()
    {
        float checkDistance = standingHeight - crouchingHeight;
        Vector3 origin = transform.position + Vector3.up * (crouchingHeight - 0.1f);
        return !Physics.SphereCast(origin, characterController.radius * 0.9f, Vector3.up, out _, checkDistance);
    }

    private void ApplySmoothCrouch(bool crouched)
    {
        float targetHeight = crouched ? crouchingHeight : standingHeight;
        float targetCamY = crouched ? crouchingCameraY : standingCameraY;
        float targetScaleY = crouched ? originalModelScale.y * crouchingVisualScaleY : originalModelScale.y;

        // Smoothly adjust CharacterController height and keep feet planted at Y = 0
        if (characterController != null)
        {
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            characterController.center = new Vector3(0f, characterController.height / 2f, 0f);
        }

        // Smoothly lower/raise the local first-person camera
        if (cameraContainer != null)
        {
            Vector3 camPos = cameraContainer.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchTransitionSpeed);
            cameraContainer.localPosition = camPos;
        }

        // Smoothly scale the third-person model vertically so the opponent sees you duck
        // (If you add an Animator crouch boolean later, replace this block with animator.SetBool("IsCrouching", crouched))
        if (characterModel != null)
        {
            Vector3 modelScale = characterModel.localScale;
            modelScale.y = Mathf.Lerp(modelScale.y, targetScaleY, Time.deltaTime * crouchTransitionSpeed);
            characterModel.localScale = modelScale;
        }
    }
}
