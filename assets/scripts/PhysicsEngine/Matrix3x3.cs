using UnityEngine;
using System.Collections;

namespace Galileo
{
    public class Matrix3x3
    {
        public float m00; public float m01; public float m02;
        public float m10; public float m11; public float m12;
        public float m20; public float m21; public float m22;

        public static Matrix3x3 identity
        {
            get { return new Matrix3x3(); }
        }

        public Matrix3x3 transpose
        {
            get { return new Matrix3x3(m00, m10, m20, m01, m11, m21, m02, m12, m22); }
        }

        public Matrix3x3 inverse
        {
            get { return ComputeInverse(); }
        }

        public Matrix3x3()
        {
            m00 = 1.0f; m01 = 0.0f; m02 = 0.0f;
            m10 = 0.0f; m11 = 1.0f; m12 = 0.0f;
            m20 = 0.0f; m21 = 0.0f; m22 = 1.0f;
        }

        public Matrix3x3(float m00, float m01, float m02,
                         float m10, float m11, float m12,
                         float m20, float m21, float m22)
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02;
            this.m10 = m10; this.m11 = m11; this.m12 = m12;
            this.m20 = m20; this.m21 = m21; this.m22 = m22;
        }


        // Transforms a direction vector.
        public Vector3 MultiplyVector(Vector3 v)
        {
            return new Vector3( v.x * m00 + v.y * m01 + v.z * m02,
                                v.x * m10 + v.y * m11 + v.z * m12,
                                v.x * m20 + v.y * m21 + v.z * m22 );
        }

        // Create a matrix from a quaternion.
        public static Matrix3x3 FromQuaternion(Quaternion q)
        {
            float r = q.w;
            float x = q.x;
            float y = q.y;
            float z = q.z;
            Matrix3x3 result = new Matrix3x3();
            result.m00 = 1.0f - 2.0f * y * y - 2.0f * z * z; ;
            result.m01 = 2.0f * x * y - 2.0f * r * z;
            result.m02 = 2.0f * x * z + 2.0f * r * y;

            result.m10 = 2.0f * x * y + 2.0f * r * z;
            result.m11 = 1.0f - 2.0f * x * x - 2.0f * z * z;
            result.m12 = 2.0f * y * z - 2.0f * r * x;

            result.m20 = 2.0f * x * z - 2.0f * r * y;
            result.m21 = 2.0f * y * z + 2.0f * r * x;
            result.m22 = 1.0f - 2.0f * x * x - 2.0f * y * y;

            return result;
        }

        #region operators
        
        // Add scalar to matrix.
        public static Matrix3x3 operator+ (Matrix3x3 lhs, float rhs)
        {
            return new Matrix3x3(lhs.m00 + rhs, lhs.m01 + rhs, lhs.m02 + rhs,
                              lhs.m10 + rhs, lhs.m11 + rhs, lhs.m12 + rhs,
                              lhs.m20 + rhs, lhs.m21 + rhs, lhs.m22 + rhs);
        }

        // Add two 3x3 matrices.
        public static Matrix3x3 operator+ (Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return new Matrix3x3(lhs.m00 + rhs.m00, lhs.m01 + rhs.m01, lhs.m02 + rhs.m02,
                                 lhs.m10 + rhs.m10, lhs.m11 + rhs.m11, lhs.m12 + rhs.m12,
                                 lhs.m20 + rhs.m20, lhs.m21 + rhs.m21, lhs.m22 + rhs.m22);
        }
        
        // Subtract scalar from matrix.
        public static Matrix3x3 operator- (Matrix3x3 lhs, float rhs)
        {
            return new Matrix3x3(lhs.m00 - rhs, lhs.m01 - rhs, lhs.m02 - rhs,
                                 lhs.m10 - rhs, lhs.m11 - rhs, lhs.m12 - rhs,
                                 lhs.m20 - rhs, lhs.m21 - rhs, lhs.m22 - rhs);
        }
            
        // Subtract two 3x3 matrices.
        public static Matrix3x3 operator- (Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return new Matrix3x3(lhs.m00 - rhs.m00, lhs.m01 - rhs.m01, lhs.m02 - rhs.m02,
                                 lhs.m10 - rhs.m10, lhs.m11 - rhs.m11, lhs.m12 - rhs.m12,
                                 lhs.m20 - rhs.m20, lhs.m21 - rhs.m21, lhs.m22 - rhs.m22);
        }

        // Matrix-scalar multiplication.
        public static Matrix3x3 operator* (Matrix3x3 lhs, float rhs)
        {
            return new Matrix3x3(lhs.m00 * rhs, lhs.m01 * rhs, lhs.m02 * rhs,
                                 lhs.m10 * rhs, lhs.m11 * rhs, lhs.m12 * rhs,
                                 lhs.m20 * rhs, lhs.m21 * rhs, lhs.m22 * rhs);
        }

        // Scalar-matrix multiplication. (Same as above, really).
        public static Matrix3x3 operator* (float lhs, Matrix3x3 rhs)
        {
            return new Matrix3x3(rhs.m00 - lhs, rhs.m01 - lhs, rhs.m02 - lhs,
                                 rhs.m10 - lhs, rhs.m11 - lhs, rhs.m12 - lhs,
                                 rhs.m20 - lhs, rhs.m21 - lhs, rhs.m22 - lhs);
        }

        
        // Matrix-Matrix multiplication
        public static Matrix3x3 operator* (Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return new Matrix3x3(lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20,
                                 lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21,
                                 lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22,
                                 lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20,
                                 lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21,
                                 lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22,
                                 lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20,
                                 lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21,
                                 lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22);
        }

        // Matrix-Vector multiplication.
        public static Vector3 operator* (Matrix3x3 lhs, Vector3 rhs)
        {
            return new Vector3(lhs.m00 * rhs.x + lhs.m01 * rhs.y + lhs.m02 * rhs.z,
                               lhs.m10 * rhs.x + lhs.m11 * rhs.y + lhs.m12 * rhs.z,
                               lhs.m20 * rhs.x + lhs.m21 * rhs.y + lhs.m22 * rhs.z);
        }

        // Matrix-scalar division.
        public static Matrix3x3 operator/ (Matrix3x3 lhs, float rhs)
        {
            return new Matrix3x3(lhs.m00 / rhs, lhs.m01 / rhs, lhs.m02 / rhs,
                                 lhs.m10 / rhs, lhs.m11 / rhs, lhs.m12 / rhs,
                                 lhs.m20 / rhs, lhs.m21 / rhs, lhs.m22 / rhs);
        }

        // Matrix-Matrix division.
        public static Matrix3x3 operator/ (Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return new Matrix3x3(lhs.m00 / rhs.m00 + lhs.m01 / rhs.m10 + lhs.m02 / rhs.m20,
                                 lhs.m00 / rhs.m01 + lhs.m01 / rhs.m11 + lhs.m02 / rhs.m21,
                                 lhs.m00 / rhs.m02 + lhs.m01 / rhs.m12 + lhs.m02 / rhs.m22,
                                 lhs.m10 / rhs.m00 + lhs.m11 / rhs.m10 + lhs.m12 / rhs.m20,
                                 lhs.m10 / rhs.m01 + lhs.m11 / rhs.m11 + lhs.m12 / rhs.m21,
                                 lhs.m10 / rhs.m02 + lhs.m11 / rhs.m12 + lhs.m12 / rhs.m22,
                                 lhs.m20 / rhs.m00 + lhs.m21 / rhs.m10 + lhs.m22 / rhs.m20,
                                 lhs.m20 / rhs.m01 + lhs.m21 / rhs.m11 + lhs.m22 / rhs.m21,
                                 lhs.m20 / rhs.m02 + lhs.m21 / rhs.m12 + lhs.m22 / rhs.m22);
        }

        #endregion

        private Matrix3x3 ComputeInverse()
        {
            float d = m00 * m11 * m22 - m00 * m21 * m12 + m10 * m21 * m02 -
                        m10 * m01 * m22 + m20 * m01 * m12 - m20 * m11 * m02;
            d = (d == 0) ? 1 : d;

            return new Matrix3x3((m11 * m22 - m12 * m21) / d,
                                -(m01 * m22 - m02 * m21) / d,
                                 (m01 * m12 - m02 * m11) / d,
                                -(m10 * m22 - m12 * m20) / d,
                                 (m00 * m22 - m02 * m20) / d,
                                -(m00 * m12 - m02 * m10) / d,
                                 (m10 * m21 - m11 * m20) / d,
                                -(m00 * m21 - m01 * m20) / d,
                                 (m00 * m11 - m01 * m10) / d);
        }
    }
}
