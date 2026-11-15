using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;

    [Header("Local HUD")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private GameObject hudCanvas;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
        }

        if (IsOwner)
        {
            UpdateHUDVisibility();
            UpdateHUD(CurrentHealth.Value);
            CurrentHealth.OnValueChanged += OnHealthChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            if (hudCanvas != null) hudCanvas.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateHUDVisibility();
    }

    private void UpdateHUDVisibility()
    {
        if (!IsOwner || hudCanvas == null) return;
        bool inArena = SceneManager.GetActiveScene().name == "Arena";
        hudCanvas.SetActive(inArena);
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        UpdateHUD(newVal);
    }

    private void UpdateHUD(int health)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }
        if (healthText != null)
        {
            healthText.text = $"{health} HP";
        }
    }

    public void TakeDamage(int amount, ulong attackerClientId = 0)
    {
        if (!IsServer) return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

        if (CurrentHealth.Value <= 0)
        {
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.RecordKill(attackerClientId);
            }
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        CurrentHealth.Value = maxHealth;
        RespawnClientRpc();
    }

    [ClientRpc]
    private void RespawnClientRpc()
    {
        if (!IsOwner) return;

        string spawnName = (OwnerClientId % 2 == 0) ? "SpawnPoint_1" : "SpawnPoint_2";
        GameObject spawnObj = GameObject.Find(spawnName);

        if (spawnObj != null)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            transform.position = spawnObj.transform.position;
            transform.rotation = spawnObj.transform.rotation;

            if (cc != null) cc.enabled = true;
        }
    }
}
