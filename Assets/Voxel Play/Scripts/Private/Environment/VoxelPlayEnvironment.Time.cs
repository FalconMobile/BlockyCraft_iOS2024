using System;
using UnityEngine;

namespace VoxelPlay {


    public partial class VoxelPlayEnvironment : MonoBehaviour {

        [NonSerialized]
        public Quaternion sunStartRotation;
        float sunStartDirectionTimestamp;

        void InitTime() {
            if (sun == null) return;
            if (world.setTimeAndAzimuth) {
                SetTimeOfDay(world.timeOfDay, world.azimuth);
            } else {
                sunStartRotation = sun.transform.rotation;
            }
        }

        void UpdateSunRotation() {
            if (applicationIsPlaying && world.dayCycleSpeed != 0) {
                if (sun == null) return;
                Transform t = sun.transform;
                float elapsed = Time.time - sunStartDirectionTimestamp;
                t.rotation = sunStartRotation;
                t.Rotate(new Vector3(1f, 0.2f, 0) * (elapsed * world.dayCycleSpeed));
            }

        }

    }

}
