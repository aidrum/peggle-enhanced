namespace Sexy
{
    /// <summary>Watches typed characters for a cheat phrase (Sexy::TypingCheck); a wrong key restarts it.</summary>
    public class TypingCheck
    {
        public string mPhrase = "";                               // 0x00

        public int mPos;                                          // 0x04

        public TypingCheck(string thePhrase)
        {
            SetPhrase(thePhrase);
        }

        public void SetPhrase(string thePhrase)
        {
            mPhrase = thePhrase;
            mPos = 0;
        }

        /// <summary>Feeds one character; true when the whole phrase has just been typed.</summary>
        public bool Check(char theChar)
        {
            if (mPhrase.Length == 0)
            {
                return false;
            }
            if (char.ToLowerInvariant(mPhrase[mPos]) == char.ToLowerInvariant(theChar))
            {
                mPos++;
                if (mPos == mPhrase.Length)
                {
                    mPos = 0;
                    return true;
                }
                return false;
            }
            mPos = 0;
            return false;
        }
    }
}
