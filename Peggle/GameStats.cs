namespace Sexy
{
    /// <summary>Per-game / accumulated statistics (Sexy::GameStats, 0x7c bytes).</summary>
    public class GameStats
    {
        public const int NUM_FREE_BALL_TYPES = 3;

        public const int NUM_STYLE_SHOTS = 24;

        public int mShots;                                        // 0x00 (summed; LogicMgr::DoShoot increments)

        public int mBestShot;                                     // 0x04 (max)

        public int mBestValue;                                    // 0x08 (max)

        public int mBucketFreeBalls;                              // 0x0c

        public int[] mFreeBalls = new int[NUM_FREE_BALL_TYPES];   // 0x10

        public int[] mStyleShots = new int[NUM_STYLE_SHOTS];      // 0x1c

        public GameStats()
        {
            Reset();
        }

        public void Reset()
        {
            mShots = 0;
            mBestShot = 0;
            mBestValue = 0;
            mBucketFreeBalls = 0;
            for (int i = 0; i < NUM_FREE_BALL_TYPES; i++)
            {
                mFreeBalls[i] = 0;
            }
            for (int i = 0; i < NUM_STYLE_SHOTS; i++)
            {
                mStyleShots[i] = 0;
            }
        }

        public void SyncState(DataSync theSync)
        {
            if (theSync.IsReading)
            {
                Reset();
            }
            int version = 1;
            theSync.SyncByte(ref version);
            theSync.SyncLong(ref mShots);
            theSync.SyncLong(ref mBestShot);
            theSync.SyncLong(ref mBestValue);
            theSync.SyncLong(ref mBucketFreeBalls);
            for (int i = 0; i < NUM_FREE_BALL_TYPES; i++)
            {
                theSync.SyncLong(ref mFreeBalls[i]);
            }
            int count = NUM_STYLE_SHOTS;
            theSync.SyncLong(ref count);
            for (int i = 0; i < count; i++)
            {
                int v = i < NUM_STYLE_SHOTS ? mStyleShots[i] : 0;
                theSync.SyncLong(ref v);
                if (i < NUM_STYLE_SHOTS)
                {
                    mStyleShots[i] = v;
                }
            }
        }

        public void CheckBestShot(int theScore)
        {
            if (mBestShot < theScore)
            {
                mBestShot = theScore;
            }
        }

        public void Add(GameStats theStats)
        {
            mShots += theStats.mShots;
            if (mBestShot < theStats.mBestShot)
            {
                mBestShot = theStats.mBestShot;
            }
            if (mBestValue < theStats.mBestValue)
            {
                mBestValue = theStats.mBestValue;
            }
            mBucketFreeBalls += theStats.mBucketFreeBalls;
            for (int i = 0; i < NUM_FREE_BALL_TYPES; i++)
            {
                mFreeBalls[i] += theStats.mFreeBalls[i];
            }
            for (int i = 0; i < NUM_STYLE_SHOTS; i++)
            {
                mStyleShots[i] += theStats.mStyleShots[i];
            }
        }

        public int GetTotalFreeBalls()
        {
            int total = mBucketFreeBalls;
            for (int i = 0; i < NUM_FREE_BALL_TYPES; i++)
            {
                total += mFreeBalls[i];
            }
            return total;
        }

        public int GetTotalStylePoints()
        {
            int total = 0;
            for (int i = 0; i < NUM_STYLE_SHOTS; i++)
            {
                total += mStyleShots[i] * SexyUtil.GetStyleShotScore(i, 0, 0);
            }
            return total;
        }

        public int GetTotalStyleShots()
        {
            int total = 0;
            for (int i = 0; i < NUM_STYLE_SHOTS; i++)
            {
                total += mStyleShots[i];
            }
            return total;
        }
    }
}
