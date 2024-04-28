using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using UnityEngine.UI;

/*
    Authenticators: https://mirror-networking.com/docs/Components/Authenticators/
    Documentation: https://mirror-networking.com/docs/Guides/Authentication.html
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
*/

public class SimplePasswordAuthenticator : NetworkAuthenticator
{   

    [Header("Custom Properties")]
    // set these in the inspector    
    public string password;    

    #region Messages
    public struct AuthRequestMessage : NetworkMessage
    {
        // use whatever credentials make sense for your game
        // for example, you might want to pass the accessToken if using oauth        
        public string authPassword;
    }

    public struct AuthResponseMessage : NetworkMessage
    {
        public byte code;
        public string message;
    }

    #endregion

    #region Server

    /// <summary>
    /// Called on server from StartServer to initialize the Authenticator
    /// <para>Server message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartServer()
    {
        // register a handler for the authentication request we expect from client
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
    }

    /// <summary>
    /// Called on server from OnServerAuthenticateInternal when a client needs to authenticate
    /// </summary>
    /// <param name="conn">Connection to client.</param>
    public override void OnServerAuthenticate(NetworkConnection conn) { }

    /// <summary>
    /// Called on server when the client's AuthRequestMessage arrives
    /// </summary>
    /// <param name="conn">Connection to client.</param>
    /// <param name="msg">The message payload</param>
    public void OnAuthRequestMessage(NetworkConnection conn, AuthRequestMessage msg)
    {
        // check the credentials by calling your web server, database table, playfab api, or any method appropriate.
        if (msg.authPassword == password)
        {
            // create and send msg to client so it knows to proceed
            AuthResponseMessage authResponseMessage = new AuthResponseMessage
            {
                code = 100,
                message = "Success"
            };

            conn.Send(authResponseMessage);

            // Accept the successful authentication
            ServerAccept(conn);
        }
        else
        {
            // create and send msg to client so it knows to disconnect
            AuthResponseMessage authResponseMessage = new AuthResponseMessage
            {
                code = 200,
                message = "Invalid Credentials"
            };

            conn.Send(authResponseMessage);

            // must set NetworkConnection isAuthenticated = false
            conn.isAuthenticated = false;

            // disconnect the client after 1 second so that response message gets delivered
            StartCoroutine(DelayedDisconnect(conn, 1));
        }
    }

    IEnumerator DelayedDisconnect(NetworkConnection conn, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        // Reject the unsuccessful authentication
        ServerReject(conn);
    }

    #endregion

    #region Client

    /// <summary>
    /// Called on client from StartClient to initialize the Authenticator
    /// <para>Client message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartClient()
    {
        // register a handler for the authentication response we expect from server
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
    }


    /// <summary>
    /// Called on client from StopClient to reset the Authenticator
    /// <para>Client message handlers should be unregistered in this method.</para>
    /// </summary>
    public override void OnStopClient()
    {
        // unregister the handler for the authentication response
        NetworkClient.UnregisterHandler<AuthResponseMessage>();
    }

    /// <summary>
    /// Called on client from OnClientAuthenticateInternal when a client needs to authenticate
    /// </summary>
    /// <param name="conn">Connection of the client.</param>
    public override void OnClientAuthenticate(NetworkConnection conn)
    {
        AuthRequestMessage authRequestMessage = new AuthRequestMessage
        {            
            authPassword = password
        };

        conn.Send(authRequestMessage);
    }

    /// <summary>
    /// Called on client when the server's AuthResponseMessage arrives
    /// </summary>
    /// <param name="conn">Connection to client.</param>
    /// <param name="msg">The message payload</param>
    public void OnAuthResponseMessage(NetworkConnection conn, AuthResponseMessage msg)
    {
        GameObject MessageDisplay = GameObject.Find("PasswordMessages");
        GameObject ErrorDisplay = GameObject.Find("ServerError");

        if (msg.code == 100)
        {
            if (MessageDisplay) MessageDisplay.GetComponentInChildren<Text>().text = "PASSWORD ACCEPTED";
            if (ErrorDisplay) ErrorDisplay.GetComponentInChildren<Text>().text = "PASSWORD ACCEPTED";
            // Authentication has been accepted
            ClientAccept(conn);
        }
        else
        {
            if (MessageDisplay) MessageDisplay.GetComponentInChildren<Text>().text = "WRONG PASSWORD!";
            if (ErrorDisplay) ErrorDisplay.GetComponentInChildren<Text>().text = "PASSWORD ACCEPTED";
            // Authentication has been rejected
            ClientReject(conn);
        }
    }
    #endregion
}
