using UnityEngine;
using UnityEngine.UI;

namespace UI {

    /// <summary>
    /// Used to animate backgroun in the main menu
    /// </summary>
    public class BackgroundAnimator : MonoBehaviour {

        public Image backgroundImage;

        public float scale = 0.9f;
        public float scrollSpeed = 0.005f;
        public float scrollAmplitude = 2f;
        public float phase;

        void Start() {
            // to avoid modifying the material asset
            backgroundImage.material = Instantiate(backgroundImage.material);
            backgroundImage.material.mainTextureScale = new Vector2(scale, scale);
        }

        void Update() {
            float dx = Mathf.Sin(Time.time * scrollSpeed + phase) * scrollAmplitude;
            float dy = Mathf.Cos(Time.time * scrollSpeed * 0.5f + phase) * scrollAmplitude;
            backgroundImage.material.mainTextureOffset = new Vector2(dx, dy);
        }
    }

}