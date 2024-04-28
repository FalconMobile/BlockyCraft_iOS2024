using System.Collections;
using System.Collections.Generic;
using UI.PiratesOfVoxel.Localization;
using UnityEngine;

namespace UI
{
    public class RateUsPanel : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _description;


        private const int ADD_COUNT = 1;
        private const string RATED_KEY = "Rated";
        private const string OPEN_COUNT_KEY = "RateOpenCount";

        public void ShowRateUs()
        {
            int currentCount = PlayerPrefs.GetInt(OPEN_COUNT_KEY);
            currentCount++;
            PlayerPrefs.SetInt(OPEN_COUNT_KEY, currentCount);

            if (PlayerPrefs.GetInt(RATED_KEY) == 1)
            {
                return;
            }

            if (currentCount < 2)
            {
                return;
            }

            _description.text = string.Format(Localization.Get("rate_description"), ADD_COUNT);
            gameObject.SetActive(true);
        }

        public void RateApp()
        {
            Application.OpenURL(ConfigLoader.ShopLink);
            FreeRespawnesController.AddFreeSpawns(ADD_COUNT);
            PlayerPrefs.SetInt(RATED_KEY, 1);
            gameObject.SetActive(false);
        }

        public void Later()
        {
            gameObject.SetActive(false);
        }
    }
}
