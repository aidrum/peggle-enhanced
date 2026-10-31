namespace Sexy
{
    /// <summary>Mersenne Twister (Sexy::MTRand). Next() returns 31-bit values; seed 0 means 0x1105.</summary>
    public class MTRand
    {
        private const int MTRAND_N = 624;

        private const int MTRAND_M = 397;

        private static readonly uint[] mag01 = { 0u, 0x9908b0dfu };

        public uint[] mt = new uint[MTRAND_N];                    // 0x000

        public int mti;                                           // 0x9c0

        public MTRand()
        {
            SRand(0x1105);
        }

        public MTRand(uint theSeed)
        {
            SRand(theSeed);
        }

        public void SRand(uint theSeed)
        {
            if (theSeed == 0)
            {
                theSeed = 0x1105;
            }
            mt[0] = theSeed;
            for (mti = 1; mti < MTRAND_N; mti++)
            {
                mt[mti] = (uint)((mt[mti - 1] ^ (mt[mti - 1] >> 30)) * 0x6c078965u + (uint)mti);
            }
        }

        public uint NextNoAssert()
        {
            uint y;
            if (mti >= MTRAND_N)
            {
                int kk;
                for (kk = 0; kk < MTRAND_N - MTRAND_M; kk++)
                {
                    y = (mt[kk] & 0x80000000u) | (mt[kk + 1] & 0x7fffffffu);
                    mt[kk] = mt[kk + MTRAND_M] ^ (y >> 1) ^ mag01[mt[kk + 1] & 1];
                }
                for (; kk < MTRAND_N - 1; kk++)
                {
                    y = (mt[kk] & 0x80000000u) | (mt[kk + 1] & 0x7fffffffu);
                    mt[kk] = mt[kk + (MTRAND_M - MTRAND_N)] ^ (y >> 1) ^ mag01[mt[kk + 1] & 1];
                }
                y = (mt[MTRAND_N - 1] & 0x80000000u) | (mt[0] & 0x7fffffffu);
                mt[MTRAND_N - 1] = mt[MTRAND_M - 1] ^ (y >> 1) ^ mag01[mt[0] & 1];
                mti = 0;
            }
            y = mt[mti++];
            y ^= y >> 11;
            y ^= (y << 7) & 0x9d2c5680u;
            y ^= (y << 15) & 0xefc60000u;
            y ^= y >> 18;
            return y & 0x7fffffffu;
        }

        public uint Next()
        {
            return NextNoAssert();
        }

        /// <summary>Next() % theRange (Sexy's inline Next(ulong)).</summary>
        public uint Next(uint theRange)
        {
            return theRange == 0 ? 0 : NextNoAssert() % theRange;
        }

        /// <summary>Uniform float in [0, theRange) (Sexy's inline Next(float)).</summary>
        public float Next(float theRange)
        {
            return (float)(NextNoAssert() / 2147483647.0 * theRange);
        }
    }
}
