using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sexy
{
    // Peggle's (iOS) Sexy::CurvedVal. Based on the Bejeweled port with the Peggle differences:
    // 8192-entry tables, default ramp 4, auto-inc clamps to InMax only, IncInVal() constant shortcut,
    // and no data-pointer change tracking (SetCurve clears it).
    public class CurvedVal
    {
        public const int MODE_CLAMP = 0;
        public const int MODE_REPEAT = 1;
        public const int MODE_PING_PONG = 2;

        public const int RAMP_NONE = 0;
        public const int RAMP_LINEAR = 1;
        public const int RAMP_SLOW_TO_FAST = 2;
        public const int RAMP_FAST_TO_SLOW = 3;
        public const int RAMP_SLOW_FAST_SLOW = 4;
        public const int RAMP_FAST_SLOW_FAST = 5;
        public const int RAMP_CURVEDATA = 6;

        public const int DFLAG_NOCLIP = 1;
        public const int DFLAG_SINGLETRIGGER = 2;
        public const int DFLAG_OUTPUTSYNC = 4;
        public const int DFLAG_HERMITE = 8;
        public const int DFLAG_AUTOINC = 16;

        public const int CV_NUM_SPLINE_POINTS = 8192;

        private const double PI = 3.14159;

        public class DataPoint
        {
            public float mX;
            public float mY;
            public float mAngleDeg;
        }

        public class CurveCacheRecord
        {
            public float[] mTable = new float[CV_NUM_SPLINE_POINTS];
            public SexyMathHermite mHermiteCurve = new SexyMathHermite();
            public string mDataStr;
        }

        public static Dictionary<string, CurveCacheRecord> mCurveCacheMap = new Dictionary<string, CurveCacheRecord>();

        public bool mKeepDataStr;               // 0x00
        public string mDataStr = "";            // 0x04
        public int mMode;                       // 0x08
        public int mRamp;                       // 0x0c
        public double mIncRate;                 // 0x10
        public double mOutMin;                  // 0x18
        public double mOutMax;                  // 0x20
        public string mDataP;                   // 0x28
        public string mCurDataPStr;             // 0x2c
        public int mInitAppUpdateCount;         // 0x30
        public CurvedVal mLinkedVal;            // 0x34
        public CurveCacheRecord mCurveCacheRecord; // 0x38
        public double mCurOutVal;               // 0x3c
        public double mPrevOutVal;              // 0x44
        public double mInMin;                   // 0x4c
        public double mInMax;                   // 0x54
        public bool mNoClip;                    // 0x5c
        public bool mSingleTrigger;             // 0x5d
        public bool mOutputSync;                // 0x5e
        public bool mTriggered;                 // 0x5f
        public bool mIsHermite;                 // 0x60
        public bool mAutoInc;                   // 0x61
        public double mPrevInVal;               // 0x64
        public double mInVal;                   // 0x6c

        public CurvedVal()
        {
            mMode = 0;
            mRamp = RAMP_SLOW_FAST_SLOW;
            mCurveCacheRecord = null;
            mSingleTrigger = false;
            mNoClip = false;
            mOutputSync = false;
            mTriggered = false;
            mIsHermite = false;
            mAutoInc = false;
            mInitAppUpdateCount = 0;
            mOutMin = 0.0;
            mOutMax = 1.0;
            mKeepDataStr = false;
            mInMin = 0.0;
            mInMax = 1.0;
            mLinkedVal = null;
            mCurOutVal = 0.0;
            mInVal = 0.0;
            mPrevInVal = 0.0;
            mIncRate = 0.0;
            mPrevOutVal = 0.0;
            mDataP = null;
            mCurDataPStr = null;
        }

        public CurvedVal(string theData, CurvedVal theLinkedVal = null) : this()
        {
            SetCurve(theData, theLinkedVal);
        }

        public static implicit operator double(CurvedVal theVal)
        {
            return theVal.GetOutVal();
        }

        public void CopyFrom(CurvedVal rhs)
        {
            if (this == rhs)
                return;
            mKeepDataStr = rhs.mKeepDataStr;
            mDataStr = rhs.mDataStr;
            mMode = rhs.mMode;
            mRamp = rhs.mRamp;
            mIncRate = rhs.mIncRate;
            mOutMin = rhs.mOutMin;
            mOutMax = rhs.mOutMax;
            mDataP = rhs.mDataP;
            mCurDataPStr = rhs.mCurDataPStr;
            mInitAppUpdateCount = rhs.mInitAppUpdateCount;
            mLinkedVal = rhs.mLinkedVal;
            mCurveCacheRecord = rhs.mCurveCacheRecord;
            mCurOutVal = rhs.mCurOutVal;
            mPrevOutVal = rhs.mPrevOutVal;
            mInMin = rhs.mInMin;
            mInMax = rhs.mInMax;
            mNoClip = rhs.mNoClip;
            mSingleTrigger = rhs.mSingleTrigger;
            mOutputSync = rhs.mOutputSync;
            mTriggered = rhs.mTriggered;
            mIsHermite = rhs.mIsHermite;
            mAutoInc = rhs.mAutoInc;
            mPrevInVal = rhs.mPrevInVal;
            mInVal = rhs.mInVal;
        }

        private static float CVCharToFloat(char theChar)
        {
            int c = theChar;
            if (c >= 92)
                c--;
            return (c - 35) / 90.0f;
        }

        private static int CVCharToInt(char theChar)
        {
            int c = theChar;
            if (c >= 92)
                c--;
            return c - 35;
        }

        private static float CVStrToAngle(string theStr)
        {
            int num = CVCharToInt(theStr[0]);
            num = num * 90 + CVCharToInt(theStr[1]);
            num = num * 90 + CVCharToInt(theStr[2]);
            return num * 360.0f / 729000.0f;
        }

        protected bool CheckCurveChange()
        {
            if (mDataP != null && (object)mDataP != (object)mCurDataPStr)
            {
                mCurDataPStr = mDataP;
                ParseDataString(mCurDataPStr);
                return true;
            }
            return false;
        }

        protected bool CheckClamping()
        {
            CheckCurveChange();
            if (mMode == MODE_CLAMP)
            {
                if (mInVal < mInMin)
                {
                    mInVal = mInMin;
                    return false;
                }
                if (mInVal > mInMax)
                {
                    mInVal = mInMax;
                    return false;
                }
            }
            else if (mMode == MODE_REPEAT || mMode == MODE_PING_PONG)
            {
                double range = mInMax - mInMin;
                if (mInVal > mInMax || mInVal < mInMin)
                    mInVal = mInMin + (mInVal - mInMin + range) % range;
            }
            return true;
        }

        protected void GenerateTable(List<DataPoint> theDataPointVector, float[] theBuffer, int theSize)
        {
            BSpline aSpline = new BSpline();
            for (int i = 0; i < theDataPointVector.Count; i++)
                aSpline.AddPoint(theDataPointVector[i].mX, theDataPointVector[i].mY);
            aSpline.CalculateSpline();

            bool first = true;
            int aLastX = 0;
            float aLastY = 0f;
            for (int j = 1; j < theDataPointVector.Count; j++)
            {
                DataPoint aPrev = theDataPointVector[j - 1];
                DataPoint aCur = theDataPointVector[j];
                int aStartX = (int)(aPrev.mX * (theSize - 1) + 0.5);
                int anEndX = (int)(aCur.mX * (theSize - 1) + 0.5);
                for (int k = aStartX; k <= anEndX; k++)
                {
                    float t = (j - 1) + (float)(k - aStartX) / (anEndX - aStartX);
                    float aY = aSpline.GetYPoint(t);
                    float aX = aSpline.GetXPoint(t);
                    int aGX = (int)(aX * (theSize - 1) + 0.5);
                    if (aGX < aLastX || aGX > anEndX)
                        continue;
                    if (!first)
                    {
                        if (aGX > aLastX + 1)
                        {
                            for (int l = aLastX; l <= aGX; l++)
                            {
                                float aPct = (float)(l - aLastX) / (aGX - aLastX);
                                float aVal = aPct * aY + (1f - aPct) * aLastY;
                                if (!mNoClip)
                                    aVal = Math.Min(Math.Max(aVal, 0f), 1f);
                                theBuffer[l] = aVal;
                            }
                        }
                        else
                        {
                            float aVal = aY;
                            if (!mNoClip)
                                aVal = Math.Min(Math.Max(aVal, 0f), 1f);
                            theBuffer[aGX] = aVal;
                        }
                    }
                    aLastX = aGX;
                    aLastY = aY;
                    first = false;
                }
            }
            for (int m = 0; m < theDataPointVector.Count; m++)
            {
                DataPoint aPt = theDataPointVector[m];
                theBuffer[(int)(aPt.mX * (theSize - 1) + 0.5)] = aPt.mY;
            }
        }

        private static double ParseDouble(string theStr)
        {
            double.TryParse(theStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double aResult);
            return aResult;
        }

        protected void ParseDataString(string theString)
        {
            mIncRate = 0.0;
            mOutMin = 0.0;
            mOutMax = 1.0;
            mSingleTrigger = false;
            mNoClip = false;
            mOutputSync = false;
            mIsHermite = false;
            mAutoInc = false;
            if (theString.Length <= 0)
                return;

            int aVersion = 0;
            int anIdx = 0;
            if (theString[0] > '`' && theString[0] < 'c')
                aVersion = theString[0] - 'a';
            anIdx++;
            if (aVersion > 0)
            {
                int aFlags = CVCharToInt(theString[anIdx++]);
                mNoClip = (aFlags & DFLAG_NOCLIP) != 0;
                mSingleTrigger = (aFlags & DFLAG_SINGLETRIGGER) != 0;
                mOutputSync = (aFlags & DFLAG_OUTPUTSYNC) != 0;
                mIsHermite = (aFlags & DFLAG_HERMITE) != 0;
                mAutoInc = (aFlags & DFLAG_AUTOINC) != 0;
            }

            int aComma = theString.IndexOf(',', anIdx);
            if (aComma == -1)
            {
                mIsHermite = true;
                return;
            }
            mOutMin = (float)ParseDouble(theString.Substring(anIdx, aComma - anIdx));
            anIdx = aComma + 1;

            aComma = theString.IndexOf(',', anIdx);
            if (aComma == -1)
                return;
            mOutMax = (float)ParseDouble(theString.Substring(anIdx, aComma - anIdx));
            anIdx = aComma + 1;

            aComma = theString.IndexOf(',', anIdx);
            if (aComma == -1)
                return;
            mIncRate = (float)ParseDouble(theString.Substring(anIdx, aComma - anIdx));
            anIdx = aComma + 1;

            if (aVersion > 0)
            {
                aComma = theString.IndexOf(',', anIdx);
                if (aComma == -1)
                    return;
                mInMax = (float)ParseDouble(theString.Substring(anIdx, aComma - anIdx));
                anIdx = aComma + 1;
            }

            string aKey = theString.Substring(anIdx);
            if (mCurveCacheMap.TryGetValue(aKey, out CurveCacheRecord aRecord))
            {
                mCurveCacheRecord = aRecord;
                return;
            }

            aRecord = new CurveCacheRecord();
            mCurveCacheMap.Add(aKey, aRecord);
            mCurveCacheRecord = aRecord;

            List<DataPoint> aPoints = new List<DataPoint>();
            float aCurX = 0f;
            while (anIdx < theString.Length)
            {
                char aChar = theString[anIdx++];
                DataPoint aPt = new DataPoint();
                aPt.mX = aCurX;
                aPt.mY = CVCharToFloat(aChar);
                if (mIsHermite)
                {
                    aPt.mAngleDeg = CVStrToAngle(theString.Substring(anIdx, 3));
                    anIdx += 3;
                }
                else
                {
                    aPt.mAngleDeg = 0f;
                }
                aPoints.Add(aPt);

                while (anIdx < theString.Length)
                {
                    aChar = theString[anIdx++];
                    if (aChar != ' ')
                    {
                        aCurX = Math.Min(aCurX + CVCharToFloat(aChar) * 0.1f, 1.0f);
                        break;
                    }
                    aCurX = (float)(aCurX + 0.1);
                }
            }

            GenerateTable(aPoints, aRecord.mTable, CV_NUM_SPLINE_POINTS);
            aRecord.mDataStr = theString;
            aRecord.mHermiteCurve.mPoints.Clear();
            for (int i = 0; i < aPoints.Count; i++)
            {
                DataPoint aPt = aPoints[i];
                float aSlope = (float)Math.Tan(aPt.mAngleDeg * 3.14159f / 180.0f);
                aRecord.mHermiteCurve.mPoints.Add(new SexyMathHermite.SPoint(aPt.mX, aPt.mY, aSlope));
            }
            aRecord.mHermiteCurve.Rebuild();
        }

        public void SetCurve(string theData, CurvedVal theLinkedVal = null)
        {
            mDataP = null;
            mCurDataPStr = null;
            if (GlobalStaticVars.gSexyAppBase != null)
                mInitAppUpdateCount = GlobalStaticVars.gSexyAppBase.mUpdateCount;
            mTriggered = false;
            mLinkedVal = theLinkedVal;
            mRamp = RAMP_CURVEDATA;
            ParseDataString(theData);
            if (mKeepDataStr)
                mDataStr = theData;
            mInVal = mInMin;
        }

        public void SetConstant(double theValue)
        {
            mInVal = 0.0;
            mTriggered = false;
            mLinkedVal = null;
            mRamp = RAMP_LINEAR;
            mInMax = 0.0;
            mInMin = 0.0;
            mOutMax = theValue;
            mOutMin = theValue;
        }

        public bool IsLinear()
        {
            return mRamp == RAMP_LINEAR;
        }

        public bool IsInitialized()
        {
            return mRamp != RAMP_NONE;
        }

        public void SetMode(int theMode)
        {
            mMode = theMode;
        }

        public void SetRamp(int theRamp)
        {
            mRamp = theRamp;
        }

        public void SetOutRange(double theMin, double theMax)
        {
            mOutMin = theMin;
            mOutMax = theMax;
        }

        public void SetInRange(double theMin, double theMax)
        {
            mInMin = theMin;
            mInMax = theMax;
        }

        public double GetOutVal()
        {
            return mCurOutVal = GetOutVal(GetInVal());
        }

        public double GetOutVal(double theInVal)
        {
            switch (mRamp)
            {
                case RAMP_NONE:
                case RAMP_LINEAR:
                    if (mMode == MODE_PING_PONG)
                    {
                        if ((mInMax - mInMin) / 2.0 < theInVal - mInMin)
                            return mOutMin + (1.0 - (theInVal - mInMin) / (mInMax - mInMin)) * (mOutMax - mOutMin) * 2.0;
                        return mOutMin + (theInVal - mInMin) / (mInMax - mInMin) * (mOutMax - mOutMin) * 2.0;
                    }
                    if (mInMin == mInMax)
                        return mOutMin;
                    return mOutMin + (theInVal - mInMin) / (mInMax - mInMin) * (mOutMax - mOutMin);

                case RAMP_SLOW_TO_FAST:
                {
                    double anAng = (theInVal - mInMin) / (mInMax - mInMin) * PI / 2.0;
                    if (mMode == MODE_PING_PONG)
                        anAng *= 2.0;
                    if (anAng > 1.570795)
                        anAng = PI - anAng;
                    return mOutMin + (1.0 - Math.Cos(anAng)) * (mOutMax - mOutMin);
                }

                case RAMP_FAST_TO_SLOW:
                {
                    double anAng = (theInVal - mInMin) / (mInMax - mInMin) * PI / 2.0;
                    if (mMode == MODE_PING_PONG)
                        anAng *= 2.0;
                    return mOutMin + Math.Sin(anAng) * (mOutMax - mOutMin);
                }

                case RAMP_SLOW_FAST_SLOW:
                {
                    double anAng = (theInVal - mInMin) / (mInMax - mInMin) * PI;
                    if (mMode == MODE_PING_PONG)
                        anAng *= 2.0;
                    return mOutMin + ((-Math.Cos(anAng) + 1.0) / 2.0) * (mOutMax - mOutMin);
                }

                case RAMP_FAST_SLOW_FAST:
                {
                    double anAng = (theInVal - mInMin) / (mInMax - mInMin) * PI;
                    if (mMode == MODE_PING_PONG)
                        anAng *= 2.0;
                    if (anAng > PI)
                        anAng = 6.28318 - anAng;
                    if (anAng < 1.570795)
                        return mOutMin + (Math.Sin(anAng) / 2.0) * (mOutMax - mOutMin);
                    return mOutMin + ((2.0 - Math.Sin(anAng)) / 2.0) * (mOutMax - mOutMin);
                }

                case RAMP_CURVEDATA:
                {
                    CheckCurveChange();
                    if (mCurveCacheRecord == null)
                        return 0.0;
                    if (mInMax - mInMin == 0.0)
                        return 0.0;
                    float aPct = Math.Min((float)((theInVal - mInMin) / (mInMax - mInMin)), 1.0f);
                    if (mMode == MODE_PING_PONG)
                    {
                        if (aPct > 0.5f)
                            aPct = 1.0f - aPct;
                        aPct *= 2.0f;
                    }
                    if (!mIsHermite)
                    {
                        int anIdx = (int)(aPct * 8191.0f);
                        if (anIdx == CV_NUM_SPLINE_POINTS - 1)
                            return mOutMin + mCurveCacheRecord.mTable[anIdx] * (mOutMax - mOutMin);
                        float aFrac = aPct * 8191.0f - anIdx;
                        return mOutMin + (mCurveCacheRecord.mTable[anIdx] * (1.0f - aFrac) + mCurveCacheRecord.mTable[anIdx + 1] * aFrac) * (mOutMax - mOutMin);
                    }
                    double aVal = mOutMin + mCurveCacheRecord.mHermiteCurve.Evaluate(aPct) * (mOutMax - mOutMin);
                    if (!mNoClip)
                    {
                        if (mOutMin < mOutMax)
                            aVal = Math.Min(Math.Max(aVal, mOutMin), mOutMax);
                        else
                            aVal = Math.Max(Math.Min(aVal, mOutMin), mOutMax);
                    }
                    return aVal;
                }

                default:
                    return mOutMin;
            }
        }

        public double GetOutValDelta()
        {
            return GetOutVal() - mPrevOutVal;
        }

        public double GetOutFinalVal()
        {
            return GetOutVal(mInMax);
        }

        public double GetInVal()
        {
            double aVal = mInVal;
            if (mLinkedVal != null)
            {
                aVal = mLinkedVal.mOutputSync ? mLinkedVal.GetOutVal() : mLinkedVal.GetInVal();
            }
            else if (mAutoInc && GlobalStaticVars.gSexyAppBase != null)
            {
                aVal = Math.Min(mInMin + (GlobalStaticVars.gSexyAppBase.mUpdateCount - mInitAppUpdateCount) * mIncRate, mInMax);
            }

            if (mMode == MODE_PING_PONG)
            {
                double aPct = (float)((aVal - mInMin) / (mInMax - mInMin));
                if (aPct <= 0.5)
                    return mInMin + aPct * 2.0 * (mInMax - mInMin);
                return mInMin + (1.0 - aPct) * 2.0 * (mInMax - mInMin);
            }
            return aVal;
        }

        public bool SetInVal(double theVal)
        {
            mPrevOutVal = GetOutVal();
            mTriggered = false;
            mPrevInVal = theVal;
            mInVal = theVal;
            if (CheckClamping())
                return true;
            if (!mTriggered)
            {
                mTriggered = true;
                return false;
            }
            return mSingleTrigger;
        }

        public bool IncInVal(double theInc)
        {
            mPrevOutVal = GetOutVal();
            mPrevInVal = mInVal;
            mInVal += theInc;
            if (CheckClamping())
                return true;
            if (!mTriggered)
            {
                mTriggered = true;
                return false;
            }
            return mSingleTrigger;
        }

        public bool IncInVal()
        {
            if (mRamp == RAMP_LINEAR && mInMin == mInMax)
                return true;
            if (mIncRate == 0.0)
                return false;
            return IncInVal(mIncRate);
        }

        public bool IsFinished()
        {
            return 1.0 <= GetInVal() / mInMax;
        }

        public bool HasBeenTriggered()
        {
            if (mAutoInc)
                mTriggered = GetInVal() == mInMax;
            return mTriggered;
        }

        public void ClearTrigger()
        {
            mTriggered = false;
        }

        public bool IsDoingCurve()
        {
            return GetInVal() != mInMax && mRamp != RAMP_NONE;
        }
    }
}
