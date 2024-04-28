using System;
using UnityEngine;

namespace VoxelPlay
{

    public interface IVoxelPlayCharacterController
    {

        /// <summary>
        /// The character controller should return a VoxelHitInfo containing data about the current highlighted object (if any)
        /// </summary>
        VoxelHitInfo crosshairHitInfo { get; }

        /// <summary>
        /// This method is called by VP just after loading a saved game and updating the position/rotation of the character controller.
        /// </summary>
        void UpdateLook ();

        /// <summary>
        /// Basic boilerplate so VP can interact with controller gameobject, like getting its position, etc.
        /// </summary>
        Transform transform { get; }
        T GetComponentInChildren<T> ();
        GameObject gameObject { get; }

        /// <summary>
        /// Returns true if the character controller is initialized and ready to use
        /// </summary>
        bool isReady { get; }

    }
}
