using System;
using System.Collections.Generic;
using UI.PiratesOfVoxel.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class LanguagesSettings : MonoBehaviour
    {
        [SerializeField] private List<LocalizationElements> _localizationElements;
        [SerializeField] private Flag _flagTemplate;
        [SerializeField] private GridLayoutGroup _grid;

        public static event Action OnLanguageChanged = delegate { };

        private List<Flag> _flags;

        private void Start() => Init();
        
        public void Init()
        {
            _flagTemplate.gameObject.SetActive(false);

            _flags = new List<Flag>();
            foreach (LocalizationElements element in _localizationElements)
            {
                Flag newFlag = Instantiate(_flagTemplate, _grid.transform);
                newFlag.gameObject.SetActive(true);
                newFlag.Init(element);
                _flags.Add(newFlag);
                if (element.LanguageName == Localization.language)
                {
                    newFlag.Select();
                }
            }
        }

        public void SelectFlag(Flag flag)
        {
            foreach (var flagElement in _flags)
            {
                flagElement.Unselect();
            }
            flag.Select();
            OnLanguageChanged();
        }

        public static void OnChangeLanguage()
        {
            OnLanguageChanged();
        }
    }

    [Serializable]
    public struct LocalizationElements
    {
        public string LanguageCode;
        public string LanguageName;
        public string LanguageVisualName;
    }
}
