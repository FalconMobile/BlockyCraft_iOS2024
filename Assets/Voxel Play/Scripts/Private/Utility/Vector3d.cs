using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelPlay
{
    public struct Vector3d
    {
        const double EPSILON_MAGNITUDE = 9.99999974737875E-06;    // ~= 1e-5
        const double EPSILON_MAGNITUDE_SQR = EPSILON_MAGNITUDE * EPSILON_MAGNITUDE;
        const double EPSILON_DOT_PRODUCT = 1.40129846432482E-45d;

        public double x;
        public double y;
        public double z;

        public double this [int index] {
            get {
                switch (index) {
                case 0:
                    return this.x;
                case 1:
                    return this.y;
                case 2:
                    return this.z;
                default:
                    throw new IndexOutOfRangeException ("Invalid index!");
                }
            }
            set {
                switch (index) {
                case 0:
                    this.x = value;
                    break;
                case 1:
                    this.y = value;
                    break;
                case 2:
                    this.z = value;
                    break;
                default:
                    throw new IndexOutOfRangeException ("Invalid Vector3d index!");
                }
            }
        }

        public Vector3d normalized {
            get {
                return Vector3d.Normalize (this);
            }
        }

        public double magnitude {
            get {
                return Math.Sqrt (this.x * this.x + this.y * this.y + this.z * this.z);
            }
        }

        public double sqrMagnitude {
            get {
                return this.x * this.x + this.y * this.y + this.z * this.z;
            }
        }

        public static Vector3d zero = new Vector3d (0d, 0d, 0d);

        public static Vector3d one = new Vector3d (1d, 1d, 1d);

        public static Vector3d forward = new Vector3d(0d, 0d, 1d);
            
        public static Vector3d back = new Vector3d(0d, 0d, -1d);

        public static Vector3d up = new Vector3d (0d, 1d, 0d);

        public static Vector3d down = new Vector3d (0d, -1d, 0d);

        public static Vector3d left = new Vector3d (-1d, 0d, 0d);

        public static Vector3d right = new Vector3d (1d, 0d, 0d);

        public Vector3d (double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3d (float x, float y, float z)
        {
            this.x = (double)x;
            this.y = (double)y;
            this.z = (double)z;
        }

        public Vector3d (Vector3 v3)
        {
            this.x = (double)v3.x;
            this.y = (double)v3.y;
            this.z = (double)v3.z;
        }

        public Vector3d (double x, double y)
        {
            this.x = x;
            this.y = y;
            this.z = 0d;
        }

        public Vector3 vector3 => new Vector3 ((float)x, (float)y, (float)z);


        public static Vector3d operator + (Vector3d a, Vector3d b)
        {
            return new Vector3d (a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static Vector3d operator + (Vector3 a, Vector3d b)
        {
            return new Vector3d ((double)a.x + b.x, (double)a.y + b.y, (double)a.z + b.z);
        }
        public static Vector3d operator + (Vector3d a, Vector3 b)
        {
            return new Vector3d (a.x + (double)b.x, a.y + (double)b.y, a.z + (double)b.z);
        }

        public static Vector3d operator - (Vector3d a, Vector3d b)
        {
            return new Vector3d (a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static Vector3d operator - (Vector3 a, Vector3d b)
        {
            return new Vector3d ((double)a.x - b.x, (double)a.y - b.y, (double)a.z - b.z);
        }
        public static Vector3d operator - (Vector3d a, Vector3 b)
        {
            return new Vector3d (a.x - (double)b.x, a.y - (double)b.y, a.z - (double)b.z);
        }

        public static Vector3d operator - (Vector3d a)
        {
            return new Vector3d (-a.x, -a.y, -a.z);
        }

        public static Vector3d operator * (Vector3d a, double d)
        {
            return new Vector3d (a.x * d, a.y * d, a.z * d);
        }

        public static Vector3d operator * (double d, Vector3d a)
        {
            return new Vector3d (a.x * d, a.y * d, a.z * d);
        }

        public static Vector3d operator / (Vector3d a, double d)
        {
            return new Vector3d (a.x / d, a.y / d, a.z / d);
        }

        public static bool operator == (Vector3d lhs, Vector3d rhs)
        {
            // Implementation similar to Vector3
            return Vector3d.SqrMagnitude (lhs - rhs) < EPSILON_MAGNITUDE_SQR;
        }
        public static bool operator == (Vector3 lhs, Vector3d rhs)
        {
            return Vector3d.SqrMagnitude (lhs - rhs) < EPSILON_MAGNITUDE_SQR;
        }
        public static bool operator == (Vector3d lhs, Vector3 rhs)
        {
            return Vector3d.SqrMagnitude (lhs - rhs) < EPSILON_MAGNITUDE_SQR;
        }

        public static bool operator != (Vector3d lhs, Vector3d rhs)
        {
            return !(lhs == rhs);
        }
        public static bool operator != (Vector3 lhs, Vector3d rhs)
        {
            return !(lhs == rhs);
        }
        public static bool operator != (Vector3d lhs, Vector3 rhs)
        {
            return !(lhs == rhs);
        }

        public static Vector3d Lerp (Vector3d from, Vector3d to, double t)
        {
            t = Mathd.Clamp01 (t);
            return new Vector3d (from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t, from.z + (to.z - from.z) * t);
        }


        public static Vector3d MoveTowards (Vector3d current, Vector3d target, double maxDistanceDelta)
        {
            Vector3d vector3 = target - current;
            double magnitude = vector3.magnitude;
            if (magnitude <= maxDistanceDelta || magnitude == 0.0d) {
                return target;
            } else {
                return current + vector3 / magnitude * maxDistanceDelta;
            }
        }


        public static Vector3d SmoothDamp (Vector3d current, Vector3d target, ref Vector3d currentVelocity, double smoothTime, double maxSpeed)
        {
            double deltaTime = (double)Time.deltaTime;
            return Vector3d.SmoothDamp (current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static Vector3d SmoothDamp (Vector3d current, Vector3d target, ref Vector3d currentVelocity, double smoothTime)
        {
            double deltaTime = (double)Time.deltaTime;
            double maxSpeed = double.PositiveInfinity;
            return Vector3d.SmoothDamp (current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static Vector3d SmoothDamp (Vector3d current, Vector3d target, ref Vector3d currentVelocity, double smoothTime, double maxSpeed, double deltaTime)
        {
            smoothTime = Mathd.Max (0.0001d, smoothTime);
            double num1 = 2d / smoothTime;
            double num2 = num1 * deltaTime;
            double num3 = (1.0d / (1.0d + num2 + 0.479999989271164d * num2 * num2 + 0.234999999403954d * num2 * num2 * num2));
            Vector3d vector = current - target;
            Vector3d vector3_1 = target;
            double maxLength = maxSpeed * smoothTime;
            Vector3d vector3_2 = Vector3d.ClampMagnitude (vector, maxLength);
            target = current - vector3_2;
            Vector3d vector3_3 = (currentVelocity + num1 * vector3_2) * deltaTime;
            currentVelocity = (currentVelocity - num1 * vector3_3) * num3;
            Vector3d vector3_4 = target + (vector3_2 + vector3_3) * num3;
            if (Vector3d.Dot (vector3_1 - current, vector3_4 - vector3_1) > 0.0) {
                vector3_4 = vector3_1;
                currentVelocity = (vector3_4 - vector3_1) / deltaTime;
            }
            return vector3_4;
        }

        public void Set (double new_x, double new_y, double new_z)
        {
            this.x = new_x;
            this.y = new_y;
            this.z = new_z;
        }

        public static Vector3d Scale (Vector3d a, Vector3d b)
        {
            return new Vector3d (a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public void Scale (Vector3d scale)
        {
            this.x *= scale.x;
            this.y *= scale.y;
            this.z *= scale.z;
        }

        public static Vector3d Cross (Vector3d lhs, Vector3d rhs)
        {
            return new Vector3d (lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x);
        }

        public override int GetHashCode ()
        {
            return this.x.GetHashCode () ^ this.y.GetHashCode () << 2 ^ this.z.GetHashCode () >> 2;
        }

        public override bool Equals (object other)
        {
            if (!(other is Vector3d)) {
                return false;
            }
            Vector3d vector3d = (Vector3d)other;
            if (this.x.Equals (vector3d.x) && this.y.Equals (vector3d.y)) {
                return this.z.Equals (vector3d.z);
            } else {
                return false;
            }
        }

        public static Vector3d Reflect (Vector3d inDirection, Vector3d inNormal)
        {
            return -2d * Vector3d.Dot (inNormal, inDirection) * inNormal + inDirection;
        }

        public static Vector3d Normalize (Vector3d value)
        {
            double num = Vector3d.Magnitude (value);
            if (num > EPSILON_MAGNITUDE) {
                return value / num;
            } else {
                return Vector3d.zero;
            }
        }

        public void Normalize ()
        {
            double num = Vector3d.Magnitude (this);
            if (num > EPSILON_MAGNITUDE) {
                this = this / num;
            } else {
                this = Vector3d.zero;
            }
        }

        public override string ToString ()
        {
            return "(" + this.x + ", " + this.y + ", " + this.z + ")";
        }

        public string ToString (string format)
        {
            return ToString (format, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString (string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty (format)) {
                format = "F3";
            }
            return string.Format ("({0}, {1}, {2})", x.ToString (format, formatProvider), y.ToString (format, formatProvider), z.ToString (format, formatProvider));
        }

        public static double Dot (Vector3d lhs, Vector3d rhs)
        {
            return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
        }

        public static Vector3d Project (Vector3d vector, Vector3d onNormal)
        {
            double num = Vector3d.Dot (onNormal, onNormal);
            if (num < EPSILON_DOT_PRODUCT) {
                return Vector3d.zero;
            } else {
                return onNormal * Vector3d.Dot (vector, onNormal) / num;
            }
        }

        public static Vector3d Exclude (Vector3d excludeThis, Vector3d fromThat)
        {
            return fromThat - Vector3d.Project (fromThat, excludeThis);
        }

        public static double Angle (Vector3d from, Vector3d to)
        {
            return Mathd.Acos (Mathd.Clamp (Vector3d.Dot (from.normalized, to.normalized), -1d, 1d)) * 57.29578d;
        }

        public static double Distance (Vector3d a, Vector3d b)
        {
            Vector3d vector3d = new Vector3d (a.x - b.x, a.y - b.y, a.z - b.z);
            return Math.Sqrt (vector3d.x * vector3d.x + vector3d.y * vector3d.y + vector3d.z * vector3d.z);
        }

        public static Vector3d ClampMagnitude (Vector3d vector, double maxLength)
        {
            if (vector.sqrMagnitude > maxLength * maxLength) {
                return vector.normalized * maxLength;
            } else {
                return vector;
            }
        }

        public static double Magnitude (Vector3d a)
        {
            return Math.Sqrt (a.x * a.x + a.y * a.y + a.z * a.z);
        }

        public static double SqrMagnitude (Vector3d a)
        {
            return a.x * a.x + a.y * a.y + a.z * a.z;
        }

        public static Vector3d Min (Vector3d lhs, Vector3d rhs)
        {
            return new Vector3d (Mathd.Min (lhs.x, rhs.x), Mathd.Min (lhs.y, rhs.y), Mathd.Min (lhs.z, rhs.z));
        }

        public static Vector3d Max (Vector3d lhs, Vector3d rhs)
        {
            return new Vector3d (Mathd.Max (lhs.x, rhs.x), Mathd.Max (lhs.y, rhs.y), Mathd.Max (lhs.z, rhs.z));
        }

        [Obsolete ("Use Vector3d.Angle instead. AngleBetween uses radians instead of degrees and was deprecated for this reason")]
        public static double AngleBetween (Vector3d from, Vector3d to)
        {
            return Mathd.Acos (Mathd.Clamp (Vector3d.Dot (from.normalized, to.normalized), -1d, 1d));
        }

        [MethodImpl (256)]
        public static implicit operator Vector3d (Vector3 worldSpacePosition)
        {
            return new Vector3d ((double)worldSpacePosition.x - VoxelPlayEnvironment.worldPivot.x, worldSpacePosition.y, (double)worldSpacePosition.z - VoxelPlayEnvironment.worldPivot.z);
        }

        [MethodImpl (256)]
        public static implicit operator Vector3 (Vector3d voxelSpacePosition)
        {
            return new Vector3 ((float)(voxelSpacePosition.x + VoxelPlayEnvironment.worldPivot.x), (float)voxelSpacePosition.y, (float)(voxelSpacePosition.z + VoxelPlayEnvironment.worldPivot.z));
        }
    }

    public static class Vector3Extensions
    {
        public static Vector3d ToVector3d (this Vector3 v)
        {
            return new Vector3d (v.x, v.y, v.z);
        }
    }
}
