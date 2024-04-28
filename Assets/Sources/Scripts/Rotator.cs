using UnityEngine;

namespace ZeichenKraftwerk
{
    public sealed class Rotator : MonoBehaviour
    {
        public Vector3 eulersPerSecond = Vector3.zero;

        private void FixedUpdate()
        {
            transform.Rotate(eulersPerSecond * Time.fixedDeltaTime);
        }
    }
}