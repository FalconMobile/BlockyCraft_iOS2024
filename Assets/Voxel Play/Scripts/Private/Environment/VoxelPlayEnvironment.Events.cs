using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        List<VoxelChunk> registeredChunkChanges;
        bool notifyChunkChanges;

        void InitNotificationManager ()
        {
            if (registeredChunkChanges == null) {
                registeredChunkChanges = new List<VoxelChunk> ();
            } else {
                registeredChunkChanges.Clear ();
            }
        }

        void NotificationManagerSend ()
        {
            bool notifyChunkChanges = captureEvents && OnChunkChanged != null;
            if (!notifyChunkChanges) return;

            int count = registeredChunkChanges.Count;
            for (int k = 0; k < count; k++) {
                OnChunkChanged (registeredChunkChanges [k]);
            }
            registeredChunkChanges.Clear ();
        }

        public void RegisterChunkChanges (List<VoxelChunk> chunks)
        {
            bool notifyChunkChanges = captureEvents && OnChunkChanged != null;
            if (!notifyChunkChanges || !captureChunkChanges) return;

            int frameCount = Time.frameCount;
            int count = chunks.Count;
            for (int k = 0; k < count; k++) {
                VoxelChunk chunk = chunks [k];
                if (chunk.modifiedFrameCount != frameCount) {
                    chunk.modifiedFrameCount = frameCount;
                    registeredChunkChanges.Add (chunk);
                }
            }
        }

        public void RegisterChunkChanges (VoxelChunk chunk)
        {
            if (!captureChunkChanges) return;
            chunk.modified = true;

            bool notifyChunkChanges = captureEvents && OnChunkChanged != null;
            if (!notifyChunkChanges) return;

            int frameCount = Time.frameCount;
            if (chunk.modifiedFrameCount != frameCount) {
                chunk.modifiedFrameCount = frameCount;
                registeredChunkChanges.Add (chunk);
            }
        }

    }

}
