using System.Collections.Generic;

namespace Sexy
{
    /// <summary>The board's floating captions (Sexy::FloatingTextMgr), addressed by id and drawn per layer.</summary>
    public class FloatingTextMgr
    {
        public int mLastId;                                       // 0x04

        public List<FloatingText> mTexts = new List<FloatingText>(); // 0x08

        public void SyncState(DataSync theSync)
        {
            theSync.SyncLong(ref mLastId);
            theSync.SyncList(mTexts, (ds, t) => { t.SyncState(ds); return t; }, () => new FloatingText());
        }

        public void Clear()
        {
            mTexts.Clear();
        }

        /// <summary>Adds a blank text with a fresh non-zero id.</summary>
        public FloatingText AddText()
        {
            mTexts.Add(new FloatingText());
            mLastId++;
            if (mLastId == 0)
            {
                mLastId = 1;
            }
            FloatingText aText = mTexts[mTexts.Count - 1];
            aText.mId = mLastId;
            return aText;
        }

        public void EraseText(int theId)
        {
            for (int i = 0; i < mTexts.Count; i++)
            {
                if (mTexts[i].mId == theId)
                {
                    mTexts.RemoveAt(i);
                    return;
                }
            }
        }

        public FloatingText GetTextById(int theId)
        {
            foreach (FloatingText aText in mTexts)
            {
                if (aText.mId == theId)
                {
                    return aText;
                }
            }
            return null;
        }

        public void Update()
        {
            for (int i = 0; i < mTexts.Count;)
            {
                if (!mTexts[i].Update())
                {
                    mTexts.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public void Draw(Graphics g, int theLayer)
        {
            for (int i = 0; i < mTexts.Count; i++)
            {
                if (mTexts[i].mLayer == theLayer)
                {
                    mTexts[i].Draw(g);
                }
            }
        }

        public bool AnyActive()
        {
            return mTexts.Count != 0;
        }
    }
}
