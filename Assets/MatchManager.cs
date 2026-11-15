using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Match Settings")]
    [SerializeField] private float matchDurationSeconds = 300f;

    [Header("UI References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TMP_Text player1KillsText;
    [SerializeField] private TMP_Text player2KillsText;
    [SerializeField] private GameObject matchOverPanel;
    [SerializeField] private TMP_Text winnerText;

    public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(
        300f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Player1Kills = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Player2Kills = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool matchEnded;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            TimeRemaining.Value = matchDurationSeconds;
            Player1Kills.Value = 0;
            Player2Kills.Value = 0;
        }

        if (matchOverPanel != null) matchOverPanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);

        UpdateScoreUI();
        Player1Kills.OnValueChanged += (_, _) => UpdateScoreUI();
        Player2Kills.OnValueChanged += (_, _) => UpdateScoreUI();
    }

    private void Update()
    {
        if (leaderboardPanel != null && !matchEnded)
        {
            leaderboardPanel.SetActive(Input.GetKey(KeyCode.Tab));
        }

        if (IsServer && !matchEnded)
        {
            if (TimeRemaining.Value > 0f)
            {
                TimeRemaining.Value = Mathf.Max(0f, TimeRemaining.Value - Time.deltaTime);
            }
            else
            {
                EndMatchClientRpc();
            }
        }

        UpdateTimerUI(TimeRemaining.Value);
    }

    private void UpdateTimerUI(float timeToDisplay)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateScoreUI()
    {
        if (player1KillsText != null) player1KillsText.text = $"Player 1 (Host): {Player1Kills.Value}";
        if (player2KillsText != null) player2KillsText.text = $"Player 2 (Client): {Player2Kills.Value}";
    }

    public void RecordKill(ulong killerClientId)
    {
        if (!IsServer || matchEnded) return;

        if (killerClientId == 0)
            Player1Kills.Value++;
        else
            Player2Kills.Value++;
    }

    [ClientRpc]
    private void EndMatchClientRpc()
    {
        matchEnded = true;

        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (matchOverPanel != null) matchOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (winnerText != null)
        {
            if (Player1Kills.Value > Player2Kills.Value)
                winnerText.text = $"Player 1 Wins! ({Player1Kills.Value} - {Player2Kills.Value})";
            else if (Player2Kills.Value > Player1Kills.Value)
                winnerText.text = $"Player 2 Wins! ({Player2Kills.Value} - {Player1Kills.Value})";
            else
                winnerText.text = $"Draw! ({Player1Kills.Value} - {Player2Kills.Value})";
        }
    }
}
