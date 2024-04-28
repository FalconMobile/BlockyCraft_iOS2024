using System.IO;
using System.Linq;
using Mirror;
using Mirror.Discovery;
using Scripts.Advertising;
using Source.Scripts.AnalyticsFirebase.FirebaseInit;
using UI.PiratesOfVoxel.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {
        /// <summary>
        /// For headless server mode settings are found in a file called 'server.config'
        /// Check documentation for options
        /// </summary>
        const string SERVER_SETTINGS_FILENAME = "server.config";

        [Header("NetworkManager Prefab")] public GameObject NetManagerPrefab;

        /// <summary>
        /// References to main components
        /// </summary>
        NetworkManager NetworkManager;

        ServerDiscoveryMenu customDiscoveryMenu;

        /// <summary>
        /// Set by 'Join Game' prefab buttons
        /// </summary>
        [Header("Server ID")]
        public long currentServerID;

        /// <summary>
        /// Menu Panels
        /// </summary>

        // Main Panel
        [Header("Main Menu Main Panel")]
        public GameObject MainPanel;
        public RateUsPanel RateUsPanel;

        /// Local Game Panels
        [Header("Local Game GUI")]
        public GameObject LocalGamePanel;

        public GameObject LocalServerListPanel;

        public GameObject LocalServerListRoot;

        // Server Options Panels
        [Header("Online Game GUI")] public GameObject ServerGamePanel;
        public GameObject ConnectToServerlPanel;
        [Header("InputFields and buttons")] public InputField serverAddressInput;
        public InputField serverPasswordInput;
        public InputField serverPasswordInput2;
        public InputField clientPasswordInput;
        public InputField clientPasswordInput2;
        public GameObject serverPasswordPanel;
        public GameObject clientPasswordPanel;
        public GameObject CancelConnectButton;

        public Text ErrorInAddress;

        // Other Panels
        [Header("Additional Panels")] public GameObject AboutPanel;

        public GameObject OptionsPanel;

        // Option sliders
        [Header("Option Sliders")] public Slider optionCannibalsAmount;
        public Slider optionWildLifeAmount;
        public Slider optionBirdsAmount;
        public Slider optionIslandSize;

        /// <summary>
        /// Menu Sounds
        /// </summary>
        [Header("Audio Stuff")]
        public AudioClip ButtonHighlight;

        public AudioClip ButtonClick;
        AudioSource thisAudioSource;

        [SerializeField] private GameObject completionWarning;
        [SerializeField] private GameObject loadedMenu;
        private void Awake()
        {
            SetStartLanguage();
            RateUsPanel.ShowRateUs();
        }

        private void Update()
        {
            //if running on Android, check for Menu/Home and exit
            if (Application.platform == RuntimePlatform.Android)
            {
                if (Input.GetKey(KeyCode.Escape))
                {
                    completionWarning.SetActive(true);
                }
            }
        }

        private static void SetStartLanguage()
        {
            if (PlayerPrefs.HasKey("Language") && PlayerPrefs.GetString("Language") != "") 
                return;
            
            var systemLang = Application.systemLanguage.ToString();
            
            if (systemLang == "Belorussian") 
                systemLang = "Russian";

            Localization.language = 
                Localization.knownLanguages.Contains(systemLang) 
                    ? systemLang : "English";
        }

        // Start is called before the first frame update
        private void Start()
        {
            Setup();

            // Make sure we have the Local Server List object, this is where Join Game prefabs get spawned under
            if (LocalServerListRoot == null) LocalServerListRoot = GameObject.Find("ServerList");

            // Find associate panel as well - these should be setup, these are just in case
            if (LocalServerListPanel == null) LocalServerListPanel = LocalServerListRoot.transform.parent.gameObject;

            LoadWorldOptions();

            // ConfigLoader.LoadConfig();
            // Disable panels so that they aren't visible on Start
            LocalGamePanel.SetActive(false);
            LocalServerListPanel.SetActive(false);
            ServerGamePanel.SetActive(false);
            ConnectToServerlPanel.SetActive(false);
            CancelConnectButton.SetActive(false);
            OptionsPanel.SetActive(false);
            AboutPanel.SetActive(false);

            clientPasswordPanel.SetActive(false);
            serverPasswordPanel.SetActive(false);

            // Assign audio source for GUI sound effects
            thisAudioSource = GetComponent<AudioSource>();

            // Make sure cursor is visible and free
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            LoadServerSettingsFile();
            
            AdvertisingViewer.Instance.Init();
            FirebaseInit.Init();
        }
        
        
        /// <summary>
        /// If we come back to this scene, some references may be lost, find references again
        /// </summary>
        private void Setup()
        {
            // Find the NetworkManager
            NetworkManager = FindObjectOfType<CustomNetworkManager>();

            // If there is none, make a new one
            if (NetworkManager == null)
                NetworkManager = Instantiate(NetManagerPrefab).GetComponent<CustomNetworkManager>();

            // And find the reference to the 
            customDiscoveryMenu = NetworkManager.GetComponent<ServerDiscoveryMenu>();
            NetworkManager.offlineScene = SceneKeyword.LOBBY;
            NetworkManager.onlineScene = SceneKeyword.GAME;
        }


        public void HostGame()
        {
            Debug.Log("MainMenu HostGame");
            thisAudioSource.PlayOneShot(ButtonClick);
            loadedMenu.SetActive(true);
            AdvertisingViewer.Instance.ShowSkipping(OnShow, AdsCallPlaces.NewGameAd);
        }

        private void OnShow()
        {
            RandomWorld.isNewGame = true;
            Debug.Log("MainMenu HostGame OnShow");
            UpdateWorldOptions();
            SetServerMode(false);
            SetPassword();
            customDiscoveryMenu.StartHost();
        }

        public void CancelConnection()
        {
            ErrorInAddress.text = "CANCELLING...";
            NetworkManager.StopClient();
            CancelConnectButton.SetActive(false);
        }

        public void StartServer()
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            UpdateWorldOptions();
            SetServerMode(true);
            customDiscoveryMenu.StartServer();
        }

        void SetServerMode(bool serverMode)
        {
            WorldState.IsDedicatedServer = serverMode;
        }

        public void FindGame()
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            serverPasswordPanel.SetActive(false);

            if (LocalServerListPanel.activeSelf)
            {
                LocalServerListPanel.SetActive(false);
                return;
            }

            if (LocalServerListRoot.transform.childCount > 0)
            {
                for (int i = 0; i < LocalServerListRoot.transform.childCount; i++)
                {
                    Destroy(LocalServerListRoot.transform.GetChild(i).gameObject);
                }
            }

            LocalServerListPanel.SetActive(true);
            if (customDiscoveryMenu == null)
            {
                Setup();
            }

            customDiscoveryMenu.StartDiscovery(LocalServerListRoot);
        }

        public void ToggleLocalGamePanel()
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            LocalGamePanel.SetActive(!LocalGamePanel.activeSelf);
            serverPasswordPanel.SetActive(LocalGamePanel.activeSelf);

            if (LocalGamePanel.activeSelf)
            {
                ConnectToServerlPanel.SetActive(false);
                ServerGamePanel.SetActive(false);
                MainPanel.SetActive(false);
                clientPasswordPanel.SetActive(false);
            }
        }

        public void ToggleServerGamePanel()
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            ServerGamePanel.SetActive(!ServerGamePanel.activeSelf);
            serverPasswordPanel.SetActive(ServerGamePanel.activeSelf);
            if (ServerGamePanel.activeSelf)
            {
                LocalServerListPanel.SetActive(false);
                LocalGamePanel.SetActive(false);
                MainPanel.SetActive(false);
                clientPasswordPanel.SetActive(false);
            }
        }

        public void ToggleConnectToServerPanel()
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            ConnectToServerlPanel.SetActive(!ConnectToServerlPanel.activeSelf);
        }

        public void BackToMainPanel()
        {
            MainPanel.SetActive(true);
            LocalGamePanel.SetActive(false);
            LocalServerListPanel.SetActive(false);
            ServerGamePanel.SetActive(false);
            ConnectToServerlPanel.SetActive(false);
            thisAudioSource.PlayOneShot(ButtonClick);
            clientPasswordPanel.SetActive(false);
            serverPasswordPanel.SetActive(false);
        }

        public void ToggleOptionsPanel()
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            OptionsPanel.SetActive(!OptionsPanel.activeSelf);
            if (OptionsPanel.activeSelf)
            {
                AboutPanel.SetActive(false);
                clientPasswordPanel.SetActive(false);
                serverPasswordPanel.SetActive(false);
            }
        }

        public void ToggleAboutPanel()
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            AboutPanel.SetActive(!AboutPanel.activeSelf);
            if (AboutPanel.activeSelf)
            {
                OptionsPanel.SetActive(false);
                clientPasswordPanel.SetActive(false);
                serverPasswordPanel.SetActive(false);
            }
        }

        public void Quit()
        {
            thisAudioSource.PlayOneShot(ButtonClick);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
         Application.OpenURL(webplayerQuitURL);
#else
         Application.Quit();
#endif
        }

        public void OnHover()
        {
            thisAudioSource.PlayOneShot(ButtonHighlight);
        }

        /// <summary>
        /// Get options values from PlayerPrefs
        /// </summary>
        private void LoadWorldOptions()
        {
            if (optionCannibalsAmount != null)
            {
                optionCannibalsAmount.value = PlayerPrefs.GetFloat(SpawnerKeyword.CANNIBALS, 0f);
            }

            if (optionBirdsAmount != null)
            {
                optionBirdsAmount.value = PlayerPrefs.GetFloat(SpawnerKeyword.BIRDS, 1f);
            }

            if (optionWildLifeAmount != null)
            {
                optionWildLifeAmount.value = PlayerPrefs.GetFloat(SpawnerKeyword.WILDLIFE, 1f);
            }

            if (optionIslandSize != null)
            {
                optionIslandSize.value = PlayerPrefs.GetFloat(TerrainKeyword.ISLAND_SIZE, 1f);
            }
        }

        /// <summary>
        /// Get options values from file, for headless
        /// </summary>
        private void LoadServerSettingsFile()
        {
            var fullPath = Path.GetFullPath(".") + "/" + SERVER_SETTINGS_FILENAME;
            if (!File.Exists(fullPath))
            {
                Debug.Log("No server settings file found at " + fullPath + ". Showing main menu...");
                return;
            }

            Debug.Log("Loading server settings from " + fullPath);

            var settings = File.ReadAllLines(fullPath);
            var separator = new[] {'='};

            var startInServerModeAutomatically = false;

            foreach (var t in settings)
            {
                var parts = t.Split(separator);
                if (parts.Length < 2) continue;
                if (!int.TryParse(parts[1], out var paramValue)) continue;
                var paramName = parts[0].Trim();

                if (paramName.Equals("serverMode", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    startInServerModeAutomatically = true;
                }
                else if (optionCannibalsAmount != null && paramName.Equals("cannibalsAmount",
                             System.StringComparison.InvariantCultureIgnoreCase))
                {
                    optionCannibalsAmount.value = paramValue;
                }
                else if (optionBirdsAmount != null &&
                         paramName.Equals("birdsAmount", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    optionBirdsAmount.value = paramValue;
                }
                else if (optionWildLifeAmount != null && paramName.Equals("wildLifeAmount",
                             System.StringComparison.InvariantCultureIgnoreCase))
                {
                    optionWildLifeAmount.value = paramValue;
                }
                else if (optionIslandSize != null &&
                         paramName.Equals("islandSize", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    optionIslandSize.value = paramValue;
                }
            }

            if (startInServerModeAutomatically)
            {
                StartServer();
            }
        }

        /// <summary>
        /// Set PlayerPrefs values based on settings
        /// </summary>
        private void UpdateWorldOptions()
        {
            if (optionCannibalsAmount != null)
            {
                PlayerPrefs.SetFloat(SpawnerKeyword.CANNIBALS, optionCannibalsAmount.value);
            }

            if (optionBirdsAmount != null)
            {
                PlayerPrefs.SetFloat(SpawnerKeyword.BIRDS, optionBirdsAmount.value);
            }

            if (optionWildLifeAmount != null)
            {
                PlayerPrefs.SetFloat(SpawnerKeyword.WILDLIFE, optionWildLifeAmount.value);
            }

            if (optionIslandSize != null)
            {
                PlayerPrefs.SetFloat(TerrainKeyword.ISLAND_SIZE, optionIslandSize.value);
            }
        }

        /// <summary>
        /// Basic IP address check for validity
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool IsAddressValid(string input)
        {
            var parts = input.Split('.');
            var correctCounter = 0;

            if (parts.Length == 4)
            {
                foreach (var p in parts)
                {
                    if (int.TryParse(p, out int k))
                    {
                        if (k < 0 || k > 255)
                            ErrorInAddress.text = "The " + p + " of the address is not between 0-255";
                        else
                            correctCounter++;
                    }
                    else
                        ErrorInAddress.text = "The " + p + " of the address is not a number";
                }
            }
            else
            {
                ErrorInAddress.text = "You need 4 parts for the address";
            }

            if (correctCounter != 4) 
                return false;
            
            ErrorInAddress.text = "CONNECTING...";
            return true;
        }


        public void JoinGame()
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            SetServerMode(false);

            if (serverAddressInput.text.Length > 0)
            {
                if (IsAddressValid(serverAddressInput.text))
                {
                    NetworkManager.networkAddress = serverAddressInput.text;
                    NetworkManager.StartClient();
                    CancelConnectButton.SetActive(true);
                }
            }
            else NetworkManager.StartClient();
        }

        private void JoinGameFromServerButton(ServerResponse info)
        {
            thisAudioSource.PlayOneShot(ButtonClick);
            SetServerMode(false);
            NetworkManager.StartClient(info.uri);
        }

        /// <summary>
        /// Called by menu buttons under InputFields button is pressed
        /// Checks which InputField is currently active and assigns that
        /// value as password on the 'SimplePasswordAuthenticator' component
        /// </summary>
        public void SetPassword()
        {
            if (serverPasswordInput.transform.parent.transform.parent.gameObject.activeSelf)
                NetworkManager.authenticator.GetComponent<SimplePasswordAuthenticator>().password =
                    serverPasswordInput.text;

            if (serverPasswordInput2.transform.parent.transform.parent.gameObject.activeSelf)
                NetworkManager.authenticator.GetComponent<SimplePasswordAuthenticator>().password =
                    serverPasswordInput2.text;

            if (clientPasswordInput.transform.parent.transform.parent.gameObject.activeSelf)
                NetworkManager.authenticator.GetComponent<SimplePasswordAuthenticator>().password =
                    clientPasswordInput.text;

            if (clientPasswordInput2.transform.parent.transform.parent.gameObject.activeSelf)
                NetworkManager.authenticator.GetComponent<SimplePasswordAuthenticator>().password =
                    clientPasswordInput2.text;
        }

        /// <summary>
        /// When a 'Join Game' prefab is pressed we get the serverID info to be used by the 'CustomDiscoveryMenu' component
        /// </summary>
        /// <param name="serverID"></param>
        public void SetServerID(long serverID)
        {
            currentServerID = serverID;
        }

        /// <summary>
        /// Called when the 'Join Game' button or 'Connect to server button is pressed'
        /// </summary>
        public void ConnectLocal()
        {
            JoinGame();
        }

        /// <summary>
        /// Called when the 'Join Game' button or 'Connect to server button is pressed'
        /// </summary>
        public void ConnectIP()
        {
            // If we have set an IP use that
            if (serverAddressInput.text.Length > 0)
                JoinGame();
            else // Else use the serverID set by the 'Join Game' prefab button
                JoinGameFromServerButton(customDiscoveryMenu.discoveredServers[currentServerID]);
        }

        /// <summary>
        /// Called when network manager times out
        /// </summary>
        public void CallTimeout()
        {
            CancelConnectButton.SetActive(false);
            ErrorInAddress.text += "TIMED OUT...";
        }

        /// <summary>
        /// Called when pressing appropriate button main menu
        /// </summary>
        public void ShowClientPasswordPanel()
        {
            clientPasswordPanel.SetActive(!clientPasswordPanel.activeSelf);
        }

        /// <summary>
        /// Called when pressing appropriate button main menu
        /// </summary>
        public void ShowServerPasswordPanel()
        {
            serverPasswordPanel.SetActive(!serverPasswordPanel.activeSelf);
        }

        public void LoadingWorld()
        {
            loadedMenu.SetActive(true);
            
            AdvertisingViewer.Instance.ShowRewarded(LoadedGame, LoadedGame,AdsCallPlaces.LoadingAd);
        }

        private void LoadedGame()
        {
            RandomWorld.isNewGame = false;
            thisAudioSource.PlayOneShot(ButtonClick);
            UpdateWorldOptions();
            SetServerMode(false);
            SetPassword();
            customDiscoveryMenu.StartHost();
            loadedMenu.SetActive(false);
        }
    }
}