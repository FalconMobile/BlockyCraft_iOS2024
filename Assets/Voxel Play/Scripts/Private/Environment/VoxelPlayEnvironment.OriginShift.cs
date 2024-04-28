using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelPlay
{

    public delegate bool OriginShiftEvent (Vector3 shift);

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        public static Vector3d worldPivot;
        public bool useOriginShift;
        public int originShiftDistanceThreshold = 1000;

        readonly static List<Transform> originShiftTransforms = new List<Transform> ();

        public void RegisterOriginShiftTransform (Transform t)
        {
            if (t == null) return;

            // distance anchor is already shifted automatically by the system so skip it to avoid issues
            if (distanceAnchor != null && distanceAnchor.root == t) return;

            if (t.root == worldRoot) return;

            if (!originShiftTransforms.Contains (t)) {
                originShiftTransforms.Add (t);
            }
        }

        public void UnregisterOriginShiftTransform (Transform t)
        {
            if (originShiftTransforms.Contains (t)) {
                originShiftTransforms.Remove (t);
            }
        }

        void InitOriginShift()
        {
            worldPivot = Vector3d.zero;
            worldRoot.position = Misc.vector3zero;
        }

        void OriginShiftUpdate ()
        {
            int fx = (int)(currentAnchorPosWS.x / originShiftDistanceThreshold);
            int fz = (int)(currentAnchorPosWS.z / originShiftDistanceThreshold);
            if (fx == 0 && fz == 0) return;

            Vector3 pivot = new Vector3 (currentAnchorPosWS.x, 0, currentAnchorPosWS.z);

            if (OnOriginPreShift != null) {
                if (!OnOriginPreShift (pivot)) return;
            }

            OriginShiftApply (pivot);
        }

        public void OriginShiftApply(Vector3 pivot) { 

            // Shift anchor
            distanceAnchor.root.position -= pivot;

            // Shift world
            worldPivot -= pivot;
            worldRoot.position = worldPivot.vector3;

            // Shift anything else
            int tcount = originShiftTransforms.Count;
            for (int k = 0; k < tcount; k++) {
                Transform t = originShiftTransforms [k];
                if (t != null && t.root != worldRoot) { // shift object but only if it's not a child of world!
                    t.position -= pivot;
                }
            }

            currentAnchorPosWS = distanceAnchor.position;
            currentAnchorPos = currentAnchorPosWS;

            if (OnOriginPostShift != null) {
                OnOriginPostShift (pivot);
            }

        }
    }

}
