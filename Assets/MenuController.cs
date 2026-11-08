using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Connection References")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TextMeshProUGUI localIpDisplay;

    private void Awake()
    {
        // Enforce clean state on launch before any frame draws
        OpenMainMenu();
    }

    private void Start()
    {
        // Display LAN IP on start if the text reference is hooked up
        DisplayLocalIP();
    }

    // Helper: Deactivates all screens and shows strictly the requested panel
    private void ShowOnly(GameObject targetPanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(targetPanel == mainMenuPanel);
        if (connectionPanel != null) connectionPanel.SetActive(targetPanel == connectionPanel);
        if (optionsPanel != null) optionsPanel.SetActive(targetPanel == optionsPanel);
        if (creditsPanel != null) creditsPanel.SetActive(targetPanel == creditsPanel);
        if (gameOverPanel != null) gameOverPanel.SetActive(targetPanel == gameOverPanel);
    }

    // --- Panel Navigation Methods (Assign to Button OnClick) ---
    public void OpenMainMenu() => ShowOnly(mainMenuPanel);
    public void OpenConnection() => ShowOnly(connectionPanel);
    public void OpenOptions() => ShowOnly(optionsPanel);
    public void OpenCredits() => ShowOnly(creditsPanel);
    public void OpenGameOver() => ShowOnly(gameOverPanel);

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // --- LAN Networking Actions ---
    public void HostGame()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("MenuController: No NetworkManager found in scene!");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("0.0.0.0", 7777);
        }

        // Bind callback before starting host to catch incoming connections
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;

        NetworkManager.Singleton.StartHost();
        Debug.Log("Hosting LAN server on port 7777. Waiting for opponent to join...");
    }

    public void JoinGame()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("MenuController: No NetworkManager found in scene!");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        string targetIP = (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
            ? ipInputField.text.Trim()
            : "127.0.0.1";

        if (transport != null)
        {
            transport.SetConnectionData(targetIP, 7777);
        }

        NetworkManager.Singleton.StartClient();
        Debug.Log($"Joining LAN host at {targetIP}:7777...");
    }

    private void HandleClientConnected(ulong clientId)
    {
        // Only the host/server coordinates scene changes
        if (!NetworkManager.Singleton.IsServer) return;

        // Auto-start once 2 players are connected (Host + 1 Client)
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= 2)
        {
            Debug.Log("Opponent connected! Loading Arena...");
            NetworkManager.Singleton.SceneManager.LoadScene(
                "Arena",
                LoadSceneMode.Single
            );
        }
    }

    private void DisplayLocalIP()
    {
        if (localIpDisplay == null) return;

        try
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIpDisplay.text = $"Your LAN IP: {ip}";
                    return;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to determine local IP: {ex.Message}");
        }

        localIpDisplay.text = "Your LAN IP: 127.0.0.1";
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }
}
