using SRDebugger.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SRDebugger.UI.Other
{
    public class ErrorNotifier : MonoBehaviour
    {
        public bool IsVisible
        {
            get { return enabled; }
        }

        private const float DisplayTime = 6;

        [SerializeField]
        private Animator _animator = null;
        [SerializeField]
        private Text _text = null;


        private int _triggerHash;

        private float _hideTime;
        private bool _isShowing;

        private bool _queueWarning;

        void Awake()
        {
            _triggerHash = Animator.StringToHash("Display");
        }

        public void ShowErrorWarning(IConsoleService console)
        {
            _queueWarning = true;
            ShowText(console);
        }

        private void ShowText(IConsoleService console)
        {
            var lastItem = console.Entries[console.Entries.Count - 1];
            _text.text = lastItem.Message;
        }

        void Update()
        {
            if (_queueWarning)
            {
                _hideTime = Time.realtimeSinceStartup + DisplayTime;

                if (!_isShowing)
                {
                    _isShowing = true;
                    _animator.SetBool(_triggerHash, true);
                }

                _queueWarning = false;
            }

            if (_isShowing && Time.realtimeSinceStartup > _hideTime)
            {
                _animator.SetBool(_triggerHash, false);
                _isShowing = false;
            }
        }
    }
}