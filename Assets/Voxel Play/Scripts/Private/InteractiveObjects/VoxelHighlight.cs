//#define USE_HIGHLIGHT_PLUS

using UnityEngine;

#if USE_HIGHLIGHT_PLUS
using HighlightPlus;
#endif

namespace VoxelPlay
{
    public class VoxelHighlight : MonoBehaviour
    {

#if USE_HIGHLIGHT_PLUS
        public HighlightProfile highlightProfile;

        HighlightEffect effect;
        MeshRenderer mr;

        private void OnEnable ()
        {
            effect = GetComponent<HighlightEffect> () ?? gameObject.AddComponent<HighlightEffect> ();
            if (highlightProfile != null) {
                effect.profile = highlightProfile;
            }
            mr = GetComponent<MeshRenderer> ();
            mr.enabled = false;
            effect.ignoreObjectVisibility = true;

        }
#endif

        public void SetTarget (Transform target)
        {
#if USE_HIGHLIGHT_PLUS
            if (effect != null) {
                if (target == null) {
                    target = transform;
                }
                if (target != effect.target) {
                    effect.SetTarget (target);
                }
            }
#endif
        }



        public void SetActive (bool visible)
        {
#if USE_HIGHLIGHT_PLUS
            if (effect != null) {
                effect.SetHighlighted (visible);
            }
#endif
            gameObject.SetActive (visible);
        }

    }

}