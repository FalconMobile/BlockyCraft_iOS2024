using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public class BufferPool<T>
    {

        struct BufferEntry
        {
            public List<T> buffer;
            public bool inUse;
        }

        static BufferEntry [] buffers = new BufferEntry [16];

        public static List<T> Get ()
        {
            for (int k = 0; k < buffers.Length; k++) {
                if (!buffers [k].inUse) {
                    if (buffers[k].buffer == null) {
                        buffers [k].buffer = new List<T> ();
                    }
                    buffers [k].inUse = true;
                    return buffers [k].buffer;
                }
            }
            Debug.LogError ("Buffer pool exhausted. This shouldn't occur. Are you releasing the pool after using it?");
            return null;
        }

        public static void Release (List<T> buffer)
        {
            if (buffer == null) return;
            for (int k = 0; k < buffers.Length; k++) {
                if (buffers [k].buffer == buffer) {
                    buffer.Clear ();
                    buffers [k].inUse = false;
                    return;
                }
            }
        }


    }

}