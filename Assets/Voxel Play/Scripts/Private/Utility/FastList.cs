using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelPlay
{

    // Fast index-based list
    public class FastList<T>
    {
        public T [] values;
        public int count;
        int fillCount;

        public FastList (int initialCapacity = 4)
        {
            values = new T [initialCapacity];
            count = 0;
            fillCount = 0;
        }

        public void Clear ()
        {
            count = 0;
        }

        public int Add (T value)
        {
            if (count >= values.Length) {
                Array.Resize (ref values, count * 2);
            }
            int index = count++;
            values [index] = value;
            fillCount++;
            return index;
        }

        public bool Contains (T value)
        {
            for (int k = 0; k < count; k++) {
                if (values [k] != null && values [k].Equals (value))
                    return true;
            }
            return false;
        }

        public int IndexOf (T value)
        {
            for (int k = 0; k < count; k++) {
                if (values [k] != null && values [k].Equals (value))
                    return k;
            }
            return -1;
        }

        public bool RemoveAt (int index)
        {
            if (index < 0 || index >= count)
                return false;
            for (int k = index; k < count - 1; k++) {
                values [k] = values [k + 1];
            }
            count--;
            return true;
        }

        public bool Remove (T value)
        {
            int k = IndexOf (value);
            if (k < 0) {
                return false;
            }
            return RemoveAt (k);
        }


        /// <summary>
        /// Removes the last added element
        /// </summary>
        public bool RemoveLast ()
        {
            if (count <= 0)
                return false;
            --count;
            return true;
        }


        /// <summary>
        /// Removes first n elements
        /// </summary>
        /// <param name="n">Number of elements to remove</param>
        public void RemoveFirst (int n)
        {
            if (n <= 0) return;
            if (n > count) n = count;
            for (int k = n; k < count; k++) {
                values [k - n] = values [k];
            }
            count -= n;
        }

        public T FetchDirty ()
        {
            if (count >= fillCount) {
                return default;
            }
            return values [count++];
        }

        public T [] ToArray ()
        {
            T [] a = new T [count];
            Array.Copy (values, a, count);
            return a;
        }
    }

}
