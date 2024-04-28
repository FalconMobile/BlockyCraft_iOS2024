using Mirror;

public partial class NetworkPlayer : NetworkBehaviour
{
    private void OnConsoleNewCommand(string text)
    {
        // send message
        if (!text.StartsWith("/") && !text.StartsWith("<"))
        {
            CmdSendTextChat(text);
        }
    }

    [Command]
    void CmdSendTextChat(string text)
    {
        // Send text messages to all clients
        RpcSendTextChat(playerName + ": " + text);
    }

#if MIRROR_32_1_2_OR_NEWER
    [ClientRpc(includeOwner = false)]
#endif
    public void RpcSendTextChat(string text)
    {
        env.ShowMessage("<color=cyan>" + text + "</color>");
    }
}