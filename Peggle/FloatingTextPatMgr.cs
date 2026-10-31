using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Re-localizes floating texts the game builds at runtime (Sexy::FloatingTextPatMgr): each
    /// recognised English pattern ("+500 Style Points", "Next 3 Turns"...) is parsed and rebuilt
    /// from its localized format string.
    /// </summary>
    public class FloatingTextPatMgr
    {
        public bool HandlePattern(ref string theText)
        {
            return IsMoreZen(ref theText) || IsStylePoints(ref theText) || Is10kScoreMarker(ref theText) ||
                   IsPlusCommaSeparate(ref theText) || IsScoreTally(ref theText) || IsPegMultiplier(ref theText) ||
                   IsDuelTurnNotify(ref theText) || IsPowerupNextTurns(ref theText) || IsTipTenOrangeLeft(ref theText);
        }

        private static bool ToInt(string theString, out int theValue)
        {
            theValue = 0;
            return Common.StringToInt(theString, ref theValue);
        }

        public bool IsMoreZen(ref string theText)
        {
            const string aSuffix = "% More Zen!";
            if (aSuffix.Length < theText.Length && theText.EndsWith(aSuffix))
            {
                if (!ToInt(theText.Substring(0, theText.Length - aSuffix.Length), out int aValue))
                {
                    return false;
                }
                theText = CFormat.StrFormat(SexyAppBase.Loc("%d%% More Zen!"), aValue);
                return true;
            }
            return false;
        }

        public bool IsStylePoints(ref string theText)
        {
            const string aSuffix = " Style Points";
            const string aPrefix = "+";
            if (aPrefix.Length + aSuffix.Length < theText.Length && theText.EndsWith(aSuffix) && theText.StartsWith(aPrefix))
            {
                if (!ToInt(theText.Substring(aPrefix.Length, theText.Length - aSuffix.Length - aPrefix.Length), out int aValue))
                {
                    return false;
                }
                theText = CFormat.StrFormat(SexyAppBase.Loc("+%S Style Points"), SexyUtil.CommaSeperate(aValue));
                return true;
            }
            return false;
        }

        /// <summary>"__10k__+N": a score marker; comma-separated from 10,000 up.</summary>
        public bool Is10kScoreMarker(ref string theText)
        {
            const string aPrefix = "__10k__+";
            if (aPrefix.Length < theText.Length && theText.StartsWith(aPrefix))
            {
                string aDigits = theText.Substring(aPrefix.Length);
                if (!ToInt(aDigits, out int aValue))
                {
                    return false;
                }
                if (aValue < 10000)
                {
                    theText = CFormat.StrFormat(SexyAppBase.Loc("+%S"), aDigits);
                }
                else
                {
                    theText = CFormat.StrFormat(SexyAppBase.Loc("+%S"), SexyUtil.CommaSeperate(aValue));
                }
                return true;
            }
            return false;
        }

        public bool IsPlusCommaSeparate(ref string theText)
        {
            const string aPrefix = "+";
            if (aPrefix.Length < theText.Length && theText.StartsWith(aPrefix))
            {
                if (!ToInt(theText.Substring(aPrefix.Length), out int aValue))
                {
                    return false;
                }
                theText = CFormat.StrFormat(SexyAppBase.Loc("+%S"), SexyUtil.CommaSeperate(aValue));
                return true;
            }
            return false;
        }

        public bool IsScoreTally(ref string theText)
        {
            const string aSuffix = " x Pegs";
            if (aSuffix.Length < theText.Length && theText.EndsWith(aSuffix))
            {
                if (!ToInt(theText.Substring(0, theText.Length - aSuffix.Length), out int aValue))
                {
                    return false;
                }
                theText = CFormat.StrFormat(SexyAppBase.Loc("%S x Pegs"), SexyUtil.CommaSeperate(aValue));
                return true;
            }
            return false;
        }

        /// <summary>"N x M Peg(s)".</summary>
        public bool IsPegMultiplier(ref string theText)
        {
            const string aRequiredBits = " x Peg";
            if (theText.Length < aRequiredBits.Length)
            {
                return false;
            }
            List<string> aTokens = new List<string>();
            utilTokenize(theText, " ", aTokens);
            if (aTokens.Count != 4 || aTokens[1] != "x")
            {
                return false;
            }
            if (aTokens[3] == "Peg")
            {
                if (!ToInt(aTokens[0], out int aScore) || !ToInt(aTokens[2], out int aCount))
                {
                    return false;
                }
                theText = CFormat.StrFormat(SexyAppBase.Loc("%S x %d Peg"), SexyUtil.CommaSeperate(aScore), aCount);
                return true;
            }
            if (aTokens[3] == "Pegs")
            {
                if (!ToInt(aTokens[0], out int aScore) || !ToInt(aTokens[2], out int aCount))
                {
                    return false;
                }
                theText = CFormat.StrFormat(SexyAppBase.Loc("%S x %d Pegs"), SexyUtil.CommaSeperate(aScore), aCount);
                return true;
            }
            return false;
        }

        public bool IsDuelTurnNotify(ref string theText)
        {
            if (theText.Length == 0)
            {
                return false;
            }
            char aLast = theText[theText.Length - 1];
            if (aLast == 'n')
            {
                const string aSuffix = "'s Turn";
                if (aSuffix.Length < theText.Length && theText.EndsWith(aSuffix))
                {
                    string aName = theText.Substring(0, theText.Length - aSuffix.Length);
                    theText = CFormat.StrFormat(SexyAppBase.Loc("%S's Turn"), SexyUtil.GetLocalizedPlayerName(aName));
                    return true;
                }
            }
            else if (aLast == '!')
            {
                const string aSuffix = "'s Last Ball!";
                if (aSuffix.Length < theText.Length && theText.EndsWith(aSuffix))
                {
                    string aName = theText.Substring(0, theText.Length - aSuffix.Length);
                    theText = CFormat.StrFormat(SexyAppBase.Loc("%S's Last Ball!"), SexyUtil.GetLocalizedPlayerName(aName));
                    return true;
                }
            }
            return false;
        }

        public bool IsPowerupNextTurns(ref string theText)
        {
            if (theText.Length == 0)
            {
                return false;
            }
            const string aSuffix = " Turns";
            if (theText[0] == 'N')
            {
                const string aPrefix = "Next ";
                if (aPrefix.Length + aSuffix.Length < theText.Length && theText.EndsWith(aSuffix) && theText.StartsWith(aPrefix))
                {
                    if (!ToInt(theText.Substring(aPrefix.Length, theText.Length - aSuffix.Length - aPrefix.Length), out int aValue))
                    {
                        return false;
                    }
                    theText = CFormat.StrFormat(SexyAppBase.Loc("Next %d Turns"), aValue);
                    return true;
                }
            }
            else if (aSuffix.Length < theText.Length && theText.EndsWith(aSuffix))
            {
                if (!ToInt(theText.Substring(0, theText.Length - aSuffix.Length), out int aValue))
                {
                    return false;
                }
                theText = CFormat.StrFormat(SexyAppBase.Loc("%d Turns"), aValue);
                return true;
            }
            return false;
        }

        public bool IsTipTenOrangeLeft(ref string theText)
        {
            const string aPrefix = "You have ^FF8800^";
            const string aSuffix = "^oldclr^ to go!\nClear all of the ^FF8800^orange pegs^oldclr^ to win!";
            const string aMiddle = " ORANGE ";
            if (aPrefix.Length + aSuffix.Length + aMiddle.Length < theText.Length && theText.StartsWith(aPrefix) && theText.EndsWith(aSuffix))
            {
                string aCore = theText.Substring(aPrefix.Length, theText.Length - aSuffix.Length - aPrefix.Length);
                List<string> aTokens = new List<string>();
                utilTokenize(aCore, " ", aTokens);
                if (aTokens.Count != 3 || aTokens[1] != "ORANGE")
                {
                    return false;
                }
                if (!ToInt(aTokens[0], out int aCount))
                {
                    return false;
                }
                theText = CFormat.StrFormat(SexyAppBase.Loc(
                    "You have ^FF8800^%d ORANGE %S^oldclr^ to go!\nClear all of the ^FF8800^orange pegs^oldclr^ to win!"),
                    aCount, aTokens[2]);
                return true;
            }
            return false;
        }

        public void utilTokenize(string theString, string theDelimiter, List<string> theTokens)
        {
            int aStart = 0;
            do
            {
                int aPos = theString.IndexOf(theDelimiter, aStart);
                if (aPos == -1)
                {
                    aPos = theString.Length;
                }
                theTokens.Add(theString.Substring(aStart, aPos - aStart));
                aStart = aPos + 1;
            }
            while (aStart < theString.Length);
        }
    }
}
