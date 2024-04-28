using System;
using Mirror;
using MobileTouchInput;
using Scripts.Advertising;
using Sources.Scripts;
using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;

namespace UI
{
    /// <summary>
    /// In game menu
    /// </summary>
    public class IngameMenu : MonoBehaviour
    {
        /// <summary>
        /// Таймер течения времени в игре (0 время остановилось , 1 течения времени 1 к 1 с реальным)
        /// </summary>
        [NonSerialized]
        private float _timer;

        /// <summary>
        /// Фдаг остановлено время или нет.
        /// </summary>
        [NonSerialized]
        private bool _isTimeFlow;

        private VoxelPlayEnvironment _env;
        public GameObject pausedMenu;
        public GameObject respawnMenu;
        public GameObject gameOverMenu;

        [SerializeField] private NetworkManager networkManager;

        public Text countdownDisplay;

        /// <summary>
        /// Menu Sounds
        /// </summary>
        public AudioClip buttonHighlight;

        public AudioClip buttonClick;
        [SerializeField] private AudioSource thisAudioSource;

        [SerializeField] AmbientSoundMixer backgroundMusicSource;
        [SerializeField] private bool isMusicOn = true;
        public GameObject toggleButtonLabelOn;
        public GameObject toggleButtonLabelOff;

        [NonSerialized] private string _nameLoadSaveMap = String.Empty;

        [NonSerialized] private NetworkPlayer player = null;

        private bool _isPossibleToPause;
        private bool _isRolledUpTheGame;

        public void OnEnable()
        {
            NetworkPlayer.PlayerDead += OnPlayerDead;
        }

        public void OnDisable()
        {
            NetworkPlayer.PlayerDead -= OnPlayerDead;
        }

        private void OnPlayerDead(NetworkPlayer player)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            respawnMenu.SetActive(true);

            this.player = player;
        }

        /// <summary>
        /// Выход в главное меню после смерти , перезаписывает сохранение и оно будет не доступно игроку.
        /// </summary>
        public void ReturnToLobbyAfterDeath()
        {
            respawnMenu.SetActive(false);
            thisAudioSource.PlayOneShot(buttonClick);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _env.input.enabled = false;
            networkManager.StopHost();
        }

        public void RespawnPlayer()
        {
            respawnMenu.SetActive(false);
            AdvertisingViewer.Instance.ShowRewarded(() =>
            {
                Cursor.visible = false;
                player.RespawnCharacter();
            }, () =>
            {
                Cursor.visible = false;
                player.RespawnCharacter();
            }, AdsCallPlaces.WillBeReborn);
        }

        public void FreeRespawnPlayer()
        {
            respawnMenu.SetActive(false);
            Cursor.visible = false;
            player.RespawnCharacter();
        }

        private void Start()
        {
            _env = VoxelPlayEnvironment.instance;
            networkManager = FindObjectOfType<NetworkManager>();
            thisAudioSource = GetComponent<AudioSource>();
            backgroundMusicSource = FindObjectOfType<AmbientSoundMixer>();
            _nameLoadSaveMap = LoadSaveParamGame.nameLoadSaveMap;
            _env.OnInitialized += LoadGame;

            ToggleMusic();
        }

        void Update()
        {
            UpdatePause();

            _timer = _isTimeFlow ? 0 : 1f;
            Time.timeScale = _timer;

            //if running on Android, check for Menu/Home and exit
            if (Application.platform == RuntimePlatform.Android)
            {
                if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Menu))
                {
                    PauseGame();
                }
            }

            if (_isRolledUpTheGame)
            {
                PauseGame();
                _isRolledUpTheGame = false;
            }
        }

        void OnApplicationPause(bool hasFocus)
        {
            _isRolledUpTheGame = true;
        }

        private void UpdatePause()
        {
            _isPossibleToPause = (_env.input.GetButtonDown(InputButtonNames.Escape)) &&
                !gameOverMenu.activeSelf &&
                !VoxelPlayUI.instance.IsConsoleVisible;
            if (!_isPossibleToPause)
            {
                return;
            }

            PauseGame();
        }

        private void GameOver()
        {
            gameOverMenu.SetActive(true);
            pausedMenu.SetActive(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _env.input.enabled = false;
        }

        public void UpdateCountdown(float seconds)
        {
            countdownDisplay.text = "RESPAWN IN: " + Mathf.CeilToInt(seconds).ToString() + " SECONDS";
        }

        public void Resume()
        {
            thisAudioSource.PlayOneShot(buttonClick);
            pausedMenu.SetActive(false);
            _env.input.enabled = true;
        }

        public void ReturnToLobby()
        {
            thisAudioSource.PlayOneShot(buttonClick);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _env.input.enabled = false;
            networkManager.StopHost();
        }

        public void SaveGame()
        {
            SaveAndLoadingInventory.Save();
            _env.SaveGameBinary();
        }

        private void LoadGame()
        {
            if (RandomWorld.isNewGame)
            {
                return;
            }

            _env.LoadGameBinary(false);
        }

        public void ToggleMusic()
        {
            isMusicOn = !isMusicOn;

            if (isMusicOn)
            {
                toggleButtonLabelOff.SetActive(false);
                toggleButtonLabelOn.SetActive(true);
            }
            else
            {
                toggleButtonLabelOn.SetActive(false);
                toggleButtonLabelOff.SetActive(true);
            }

            if (backgroundMusicSource)
                backgroundMusicSource.ToggleMusic(isMusicOn);
            else
            {
                backgroundMusicSource = FindObjectOfType<AmbientSoundMixer>();
                backgroundMusicSource.ToggleMusic(isMusicOn);
            }
        }

        public void PauseGame()
        {
            bool isPaused = _isTimeFlow;

            pausedMenu.SetActive(!isPaused);
            Cursor.visible = !isPaused;
            _env.input.enabled = isPaused;

            if (!isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            _isTimeFlow = !isPaused;
        }

        public void ContinueGame()
        {
            _isTimeFlow = false;

            pausedMenu.SetActive(false);
            _env.input.enabled = true;

            Cursor.visible = false;

            _timer = 1f;
            Time.timeScale = _timer;
        }

        public void OnHover()
        {
            thisAudioSource.PlayOneShot(buttonHighlight);
        }

        public void MobileInputOnButtonDown(InputButtonNames buttonName)
        {
            if (MobileInput.Instance == null)
            {
                return;
            }

            MobileInput.Instance.OnButtonDown(buttonName);
        }

        public void MobileInputOnButtonUp(InputButtonNames buttonName)
        {
            if (MobileInput.Instance == null)
            {
                return;
            }

            MobileInput.Instance.OnButtonUp(buttonName);
        }

        public void Quit()
        {
            thisAudioSource.PlayOneShot(buttonClick);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
         Application.OpenURL(webplayerQuitURL);
#else
         Application.Quit();
#endif
        }
    }
}
