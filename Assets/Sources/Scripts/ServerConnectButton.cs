using UnityEngine;
using UnityEngine.UI;

public class ServerConnectButton : MonoBehaviour
{
    /// <summary>
    /// Server button parameters
    /// </summary>
    public long serverID;
    public string hostName;

    /// <summary>
    /// The lable of the button
    /// </summary>
    Text ButtonLabel;

    /// <summary>
    /// Called upon creation for setup
    /// </summary>
    private void Start()
    {
        // Assign the main menu reference
        //mainMenu = FindObjectOfType<MainMenu>();
        // FInd the label
        ButtonLabel = GetComponentInChildren<Text>();
        // Set the value of the label to the name of the host (IP)
        ButtonLabel.text = hostName;
    }

    /// <summary>
    /// Called upon clicking the button
    /// </summary>
    public void OnClick()
    {
        if (!enabled) return;
        enabled = false; // prevent double clicking

        // Set server ID we clicked on
        //mainMenu.SetServerID(serverID);
        // Show the password input panel
        //mainMenu.ShowClientPasswordPanel();
    }
}