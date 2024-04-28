using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DedicatedServerMessage : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject canvasRoot = new GameObject();
        canvasRoot.name = "DedicatedServerMessageCanvas";
        Canvas tempCanvas = canvasRoot.gameObject.AddComponent<Canvas>();
        tempCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRoot.gameObject.AddComponent<CanvasScaler>();
        canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
        canvasRoot.GetComponent<GraphicRaycaster>().enabled = false;
        // Make Sub BG
        GameObject Subtitles = new GameObject();
        Subtitles.name = "DedicatedServerMessage";
        // Add Image component
        Image tempImage = Subtitles.AddComponent<Image>();
        Subtitles.transform.SetParent(canvasRoot.transform, false);
        // Make Correct Rect
        tempImage.rectTransform.anchorMin = new Vector2(.01f, .01f);
        tempImage.rectTransform.anchorMax = new Vector2(.99f, .20f);
        tempImage.rectTransform.offsetMin = Vector2.zero;
        tempImage.rectTransform.offsetMax = Vector2.zero;
        tempImage.color = Color.black;
        // Make Text Object
        GameObject tempText = new GameObject();
        tempText.name = "MessageText";
        Text SubtitlesText = tempText.AddComponent<Text>();
        SubtitlesText.transform.SetParent(tempImage.transform, false);
        // Make Correct Rect
        SubtitlesText.rectTransform.anchorMin = new Vector2(.02f, .02f);
        SubtitlesText.rectTransform.anchorMax = new Vector2(.98f, .98f);
        SubtitlesText.rectTransform.offsetMin = Vector2.zero;
        SubtitlesText.rectTransform.offsetMax = Vector2.zero;
        SubtitlesText.alignment = TextAnchor.MiddleCenter;
        SubtitlesText.color = Color.white;
        SubtitlesText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        SubtitlesText.resizeTextForBestFit = true;
        SubtitlesText.text = "This is a dedicated server build, meaning only the server logic is updated, which is why you can't see anything. This is normal, if you didn't want this press 'Escape' and 'Back to Lobby'";
    }
}