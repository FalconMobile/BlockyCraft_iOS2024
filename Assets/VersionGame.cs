using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VersionGame : MonoBehaviour
{
   [SerializeField] private Text GameVersion;
    void Start()
    {
        GameVersion.text = Application.version;
    }
}
