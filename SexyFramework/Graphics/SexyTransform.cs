using System;

namespace Sexy
{
    /// <summary>3x3 matrix acting on column vectors (Sexy::SexyMatrix3).</summary>
    public class SexyMatrix3
    {
        public float m00, m01, m02;

        public float m10, m11, m12;

        public float m20, m21, m22;

        public SexyMatrix3()
        {
        }

        public SexyMatrix3(SexyMatrix3 theMatrix)
        {
            CopyFrom(theMatrix);
        }

        public void CopyFrom(SexyMatrix3 theMatrix)
        {
            m00 = theMatrix.m00;
            m01 = theMatrix.m01;
            m02 = theMatrix.m02;
            m10 = theMatrix.m10;
            m11 = theMatrix.m11;
            m12 = theMatrix.m12;
            m20 = theMatrix.m20;
            m21 = theMatrix.m21;
            m22 = theMatrix.m22;
        }

        public void ZeroMatrix()
        {
            m00 = m01 = m02 = m10 = m11 = m12 = m20 = m21 = m22 = 0;
        }

        public void LoadIdentity()
        {
            m01 = m02 = m10 = m12 = m20 = m21 = 0;
            m00 = m11 = m22 = 1;
        }

        public static SexyMatrix3 operator *(SexyMatrix3 a, SexyMatrix3 b)
        {
            SexyMatrix3 r = new SexyMatrix3();
            r.m00 = a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20;
            r.m01 = a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21;
            r.m02 = a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22;
            r.m10 = a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20;
            r.m11 = a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21;
            r.m12 = a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22;
            r.m20 = a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20;
            r.m21 = a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21;
            r.m22 = a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22;
            return r;
        }

        public static SexyVector2 operator *(SexyMatrix3 m, SexyVector2 v)
        {
            return new SexyVector2(m.m00 * v.x + m.m01 * v.y + m.m02, m.m10 * v.x + m.m11 * v.y + m.m12);
        }
    }

    /// <summary>Affine 2D transform built by pre-multiplying operations (Sexy::SexyTransform2D).</summary>
    public class SexyTransform2D : SexyMatrix3
    {
        public SexyTransform2D()
        {
            LoadIdentity();
        }

        public SexyTransform2D(bool loadIdentity)
        {
            if (loadIdentity)
            {
                LoadIdentity();
            }
        }

        public SexyTransform2D(SexyMatrix3 theMatrix) : base(theMatrix)
        {
        }

        // The operations pre-multiply in place (this = op * this) rather than building the op matrix and
        // multiplying, which allocated two matrices per call; the products are the same.
        public void Translate(float tx, float ty)
        {
            m00 += tx * m20;
            m01 += tx * m21;
            m02 += tx * m22;
            m10 += ty * m20;
            m11 += ty * m21;
            m12 += ty * m22;
        }

        /// <summary>Rotates counter-clockwise on screen by <paramref name="rot"/> radians.</summary>
        public void RotateRad(float rot)
        {
            float s = (float)Math.Sin(rot);
            float c = (float)Math.Cos(rot);
            float a00 = m00, a01 = m01, a02 = m02;
            m00 = c * a00 + s * m10;
            m01 = c * a01 + s * m11;
            m02 = c * a02 + s * m12;
            m10 = -s * a00 + c * m10;
            m11 = -s * a01 + c * m11;
            m12 = -s * a02 + c * m12;
        }

        public void RotateDeg(float rot)
        {
            RotateRad(3.1415927f * rot / 180.0f);
        }

        public void Scale(float sx, float sy)
        {
            m00 *= sx;
            m01 *= sx;
            m02 *= sx;
            m10 *= sy;
            m11 *= sy;
            m12 *= sy;
        }
    }

    /// <summary>
    /// Lazily-built transform (Sexy::Transform): simple translate/rotate/scale combinations are kept
    /// as parameters so Graphics can draw them with ordinary blits; anything else becomes a matrix.
    /// </summary>
    public class Transform
    {
        private readonly SexyTransform2D mMatrix = new SexyTransform2D(false); // 0x00

        public bool mNeedCalcMatrix;                              // 0x24

        public bool mComplex;                                     // 0x25

        public bool mHaveRot;                                     // 0x26

        public bool mHaveScale;                                   // 0x27

        public float mTransX1;                                    // 0x28

        public float mTransY1;                                    // 0x2c

        public float mTransX2;                                    // 0x30

        public float mTransY2;                                    // 0x34

        public float mScaleX;                                     // 0x38

        public float mScaleY;                                     // 0x3c

        public float mRot;                                        // 0x40

        public Transform()
        {
            Reset();
        }

        /// <summary>Copies another transform (the original memcpy's the 0x44-byte struct).</summary>
        public Transform(Transform theTransform)
        {
            mMatrix.CopyFrom(theTransform.mMatrix);
            mNeedCalcMatrix = theTransform.mNeedCalcMatrix;
            mComplex = theTransform.mComplex;
            mHaveRot = theTransform.mHaveRot;
            mHaveScale = theTransform.mHaveScale;
            mTransX1 = theTransform.mTransX1;
            mTransY1 = theTransform.mTransY1;
            mTransX2 = theTransform.mTransX2;
            mTransY2 = theTransform.mTransY2;
            mScaleX = theTransform.mScaleX;
            mScaleY = theTransform.mScaleY;
            mRot = theTransform.mRot;
        }

        public void Reset()
        {
            mNeedCalcMatrix = true;
            mComplex = false;
            mTransY1 = 0;
            mTransX1 = 0;
            mTransY2 = 0;
            mTransX2 = 0;
            mScaleY = 1;
            mScaleX = 1;
            mRot = 0;
            mHaveRot = false;
            mHaveScale = false;
        }

        public void Translate(float tx, float ty)
        {
            if (mComplex)
            {
                mMatrix.Translate(tx, ty);
                return;
            }
            mNeedCalcMatrix = true;
            if (!mHaveRot && !mHaveScale)
            {
                mTransX1 += tx;
                mTransY1 += ty;
            }
            else
            {
                mTransX2 += tx;
                mTransY2 += ty;
            }
        }

        public void RotateRad(float rot)
        {
            if (mComplex)
            {
                mMatrix.RotateRad(rot);
                return;
            }
            if (!mHaveScale)
            {
                mNeedCalcMatrix = true;
                mHaveRot = true;
                mRot += rot;
            }
            else
            {
                MakeComplex();
                mMatrix.RotateRad(rot);
            }
        }

        public void RotateDeg(float rot)
        {
            RotateRad(3.1415927f * rot / 180.0f);
        }

        public void Scale(float sx, float sy)
        {
            if (mComplex)
            {
                mMatrix.Scale(sx, sy);
                return;
            }
            if (!mHaveRot && mTransX1 == 0 && mTransY1 == 0 && (sx >= 0 || mScaleX * sx == -1) && sy >= 0)
            {
                mNeedCalcMatrix = true;
                mHaveScale = true;
                mScaleX *= sx;
                mScaleY *= sy;
            }
            else
            {
                MakeComplex();
                mMatrix.Scale(sx, sy);
            }
        }

        public void MakeComplex()
        {
            if (!mComplex)
            {
                mComplex = true;
                CalcMatrix();
            }
        }

        private void CalcMatrix()
        {
            if (!mNeedCalcMatrix)
            {
                return;
            }
            mNeedCalcMatrix = false;
            mMatrix.LoadIdentity();
            mMatrix.m02 = mTransX1;
            mMatrix.m12 = mTransY1;
            mMatrix.m22 = 1;
            if (mHaveScale)
            {
                mMatrix.m00 = mScaleX;
                mMatrix.m11 = mScaleY;
            }
            else if (mHaveRot)
            {
                mMatrix.RotateRad(mRot);
            }
            if (mTransX2 != 0 || mTransY2 != 0)
            {
                mMatrix.Translate(mTransX2, mTransY2);
            }
        }

        public SexyTransform2D GetMatrix()
        {
            CalcMatrix();
            return mMatrix;
        }
    }
}
