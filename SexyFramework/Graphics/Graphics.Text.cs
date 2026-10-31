using System.Text;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Text output, ported from Peggle Classic HD's Sexy::Graphics (DrawString, WriteString,
    /// WriteWordWrapped). WriteString understands "^RRGGBB^" colour codes, "^oldclr^" and "^^"
    /// when mWriteColoredString is set.
    /// </summary>
    public partial class Graphics
    {
        public void DrawString(string theString, int theX, int theY)
        {
            if (mFont == null)
            {
                return;
            }
            int y = theY + mTransY;
            if (y >= mClipRect.mY && y - mFont.GetAscent() < mClipRect.mY + mClipRect.mHeight)
            {
                mFont.DrawString(this, theX, theY, theString, mColor, mClipRect);
            }
        }

        public void DrawString(StringBuilder theString, int theX, int theY)
        {
            DrawString(theString.ToString(), theX, theY);
        }

        public int StringWidth(string theString)
        {
            return mFont.StringWidth(theString);
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }
            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }
            return 0;
        }

        private static int ColorToInt(Color c)
        {
            return (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
        }

        public int WriteString(string theString, int theX, int theY)
        {
            return WriteString(theString, theX, theY, -1, -1, true, 0, -1, -1);
        }

        public int WriteString(string theString, int theX, int theY, int theWidth, int theJustification)
        {
            return WriteString(theString, theX, theY, theWidth, theJustification, true, 0, -1, -1);
        }

        public int WriteString(string theString, int theX, int theY, int theWidth, int theJustification, bool drawString, int theOffset, int theLength)
        {
            return WriteString(theString, theX, theY, theWidth, theJustification, drawString, theOffset, theLength, -1);
        }

        public int WriteString(string theString, int theX, int theY, int theWidth, int theJustification, bool drawString, int theOffset, int theLength, int theOldColor)
        {
            if (theOldColor == -1)
            {
                theOldColor = ColorToInt(mColor);
            }
            if (drawString)
            {
                if (theJustification == 1)
                {
                    theX += theWidth - WriteString(theString, theX, theY, theWidth, -1, false, theOffset, theLength, -1);
                }
                else if (theJustification == 0)
                {
                    theX += (theWidth - WriteString(theString, theX, theY, theWidth, -1, false, theOffset, theLength, -1)) / 2;
                }
            }
            int end = theString.Length;
            if (theLength >= 0 && theOffset + theLength <= end)
            {
                end = theOffset + theLength;
            }
            StringBuilder buf = new StringBuilder();
            int xOffset = 0;
            for (int i = theOffset; i < end; i++)
            {
                char c = theString[i];
                if (c == '^' && mWriteColoredString)
                {
                    if (i + 1 < theString.Length && theString[i + 1] == '^')
                    {
                        buf.Append('^');
                        i++;
                        continue;
                    }
                    if (i > end - 8)
                    {
                        break;
                    }
                    int color = 0;
                    if (theString[i + 1] == 'o')
                    {
                        if (string.CompareOrdinal(theString, i + 1, "oldclr", 0, 6) == 0)
                        {
                            color = theOldColor;
                        }
                    }
                    else
                    {
                        for (int d = 0; d < 6; d++)
                        {
                            color += HexDigit(theString[i + 1 + d]) << ((5 - d) * 4);
                        }
                    }
                    string run = buf.ToString();
                    if (drawString)
                    {
                        DrawString(run, theX + xOffset, theY);
                        SetColor(new Color((color >> 16) & 0xFF, (color >> 8) & 0xFF, color & 0xFF, (int)mColor.A));
                    }
                    i += 7;
                    xOffset += mFont.StringWidth(run);
                    buf.Clear();
                }
                else
                {
                    buf.Append(c);
                }
            }
            string last = buf.ToString();
            if (drawString)
            {
                DrawString(last, theX + xOffset, theY);
            }
            xOffset += mFont.StringWidth(last);
            return xOffset;
        }

        private int WriteWordWrappedHelper(string theString, int theX, int theY, int theWidth, int theJustification, bool drawString, int theOffset, int theLength, int theOldColor, int theMaxChars)
        {
            if (theOffset + theLength > theMaxChars)
            {
                theLength = theMaxChars - theOffset;
                if (theLength <= 0)
                {
                    return -1;
                }
            }
            return WriteString(theString, theX, theY, theWidth, theJustification, drawString, theOffset, theLength, theOldColor);
        }

        public int WriteWordWrapped(TRect theRect, string theLine)
        {
            return WriteWordWrapped(theRect, theLine, -1, -1);
        }

        public int WriteWordWrapped(TRect theRect, string theLine, int theLineSpacing, int theJustification)
        {
            int maxWidth = 0;
            return WriteWordWrapped(theRect, theLine, theLineSpacing, theJustification, ref maxWidth, -1);
        }

        public int WriteWordWrapped(TRect theRect, string theLine, int theLineSpacing, int theJustification, ref int theMaxWidth, int theMaxChars)
        {
            int lastWidth = 0;
            int lineCount = 0;
            return WriteWordWrapped(theRect, theLine, theLineSpacing, theJustification, ref theMaxWidth, theMaxChars, ref lastWidth, ref lineCount, false);
        }

        public int WriteWordWrapped(TRect theRect, string theLine, int theLineSpacing, int theJustification, ref int theMaxWidth, int theMaxChars, ref int theLastWidth, ref int theLineCount, bool useLastWidth)
        {
            Color origColor = GetColor();
            int origColorInt = ColorToInt(origColor);
            if (((uint)origColorInt & 0xFF000000u) == 0xFF000000u)
            {
                origColorInt &= 0xFFFFFF;
            }
            if (theMaxChars < 0)
            {
                theMaxChars = theLine.Length;
            }
            Font font = mFont;
            int curY = font.GetAscent() - font.GetAscentPadding();
            if (theLineSpacing == -1)
            {
                theLineSpacing = font.GetLineSpacing();
            }
            int curPos = 0;
            int lineStartPos = 0;
            int curWidth = 0;
            char curChar = '\0';
            char prevChar = '\0';
            int spacePos = -1;
            int maxWidth = 0;
            int lineCount = 0;
            int indentX = 0;
            if (useLastWidth)
            {
                indentX = theLastWidth;
                curWidth = indentX;
            }
            while (curPos < theLine.Length)
            {
                curChar = theLine[curPos];
                if (curChar == '^' && mWriteColoredString)
                {
                    if (curPos + 1 < theLine.Length)
                    {
                        if (theLine[curPos + 1] != '^')
                        {
                            curPos += 8;
                            continue;
                        }
                        curPos++;
                    }
                }
                else if (curChar == ' ')
                {
                    spacePos = curPos;
                }
                else if (curChar == '\n')
                {
                    curWidth = theRect.mWidth + 1;
                    spacePos = curPos;
                    curPos++;
                }
                curWidth += font.CharWidthKern(curChar, prevChar);
                if (curWidth > theRect.mWidth)
                {
                    int lineWidth;
                    if (curPos < theMaxChars)
                    {
                        lineCount++;
                    }
                    if (spacePos == -1)
                    {
                        if (curPos < lineStartPos + 1)
                        {
                            curPos++;
                        }
                        lineWidth = WriteWordWrappedHelper(theLine, theRect.mX + indentX, theRect.mY + curY, theRect.mWidth, theJustification, true, lineStartPos, curPos - lineStartPos, origColorInt, theMaxChars);
                        if (lineWidth < 0)
                        {
                            break;
                        }
                        if (theMaxWidth < lineWidth)
                        {
                            theMaxWidth = lineWidth;
                        }
                        theLastWidth = lineWidth;
                    }
                    else
                    {
                        int y = theRect.mY + curY + mTransY;
                        if (y >= mClipRect.mY && y < mClipRect.mY + mClipRect.mHeight + theLineSpacing)
                        {
                            WriteWordWrappedHelper(theLine, theRect.mX + indentX, theRect.mY + curY, theRect.mWidth, theJustification, true, lineStartPos, spacePos - lineStartPos, origColorInt, theMaxChars);
                        }
                        lineWidth = curWidth + indentX;
                        if (lineWidth < 0)
                        {
                            break;
                        }
                        curPos = spacePos + 1;
                        if (curChar != '\n')
                        {
                            while (curPos < theLine.Length && theLine[curPos] == ' ')
                            {
                                curPos++;
                            }
                        }
                    }
                    if (maxWidth < lineWidth)
                    {
                        maxWidth = lineWidth;
                    }
                    lineStartPos = curPos;
                    spacePos = -1;
                    curWidth = 0;
                    prevChar = '\0';
                    indentX = 0;
                    curY += theLineSpacing;
                }
                else
                {
                    curPos++;
                    prevChar = curChar;
                }
            }
            if (lineStartPos < theLine.Length)
            {
                int lineWidth = WriteWordWrappedHelper(theLine, theRect.mX + indentX, theRect.mY + curY, theRect.mWidth, theJustification, true, lineStartPos, theLine.Length - lineStartPos, origColorInt, theMaxChars);
                if (lineWidth >= 0)
                {
                    if (maxWidth < lineWidth)
                    {
                        maxWidth = lineWidth;
                    }
                    if (theMaxWidth < lineWidth)
                    {
                        theMaxWidth = lineWidth;
                    }
                    theLastWidth = lineWidth;
                    curY += theLineSpacing;
                }
            }
            else if (curChar == '\n')
            {
                curY += theLineSpacing;
                theLastWidth = 0;
            }
            SetColor(origColor);
            theMaxWidth = maxWidth;
            theLineCount = lineCount + 1;
            return font.GetDescent() + curY - theLineSpacing;
        }

        public void DrawStringColor(string theLine, int theX, int theY, int theOldColor)
        {
            WriteString(theLine, theX, theY, -1, -1, true, 0, -1, theOldColor);
        }

        public int GetWordWrappedHeight(int theWidth, string theLine, int theLineSpacing, ref int theMaxWidth, int theMaxChars)
        {
            Graphics g = Graphics.GetNew();
            g.SetFont(mFont);
            g.mWriteColoredString = mWriteColoredString;
            g.mClipRect = new TRect(0, 0, 0, 0);
            int lastWidth = 0;
            int lineCount = 0;
            int result = g.WriteWordWrapped(new TRect(0, 0, theWidth, 0), theLine, theLineSpacing, -1, ref theMaxWidth, theMaxChars, ref lastWidth, ref lineCount, false);
            g.PrepareForReuse();
            return result;
        }

        public int GetWordWrappedHeight(int theWidth, string theLine)
        {
            int maxWidth = 0;
            return GetWordWrappedHeight(theWidth, theLine, -1, ref maxWidth, -1);
        }
    }
}
