using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SRDebugger.UI.Controls
{
    public class CopyLog : MonoBehaviour
    {
        [SerializeField] private Text _contentText;
        public void CopySelected()
        {
            GUIUtility.systemCopyBuffer = _contentText.text;
        }
    }

}