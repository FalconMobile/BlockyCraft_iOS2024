using UnityEngine;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace VoxelPlay
{
    [StructLayout (LayoutKind.Sequential)]
    public partial struct Boundsd
    {
        private Vector3d m_Center;
        private Vector3d m_Extents;

        public static Boundsd empty = new Boundsd ();

        // Creates new Bounds with a given /center/ and total /size/. Bound ::ref::extents will be half the given size.
        public Boundsd (Vector3d center, Vector3d size)
        {
            m_Center = center;
            m_Extents = size * 0.5;
        }

        // Creates new Bounds with a given /center/ and total /size/. Bound ::ref::extents will be half the given size.
        public Boundsd (Vector3d center, Vector3 size)
        {
            m_Center = center;
            m_Extents = new Vector3d(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
        }

        [MethodImpl (256)]
        public static implicit operator Bounds(Boundsd bb)
        {
            return new Bounds (bb.center, bb.size);
        }

        [MethodImpl(256)]
        public static implicit operator Boundsd(Bounds bb) {
            return new Boundsd(bb.center, bb.size);
        }

        // used to allow Bounds to be used as keys in hash tables
        public override int GetHashCode ()
        {
            return center.GetHashCode () ^ (extents.GetHashCode () << 2);
        }

        // also required for being able to use Vector4s as keys in hash tables
        public override bool Equals (object other)
        {
            if (!(other is Boundsd)) return false;

            return Equals ((Boundsd)other);
        }

        public bool Equals (Boundsd other)
        {
            return center.Equals (other.center) && extents.Equals (other.extents);
        }

        // The center of the bounding box.
        public Vector3d center { get { return m_Center; } set { m_Center = value; } }

        // The total size of the box. This is always twice as large as the ::ref::extents.
        public Vector3d size { get { return m_Extents * 2.0; } set { m_Extents = value * 0.5; } }

        // The extents of the box. This is always half of the ::ref::size.
        public Vector3d extents { get { return m_Extents; } set { m_Extents = value; } }

        // The minimal point of the box. This is always equal to ''center-extents''.
        public Vector3d min { get { return center - extents; } set { SetMinMax (value, max); } }

        // The maximal point of the box. This is always equal to ''center+extents''.
        public Vector3d max { get { return center + extents; } set { SetMinMax (min, value); } }

        //*undoc*
        public static bool operator == (Boundsd lhs, Boundsd rhs)
        {
            // Returns false in the presence of NaN values.
            return (lhs.center == rhs.center && lhs.extents == rhs.extents);
        }

        //*undoc*
        public static bool operator != (Boundsd lhs, Boundsd rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Sets the bounds to the /min/ and /max/ value of the box.
        public void SetMinMax (Vector3d min, Vector3d max)
        {
            extents = (max - min) * 0.5;
            center = min + extents;
        }

        // Grows the Bounds to include the /point/.
        public void Encapsulate (Vector3d point)
        {
            SetMinMax (Vector3d.Min (min, point), Vector3d.Max (max, point));
        }

        // Grows the Bounds to include the /Bounds/.
        public void Encapsulate (Boundsd bounds)
        {
            Encapsulate (bounds.center - bounds.extents);
            Encapsulate (bounds.center + bounds.extents);
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        public void Expand (double amount)
        {
            amount *= .5;
            extents += new Vector3d (amount, amount, amount);
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        public void Expand (Vector3d amount)
        {
            extents += amount * 0.5;
        }

        // Does another bounding box intersect with this bounding box?
        public bool Intersects (Boundsd bounds)
        {
            return (min.x <= bounds.max.x) && (max.x >= bounds.min.x) &&
                (min.y <= bounds.max.y) && (max.y >= bounds.min.y) &&
                (min.z <= bounds.max.z) && (max.z >= bounds.min.z);
        }


        override public string ToString ()
        {
            return ToString (null);
        }

        // Returns a nicely formatted string for the bounds.
        public string ToString (string format)
        {
            if (string.IsNullOrEmpty (format))
                format = "F1";
            return string.Format ("Center: {0}, Extents: {1}", m_Center.ToString (format), m_Extents.ToString (format));
        }
    }
}

