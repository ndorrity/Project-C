using Unity.Netcode;
using UnityEngine;

public class ArenaSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint1;
    [SerializeField] private Transform spawnPoint2;

    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        // Only the Server positions players
        if (!NetworkManager.Singleton.IsServer) return;

        // When a client finishes loading the scene OR synchronizes into an active game
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete || 
            sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete)
        {
            ulong clientId = sceneEvent.ClientId;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    PositionPlayer(client.PlayerObject, (int)clientId);
                }
            }
        }
    }

    private void PositionPlayer(NetworkObject playerObj, int playerIndex)
    {
        Transform targetSpawn = (playerIndex % 2 == 0) ? spawnPoint1 : spawnPoint2;
        if (targetSpawn == null) return;

        // Must temporarily disable CharacterController or it will reject the transform update
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerObj.transform.position = targetSpawn.position;
        playerObj.transform.rotation = targetSpawn.rotation;

        if (cc != null) cc.enabled = true;
    }
}
