using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NetworkWeapon : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private int damage = 25;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.25f;

    [Header("Audio & Visuals")]
    [SerializeField] private AudioSource gunAudioSource;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private LineRenderer bulletTracer;
    [SerializeField] private float tracerDuration = 0.05f;

    private float nextFireTime;

    private void Update()
    {
        if (!IsOwner) return;
        if (SceneManager.GetActiveScene().name != "Arena") return;

        bool fireHeld = (Mouse.current != null && Mouse.current.leftButton.isPressed)
                        || Input.GetMouseButton(0);

        if (fireHeld && Time.time >= nextFireTime)
        {
            if (cameraTransform == null) return;

            nextFireTime = Time.time + fireRate;
            ShootServerRpc(cameraTransform.position, cameraTransform.forward);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 origin, Vector3 direction)
    {
        Vector3 endPoint = origin + direction * range;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            endPoint = hit.point;
            NetworkHealth targetHealth = hit.collider.GetComponentInParent<NetworkHealth>();
            if (targetHealth != null && targetHealth.OwnerClientId != OwnerClientId)
            {
                targetHealth.TakeDamage(damage, OwnerClientId);
            }
        }

        Vector3 startPoint = muzzlePoint != null ? muzzlePoint.position : origin;
        PlayShootEffectsClientRpc(startPoint, endPoint);
    }

    [ClientRpc]
    private void PlayShootEffectsClientRpc(Vector3 startPoint, Vector3 endPoint)
    {
        if (gunAudioSource != null && shootClip != null)
        {
            gunAudioSource.PlayOneShot(shootClip);
        }

        if (bulletTracer != null)
        {
            StartCoroutine(ShowTracer(startPoint, endPoint));
        }
    }

    private IEnumerator ShowTracer(Vector3 startPoint, Vector3 endPoint)
    {
        bulletTracer.SetPosition(0, startPoint);
        bulletTracer.SetPosition(1, endPoint);
        bulletTracer.enabled = true;

        yield return new WaitForSeconds(tracerDuration);

        bulletTracer.enabled = false;
    }
}
