using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.PiratesOfVoxel.Localize
{
    [ExecuteInEditMode]
   // [RequireComponent(typeof(TextMeshProUGUI))]
    public class Localize : MonoBehaviour
    {
        [FormerlySerializedAs("key")] [SerializeField] private string _key;
        private bool _started;
        private TextMeshProUGUI _text;
        private Text _textUnityEngineUI;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _textUnityEngineUI = GetComponent<Text>();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            LanguagesSettings.OnLanguageChanged += ApplyLocalizationProUGUI;
            
            if (_started)
            {
                ApplyLocalizationProUGUI();
            }
        }

        private void OnDisable()
        {
            LanguagesSettings.OnLanguageChanged += ApplyLocalizationProUGUI;
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            _started = true;
            ApplyLocalizationProUGUI();
        }

        private void ApplyLocalizationProUGUI()
        {
            if (_text == null)
            {
                ApplyLocalizationTextUi();
                return;
            }

            // If no localization key has been specified, use the label's text as the key
            if (string.IsNullOrEmpty(_key))
            {
                _key = _text.text;
            }

            if (!string.IsNullOrEmpty(_key))
            {
                _text.text = Localization.Localization.Get(_key);
            }
        }
        
        private void ApplyLocalizationTextUi()
        {
            if (_textUnityEngineUI == null)
            {
                return;
            }

            // If no localization key has been specified, use the label's text as the key
            if (string.IsNullOrEmpty(_key))
            {
                _key = _textUnityEngineUI.text;
            }

            if (!string.IsNullOrEmpty(_key))
            {
                _textUnityEngineUI.text = Localization.Localization.Get(_key);
            }
        }
#if UNITY_EDITOR
        public string SetValue
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (_text != null)
                    {
                        _text.SetAllDirty();
                        _text.SetText(value);
                    }
                }
            }
        }
#endif
    }
}
