using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CharacterSelect : MonoBehaviour
    {
        CustomNetworkManager customNetworkManager;

        public Transform charactersRoot;
        public static int selectedCharacter;
        int degrees = 0;

        /// <summary>
        /// UI Elements
        /// </summary>
        public GameObject mainMenu;

        public Text walkSpeedDisplay;
        public Text runSpeedDisplay;
        public Text maxHealthDisplay;
        public Text meleeRangeDisplay;
        public Text meleeDamageDisplay;

        /// <summary>
        /// Menu Sounds
        /// </summary>
        public AudioClip buttonHighlight;
        public AudioClip buttonClick;
        AudioSource thisAudioSource;
        public AudioClip[] piratePhrases;
        public AudioClip[] navyPhrases;

        /// <summary>
        /// Pirate Animation and Sounds
        /// </summary>
        Animator[] thisAnimators;
        int randomAction;
        

        private void Awake()
        {
            thisAnimators = charactersRoot.GetComponentsInChildren<Animator>();
            for (int i = 0; i < thisAnimators.Length; i++)
            {
                thisAnimators[i].fireEvents = false;
            }
            
        }

        private void Start()
        {
            // Find the network manager instance
            customNetworkManager = FindObjectOfType<CustomNetworkManager>();
            // Assign the audiosource connected to this object and script
            thisAudioSource = GetComponent<AudioSource>();
            // Display stats of 'highlighted' character
            UpdateStatsDisplay();
            // Start random animations
            StartCoroutine(AnimatePirates());
            // Play a sound effect for the current pirate
            PlaySoundEffect();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextCharacter();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PreviousCharacter();
            }

            // Animates the character wheel so that we are looking at the correct character
            charactersRoot.rotation = Quaternion.RotateTowards(charactersRoot.rotation, Quaternion.Euler(0, degrees, 0), 1);
        }

        public void NextCharacter()
        {
            // Change character index
            if (selectedCharacter < thisAnimators.Length - 1)
                selectedCharacter++;
            else
                selectedCharacter = 0;

            // Display stats of 'highlighted' character
            UpdateStatsDisplay();

            // Tell character wheel to rotate
            degrees -= 40;

            // Play a sound effect on click
            PlaySoundEffect();
        }

        public void PreviousCharacter()
        {
            // Change character index
            if (selectedCharacter > 0)
                selectedCharacter--;
            else
                selectedCharacter = thisAnimators.Length - 1;

            // Display stats of 'highlighted' character
            UpdateStatsDisplay();

            // Tell character wheel to rotate
            degrees += 40;

            // Play a sound effect on click
            PlaySoundEffect();
        }

        /// <summary>
        /// On Click of Select button enable the main menu and play a click sound
        /// </summary>
        public void SelectCharacter()
        {            
            mainMenu.SetActive(true);
            thisAudioSource.PlayOneShot(buttonClick);
        }

        /// <summary>
        /// Called on mouse enter button area
        /// </summary>
        public void OnHover()
        {
            thisAudioSource.PlayOneShot(buttonHighlight);
        }

        void UpdateStatsDisplay ()
        {
            //walkSpeedDisplay.text = customNetworkManager.playerPrefabs[selectedCharacter].GetComponent<VoxelPlayFirstPersonController>().walkSpeed.ToString();
            //runSpeedDisplay.text = customNetworkManager.playerPrefabs[selectedCharacter].GetComponent<VoxelPlayFirstPersonController>().runSpeed.ToString();
            //maxHealthDisplay.text = customNetworkManager.playerPrefabs[selectedCharacter].GetComponent<NetworkPlayer>().maxHealth.ToString();
            //meleeRangeDisplay.text = customNetworkManager.playerPrefabs[selectedCharacter].GetComponent<NetworkPlayer>().meleeRange.ToString();
            //meleeDamageDisplay.text = customNetworkManager.playerPrefabs[selectedCharacter].GetComponent<NetworkPlayer>().meleeBluntDamage.ToString();
        }

        void PlaySoundEffect()
        {
            if (selectedCharacter == 0 || selectedCharacter == 1)
            {
                if (navyPhrases.Length > 0)
                    thisAudioSource.PlayOneShot(navyPhrases[Random.Range(0, navyPhrases.Length)]);
                else
                    thisAudioSource.PlayOneShot(buttonClick);
            }
            else
            {
                // Play a random pirate phrase if available 
                if (piratePhrases.Length > 0)
                    thisAudioSource.PlayOneShot(piratePhrases[Random.Range(0, piratePhrases.Length)]);
                else
                    thisAudioSource.PlayOneShot(buttonClick);
            }
        }

        IEnumerator AnimatePirates()
        {
            while (true)
            {                
                for (int i = 0; i < thisAnimators.Length; i++)
                {
                    randomAction = Random.Range(0, 4);
                    if (randomAction == 0) thisAnimators[i].SetFloat("Speed", Random.Range(0, 1f));
                    else if (randomAction == 1) thisAnimators[i].SetTrigger("Attack");
                    else if (randomAction == 2) thisAnimators[i].SetTrigger("Attack2");
                    else if (randomAction == 3) thisAnimators[i].SetTrigger("Jump");
                } 

                yield return new WaitForSeconds(Random.Range(2, 5));
            }
        }


        private void OnDisable()
        {
            StopCoroutine(AnimatePirates());
        }
    }
}
