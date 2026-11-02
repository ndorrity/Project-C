using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkConnectUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TextMeshProUGUI localIpDisplay;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    private UnityTransport transport;

    private void Awake()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);

        DisplayLocalIP();
    }

    private void DisplayLocalIP()
    {
        string localIP = GetLocalIPv4();
        if (localIpDisplay != null)
        {
            localIpDisplay.text = $"Your LAN IP: {localIP}";
        }
    }

    public void StartHost()
    {
        transport.SetConnectionData("0.0.0.0", 7777);
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        string targetIP = string.IsNullOrEmpty(ipInputField.text) ? "127.0.0.1" : ipInputField.text.Trim();
        transport.SetConnectionData(targetIP, 7777);
        NetworkManager.Singleton.StartClient();
    }

    private string GetLocalIPv4()
    {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}
