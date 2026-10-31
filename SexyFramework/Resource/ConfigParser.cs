using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sexy
{
    public class ParserException : Exception
    {
        public string mError;

        public ParserException(string theError) : base(theError)
        {
            mError = theError;
        }
    }

    /// <summary>Reads a UTF-16 text file one character at a time (Sexy::TextReader).</summary>
    public class TextReader
    {
        public string mBuffer = "";

        public int mPos;

        public int mMark;

        public int mLineNum = 1;

        public string mFileName = "";

        public string mFileDir = "";

        public bool Open(string theFileName)
        {
            byte[] data = ContentFS.ReadAllBytes(theFileName);
            if (data == null)
            {
                return false;
            }
            // The game's configs are UTF-16LE with a BOM.
            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            {
                mBuffer = Encoding.Unicode.GetString(data, 2, data.Length - 2);
            }
            else if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                mBuffer = Encoding.UTF8.GetString(data, 3, data.Length - 3);
            }
            else
            {
                mBuffer = Encoding.UTF8.GetString(data);
            }
            mPos = 0;
            mMark = 0;
            mLineNum = 1;
            return true;
        }

        public bool EndOfFile()
        {
            return mPos >= mBuffer.Length;
        }

        public char NextChar()
        {
            if (mPos >= mBuffer.Length)
            {
                return '\0';
            }
            char c = mBuffer[mPos++];
            if (c == '\n')
            {
                mLineNum++;
            }
            return c;
        }

        public char PeekChar()
        {
            return mPos < mBuffer.Length ? mBuffer[mPos] : '\0';
        }

        public void UngetChar()
        {
            if (mPos > 0)
            {
                mPos--;
                if (mBuffer[mPos] == '\n')
                {
                    mLineNum--;
                }
            }
        }

        public void Mark()
        {
            mMark = mPos;
        }

        public void RewindToMark()
        {
            mPos = mMark;
        }

        public void SkipWhitespace()
        {
            while (mPos < mBuffer.Length && char.IsWhiteSpace(mBuffer[mPos]))
            {
                NextChar();
            }
        }

        public bool GotoChar(char theChar, bool skipPast)
        {
            while (!EndOfFile())
            {
                if (NextChar() == theChar)
                {
                    if (!skipPast)
                    {
                        UngetChar();
                    }
                    return true;
                }
            }
            return false;
        }

        public bool GotoString(string theString, bool skipPast)
        {
            int idx = mBuffer.IndexOf(theString, mPos, StringComparison.Ordinal);
            if (idx < 0)
            {
                while (!EndOfFile())
                {
                    NextChar();
                }
                return false;
            }
            int target = skipPast ? idx + theString.Length : idx;
            while (mPos < target)
            {
                NextChar();
            }
            return true;
        }
    }

    /// <summary>Parses comma separated values out of a config value (Sexy::StringParser).</summary>
    public class StringParser
    {
        public string mString = "";

        public int mPos;

        public string mError = "";

        public bool mThrowExceptions;

        public StringParser()
        {
        }

        public StringParser(string theString)
        {
            Set(theString);
        }

        public void Set(string theString)
        {
            mString = theString ?? "";
            mPos = 0;
            mError = "";
        }

        public bool EndOfString()
        {
            return mPos >= mString.Length;
        }

        public bool HadError()
        {
            return mError.Length > 0;
        }

        private char Cur => mPos < mString.Length ? mString[mPos] : '\0';

        public bool Error(string theError, bool canThrow)
        {
            mError = theError;
            if (mThrowExceptions && canThrow)
            {
                throw new ParserException(theError);
            }
            return false;
        }

        public void SkipWhitespace()
        {
            while (mPos < mString.Length && char.IsWhiteSpace(mString[mPos]))
            {
                mPos++;
            }
        }

        public bool CheckNextChar(char theChar)
        {
            SkipWhitespace();
            if (Cur != theChar)
            {
                return false;
            }
            mPos++;
            return true;
        }

        public bool EnsureNextChar(char theChar)
        {
            SkipWhitespace();
            if (Cur == theChar)
            {
                mPos++;
                return true;
            }
            return Error("Expecting " + theChar, true);
        }

        public bool ReadIntOrFloat(out float theValue, out bool isFloat, bool canThrow)
        {
            SkipWhitespace();
            int start = mPos;
            int pos = mPos;
            if (pos < mString.Length && mString[pos] == '-')
            {
                pos++;
            }
            bool hadDot = false;
            bool hadDigit = false;
            while (pos < mString.Length)
            {
                char c = mString[pos];
                if (c == '.')
                {
                    if (hadDot)
                    {
                        break;
                    }
                    hadDot = true;
                }
                else if (c >= '0' && c <= '9')
                {
                    hadDigit = true;
                }
                else
                {
                    break;
                }
                pos++;
            }
            if (hadDot)
            {
                mPos = pos;
                if (hadDigit)
                {
                    isFloat = true;
                    float.TryParse(mString.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out theValue);
                    return true;
                }
                isFloat = false;
                theValue = 0f;
                return Error("Expecting Number", canThrow);
            }
            mPos = start;
            theValue = ReadIntBase(canThrow);
            isFloat = false;
            return true;
        }

        public int ReadIntBase(bool canThrow)
        {
            SkipWhitespace();
            int start = mPos;
            bool negative = false;
            if (Cur == '-')
            {
                negative = true;
                mPos++;
            }
            if (Cur == '0' && mPos + 1 < mString.Length && char.ToLowerInvariant(mString[mPos + 1]) == 'x')
            {
                mPos += 2;
                int hexStart = mPos;
                while (mPos < mString.Length && Uri.IsHexDigit(mString[mPos]))
                {
                    mPos++;
                }
                if (mPos > hexStart)
                {
                    int v = int.Parse(mString.Substring(hexStart, mPos - hexStart), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return negative ? -v : v;
                }
                Error("Expecting Integer", canThrow);
                return 0;
            }
            int digitStart = mPos;
            while (mPos < mString.Length && mString[mPos] >= '0' && mString[mPos] <= '9')
            {
                mPos++;
            }
            if (mPos > digitStart)
            {
                long.TryParse(mString.Substring(digitStart, mPos - digitStart), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v);
                return (int)(negative ? -v : v);
            }
            mPos = start;
            Error("Expecting Integer", canThrow);
            return 0;
        }

        public int ReadInt()
        {
            return ReadIntBase(true);
        }

        public int ReadInt(int theDefault)
        {
            if (mError.Length > 0)
            {
                return theDefault;
            }
            int v = ReadIntBase(false);
            if (mError.Length > 0)
            {
                mError = "";
                return theDefault;
            }
            return v;
        }

        public float ReadFloat(float theDefault)
        {
            if (mError.Length > 0)
            {
                return theDefault;
            }
            ReadIntOrFloat(out float v, out _, false);
            if (mError.Length > 0)
            {
                mError = "";
                return theDefault;
            }
            return v;
        }

        public float ReadFloat()
        {
            ReadIntOrFloat(out float v, out _, true);
            return v;
        }

        public bool ReadBool()
        {
            string s = ReadString(true, true);
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "y", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "n", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            Error("Expecting Boolean", true);
            return false;
        }

        /// <summary>
        /// Reads a quoted ("...", with \n \t \\ \" escapes and "a" + "b" continuation) or bare string.
        /// A bare string ends at ',' (not consumed) or, when <paramref name="stopAtSpace"/>, at whitespace.
        /// </summary>
        public bool ReadString(out string theString, bool stopAtSpace, bool trim)
        {
            StringBuilder sb = new StringBuilder();
            SkipWhitespace();
            bool quoted = Cur == '"';
            if (quoted)
            {
                mPos++;
            }
            while (mPos < mString.Length)
            {
                char c = mString[mPos];
                if (quoted)
                {
                    if (c == '\n')
                    {
                        theString = sb.ToString();
                        return Error("Missing end quote", true);
                    }
                    if (c == '\\')
                    {
                        mPos++;
                        char e = Cur;
                        switch (e)
                        {
                        case '"':
                            sb.Append('"');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        default:
                            theString = sb.ToString();
                            return Error("Invalid escape char: \\" + e, true);
                        }
                    }
                    else if (c == '"')
                    {
                        mPos++;
                        if (Cur != '+')
                        {
                            theString = sb.ToString();
                            return true;
                        }
                        mPos++;
                        SkipWhitespace();
                        if (Cur != '"')
                        {
                            theString = sb.ToString();
                            return Error("Missing continued quote", true);
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (char.IsWhiteSpace(c))
                    {
                        if (stopAtSpace)
                        {
                            theString = sb.ToString();
                            return true;
                        }
                    }
                    else if (c == ',')
                    {
                        break;
                    }
                    sb.Append(c);
                }
                mPos++;
            }
            theString = sb.ToString();
            if (trim && !stopAtSpace)
            {
                theString = theString.Trim(' ', '\t', '\r', '\n');
            }
            return true;
        }

        public string ReadString(bool stopAtSpace, bool trim)
        {
            ReadString(out string s, stopAtSpace, trim);
            return s;
        }

        public string ReadString()
        {
            return ReadString(false, true);
        }

        /// <summary>
        /// A quoted string is read as with ReadString; an unquoted one runs to the end of the
        /// value with whitespace collapsed HTML-style (two spaces after a full stop) and "\\" /
        /// "\&lt;newline&gt;" escapes.
        /// (StringParser::ReadHTMLString)
        /// </summary>
        public bool ReadHTMLString(out string theString)
        {
            SkipWhitespace();
            if (Cur == '"')
            {
                return ReadString(out theString, false, true);
            }
            StringBuilder sb = new StringBuilder();
            bool pendingSpace = false;
            bool afterPeriod = false;
            while (mPos < mString.Length)
            {
                char c = mString[mPos];
                if (c == '\\')
                {
                    mPos++;
                    char e = Cur;
                    if (e != '\\' && e != '\n')
                    {
                        theString = sb.ToString();
                        return Error("Invalid escape char: \\" + e, true);
                    }
                    sb.Append(e);
                    pendingSpace = false;
                    afterPeriod = false;
                }
                else if (char.IsWhiteSpace(c))
                {
                    pendingSpace = sb.Length > 0;
                }
                else
                {
                    if (pendingSpace)
                    {
                        sb.Append(' ');
                        if (afterPeriod)
                        {
                            sb.Append(' ');
                        }
                    }
                    pendingSpace = false;
                    sb.Append(c);
                    afterPeriod = c == '.';
                }
                mPos++;
            }
            theString = sb.ToString();
            return true;
        }

        public string ReadHTMLString()
        {
            ReadHTMLString(out string s);
            return s;
        }
    }

    /// <summary>
    /// Reads PopCap ".cfg" description files (Sexy::ConfigParser):
    /// <code>GroupType [Name] { Key: value ... }</code> with // and /* */ comments.
    /// </summary>
    public class ConfigParser
    {
        public enum ItemType
        {
            None,
            GroupStart,
            KeyValue,
            GroupEnd
        }

        private struct GroupInfo
        {
            public string mType;

            public string mName;
        }

        private readonly List<TextReader> mFileStack = new List<TextReader>();

        private TextReader mReader;

        private readonly List<GroupInfo> mGroupStack = new List<GroupInfo>();

        public string mError = "";

        public string mKey = "";

        public string mValue = "";

        public int mKeyLine;

        public ItemType mType;

        public StringParser mValParser = new StringParser();

        public bool mThrowExceptions;

        public ConfigParser(bool throwExceptions)
        {
            mThrowExceptions = throwExceptions;
            mValParser.mThrowExceptions = throwExceptions;
        }

        public bool Open(string theFileName, bool showError)
        {
            mFileStack.Clear();
            mGroupStack.Clear();
            mError = "";
            mKey = "";
            mValue = "";
            mType = ItemType.None;
            mReader = new TextReader();
            mReader.mFileName = System.IO.Path.GetFileName(theFileName);
            mReader.mFileDir = System.IO.Path.GetDirectoryName(theFileName)?.Replace('\\', '/') ?? "";
            mFileStack.Add(mReader);
            if (!mReader.Open(theFileName))
            {
                if (showError)
                {
                    Error("Unable to open file: " + theFileName, false, true);
                }
                return false;
            }
            return true;
        }

        public bool Error(string theError, bool showKey, bool canThrow)
        {
            if (mError.Length != 0)
            {
                return false;
            }
            StringBuilder sb = new StringBuilder(theError).Append(' ');
            for (int i = 0; i < mFileStack.Count; i++)
            {
                TextReader r = mFileStack[i];
                int line = (i == mFileStack.Count - 1 && showKey) ? mKeyLine : r.mLineNum;
                sb.Append(string.Format("\n{0}({1})", r.mFileName, line));
            }
            mError = sb.ToString();
            if (mThrowExceptions && canThrow)
            {
                throw new ParserException(mError);
            }
            return false;
        }

        public string GetError()
        {
            if (mError.Length == 0 && mValParser.mError.Length > 0)
            {
                Error(mValParser.mError, false, false);
            }
            return mError;
        }

        public bool ErrorUnexpectedGroupType()
        {
            return Error("Unexpected Group - " + GetGroupType(), false, true);
        }

        public bool ErrorUnexpectedKey()
        {
            return Error("Unexpected Key - " + mKey, true, true);
        }

        public new ItemType GetType()
        {
            return mType;
        }

        public string GetKey()
        {
            return mKey;
        }

        public bool KeyIs(string theKey)
        {
            return string.Equals(mKey, theKey, StringComparison.OrdinalIgnoreCase);
        }

        public string GetGroupType()
        {
            return mGroupStack.Count == 0 ? "" : mGroupStack[mGroupStack.Count - 1].mType;
        }

        public string GetGroupName()
        {
            return mGroupStack.Count == 0 ? "" : mGroupStack[mGroupStack.Count - 1].mName;
        }

        public int GetGroupDepth()
        {
            return mGroupStack.Count;
        }

        public bool GroupTypeIs(string theType, int theDepth = -1)
        {
            if (theDepth >= 0 && theDepth != mGroupStack.Count)
            {
                ErrorUnexpectedGroupType();
                return false;
            }
            return string.Equals(GetGroupType(), theType, StringComparison.OrdinalIgnoreCase);
        }

        public StringParser GetValParser()
        {
            mValParser.Set(mValue);
            return mValParser;
        }

        public string GetValue()
        {
            return mValue;
        }

        private void GetCode(StringBuilder theCode)
        {
            while (!mReader.EndOfFile())
            {
                char c = mReader.NextChar();
                if (c == ']')
                {
                    mReader.Mark();
                    bool lineEnd = false;
                    while (!mReader.EndOfFile())
                    {
                        char n = mReader.NextChar();
                        if (n == '\n')
                        {
                            return;
                        }
                        if (!char.IsWhiteSpace(n))
                        {
                            break;
                        }
                    }
                    if (!lineEnd)
                    {
                        mReader.RewindToMark();
                    }
                    theCode.Append(c);
                }
                else if (c == '/' && mReader.PeekChar() == '/')
                {
                    mReader.GotoChar('\n', false);
                }
                else if (c == '/' && mReader.PeekChar() == '*')
                {
                    mReader.GotoString("*/", true);
                }
                else
                {
                    theCode.Append(c);
                }
            }
        }

        private string GetToken(bool toEndOfLine)
        {
            StringBuilder token = new StringBuilder();
            mReader.SkipWhitespace();
            while (!mReader.EndOfFile())
            {
                char c = mReader.NextChar();
                if (c == '"')
                {
                    token.Append('"');
                    while (!mReader.EndOfFile())
                    {
                        char q = mReader.NextChar();
                        if (q == '\n')
                        {
                            Error("Carriage return found in quote", false, true);
                            return token.ToString();
                        }
                        token.Append(q);
                        if (q == '"')
                        {
                            break;
                        }
                    }
                    continue;
                }
                if (c == '/' && mReader.PeekChar() == '/')
                {
                    mReader.GotoChar('\n', false);
                    continue;
                }
                if (c == '/' && mReader.PeekChar() == '*')
                {
                    int line = mReader.mLineNum;
                    mReader.GotoString("*/", true);
                    if (mReader.mLineNum != line && toEndOfLine)
                    {
                        break;
                    }
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    if (!toEndOfLine)
                    {
                        if (token.Length == 0)
                        {
                            continue;
                        }
                        break;
                    }
                    if (c == '\n')
                    {
                        break;
                    }
                    token.Append(c);
                    continue;
                }
                if (c == ';' && !toEndOfLine)
                {
                    break;
                }
                if (c == '[')
                {
                    GetCode(token);
                    continue;
                }
                if (c == '\\' && mReader.PeekChar() == '\n')
                {
                    mReader.NextChar();
                    continue;
                }
                if (c == '{' || c == '}')
                {
                    if (token.Length == 0)
                    {
                        token.Append(c);
                    }
                    else
                    {
                        mReader.UngetChar();
                    }
                    return token.ToString();
                }
                token.Append(c);
                if (c == ':' && !toEndOfLine)
                {
                    break;
                }
            }
            string result = toEndOfLine ? token.ToString().Trim(' ', '\t', '\r', '\n') : token.ToString();
            if (result.Length == 0 && mReader.EndOfFile() && mFileStack.Count > 1)
            {
                mFileStack.RemoveAt(mFileStack.Count - 1);
                mReader = mFileStack[mFileStack.Count - 1];
                return GetToken(toEndOfLine);
            }
            return result;
        }

        private bool IncludeFile(string theFileName)
        {
            string dir = mReader.mFileDir;
            TextReader reader = new TextReader();
            string path = (dir.Length > 0 ? dir + "/" : "") + theFileName;
            if (!reader.Open(path))
            {
                return Error("File not found: " + theFileName + "\n", false, true);
            }
            reader.mFileName = System.IO.Path.GetFileName(theFileName);
            reader.mFileDir = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
            mFileStack.Add(reader);
            mReader = reader;
            return true;
        }

        private bool DoReadNext()
        {
            string token = GetToken(false);
            if (token.Length == 0)
            {
                return false;
            }
            if (token[token.Length - 1] == ':')
            {
                mKey = token.Substring(0, token.Length - 1);
                mKeyLine = mReader.mLineNum;
                mValue = GetToken(true);
                mType = ItemType.KeyValue;
                if (string.Equals(mKey, "IncludeFile", StringComparison.OrdinalIgnoreCase))
                {
                    if (IncludeFile(mValue))
                    {
                        return DoReadNext();
                    }
                    return false;
                }
                return true;
            }
            if (token == "}")
            {
                if (mGroupStack.Count == 0)
                {
                    return Error("Unexpected }", false, true);
                }
                mType = ItemType.GroupEnd;
                return true;
            }
            string groupType = token;
            string groupName = "";
            string next = GetToken(true);
            if (next == "=")
            {
                // "Key = [code]" form.
                mKey = groupType;
                mKeyLine = mReader.mLineNum;
                StringBuilder code = new StringBuilder();
                GetCode(code);
                mValue = code.ToString();
                mType = ItemType.KeyValue;
                return true;
            }
            if (next != "{")
            {
                if (next.Length > 0 && next[0] == '"')
                {
                    StringParser sp = new StringParser(next);
                    groupName = sp.ReadString(false, true);
                }
                else
                {
                    groupName = next;
                }
                if (GetToken(false) != "{")
                {
                    return Error("Expecting {", false, true);
                }
            }
            mGroupStack.Add(new GroupInfo { mType = groupType, mName = groupName });
            mType = ItemType.GroupStart;
            return true;
        }

        public bool ReadNext()
        {
            if (mError.Length != 0 && mValParser.mError.Length == 0)
            {
                return false;
            }
            if (mType == ItemType.GroupEnd && mGroupStack.Count > 0)
            {
                mGroupStack.RemoveAt(mGroupStack.Count - 1);
            }
            if (DoReadNext())
            {
                return true;
            }
            return mGroupStack.Count != 0;
        }
    }
}
