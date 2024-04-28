using UnityEngine;
using Mirror;
using Mirror.Discovery;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    public CustomNetworkManager Create()
    {
        var currentManager = Instantiate<CustomNetworkManager>(this);
        //currentManager.offlineScene = SceneKeyword.BED_WARS;
        //currentManager.onlineScene = SceneKeyword.GAME;
        return currentManager;
    }

    public void HostGame()
    {
        WorldState.IsDedicatedServer = false;
        GetComponent<ServerDiscoveryMenu>().StartHost();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<CharacterSelectionMessage>(OnCreateCharacter);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        // you can send the message here, or wherever else you want
        CharacterSelectionMessage characterMessage = new CharacterSelectionMessage
        {
            characterIndex = 0
        };

        conn.Send(characterMessage);
    }

    void OnCreateCharacter(NetworkConnection conn, CharacterSelectionMessage message)
    {
        GameObject playerGameObject = Instantiate(playerPrefab);

        // place the player on a random position on the beach looking to the center of the island
        WorldState.Instance.PlacePlayerOnStartPosition(playerGameObject.transform);

        // call this to use this gameobject as the primary controller
        NetworkServer.AddPlayerForConnection(conn, playerGameObject);
    }


    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        //FindObjectOfType<MainMenu>().CallTimeout(); TO DO
    }

    private struct CharacterSelectionMessage : NetworkMessage
    {
        public int characterIndex;
    }
}