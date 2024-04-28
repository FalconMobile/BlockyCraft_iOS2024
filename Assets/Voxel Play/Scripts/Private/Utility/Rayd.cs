using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelPlay
{
    public partial struct Rayd
    {
        private Vector3d m_Origin;
        private Vector3 m_Direction;

        // Creates a ray starting at /origin/ along /direction/.
        public Rayd (Vector3d origin, Vector3 direction)
        {
            m_Origin = origin;
            m_Direction = direction.normalized;
        }


        // The origin point of the ray.
        public Vector3d origin {
            get { return m_Origin; }
            set { m_Origin = value; }
        }

        // The direction of the ray.
        public Vector3 direction {
            get { return m_Direction; }
            set { m_Direction = value.normalized; }
        }

        // Returns a point at /distance/ units along the ray.
        public Vector3d GetPoint (float distance)
        {
            return m_Origin + m_Direction * distance;
        }

        public override string ToString ()
        {
            return ToString (null);
        }

        public string ToString (string format)
        {
            if (string.IsNullOrEmpty (format))
                format = "F1";
            return string.Format ("Origin: {0}, Dir: {1}", m_Origin.ToString (format), m_Direction.ToString (format));
        }


        [MethodImpl (256)]
        public static implicit operator Rayd (Ray worldSpaceRay)
        {
            return new Rayd (worldSpaceRay.origin, worldSpaceRay.direction);
        }


        public bool Intersects (Bounds bounds)
        {
            double tmin, tmax, tymin, tymax, tzmin, tzmax;

            double invDirX = 1.0 / direction.x;
            double invDirY = 1.0 / direction.y;
            double invDirZ = 1.0 / direction.z;

            double signX = invDirX < 0 ? 1 : 0;
            double signY = invDirY < 0 ? 1 : 0;
            double signZ = invDirZ < 0 ? 1 : 0;

            tmin = ((signX <= double.Epsilon ? bounds.min.x : bounds.max.x) - origin.x) * invDirX;
            tmax = ((signX <= double.Epsilon ? bounds.max.x : bounds.min.x) - origin.x) * invDirX;
            tymin = ((signY <= double.Epsilon ? bounds.min.y : bounds.max.y) - origin.y) * invDirY;
            tymax = ((signY <= double.Epsilon ? bounds.max.y : bounds.min.y) - origin.y) * invDirY;

            if ((tmin > tymax) || (tymin > tmax)) {
                return false;
            }

            if (tymin > tmin) {
                tmin = tymin;
            }

            if (tymax < tmax) {
                tmax = tymax;
            }

            tzmin = ((signZ <= double.Epsilon ? bounds.min.z : bounds.max.z) - origin.z) * invDirZ;
            tzmax = ((signZ <= double.Epsilon ? bounds.max.z : bounds.min.z) - origin.z) * invDirZ;

            if ((tmin > tzmax) || (tzmin > tmax)) {
                return false;
            }

            return true;
        }
    }
}

