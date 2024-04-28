using TMPro;
using UI.PiratesOfVoxel.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class Flag : MonoBehaviour
    {
        [SerializeField] private TMP_Text _languageText;

        private string _languageName;
        private Button _button;

        public void Init(LocalizationElements elements)
        {
            _button = GetComponent<Button>();

            _languageText.text = elements.LanguageVisualName;
            _languageName = elements.LanguageName;
        }

        public void Select()
        {
            _button.interactable = false;
            Localization.language = _languageName;
            LanguagesSettings.OnChangeLanguage();
        }

        public void Unselect()
        {
            _button.interactable = true;
        }
    }
}