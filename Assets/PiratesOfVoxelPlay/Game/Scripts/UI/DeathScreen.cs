using System.Collections;
using System.Collections.Generic;
using UI.PiratesOfVoxel.Localization;
using UnityEngine;

namespace UI
{
    public class DeathScreen : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _freeSpawnText;
        [SerializeField] private IngameMenu _ingameMenu;
        [SerializeField] private GameObject _adsRespawnButton;
        [SerializeField] private GameObject _freeRespawnButton;


        private void OnEnable()
        {
            int current = FreeRespawnesController.GetFreeSpawns();
            _adsRespawnButton.SetActive(current <= 0);
            _freeRespawnButton.SetActive(current > 0);
            _freeSpawnText.text = string.Format(Localization.Get("free_respawn"), current);
        }

        public void FreeRespawnPlayer()
        {
            _ingameMenu.FreeRespawnPlayer();
            FreeRespawnesController.ConsumeFreeSpawn();
        }

        public void RespawnPlayer()
        {
            _ingameMenu.RespawnPlayer();
        }

    }
}
