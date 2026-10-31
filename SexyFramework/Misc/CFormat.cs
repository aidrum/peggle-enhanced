using System;
using System.Globalization;
using System.Text;

namespace Sexy
{
    /// <summary>
    /// printf-style formatting used by the ported C++ code (Sexy::StrFormat). Supports flags
    /// (-, 0, +, space), width, precision and the conversions d i u x X c s S f g e %.
    /// </summary>
    public static class CFormat
    {
        public static string StrFormat(string theFormat, params object[] theArgs)
        {
            StringBuilder sb = new StringBuilder();
            int argIdx = 0;
            int i = 0;
            while (i < theFormat.Length)
            {
                char c = theFormat[i++];
                if (c != '%')
                {
                    sb.Append(c);
                    continue;
                }
                if (i < theFormat.Length && theFormat[i] == '%')
                {
                    sb.Append('%');
                    i++;
                    continue;
                }
                bool left = false;
                bool zero = false;
                bool plus = false;
                bool space = false;
                while (i < theFormat.Length && "-0+ #".IndexOf(theFormat[i]) >= 0)
                {
                    switch (theFormat[i])
                    {
                    case '-':
                        left = true;
                        break;
                    case '0':
                        zero = true;
                        break;
                    case '+':
                        plus = true;
                        break;
                    case ' ':
                        space = true;
                        break;
                    }
                    i++;
                }
                int width = 0;
                while (i < theFormat.Length && char.IsDigit(theFormat[i]))
                {
                    width = width * 10 + (theFormat[i++] - '0');
                }
                int precision = -1;
                if (i < theFormat.Length && theFormat[i] == '.')
                {
                    i++;
                    precision = 0;
                    while (i < theFormat.Length && char.IsDigit(theFormat[i]))
                    {
                        precision = precision * 10 + (theFormat[i++] - '0');
                    }
                }
                while (i < theFormat.Length && "hlLqjzt".IndexOf(theFormat[i]) >= 0)
                {
                    i++;
                }
                if (i >= theFormat.Length)
                {
                    break;
                }
                char conv = theFormat[i++];
                object arg = argIdx < theArgs.Length ? theArgs[argIdx++] : null;
                string s;
                switch (conv)
                {
                case 'd':
                case 'i':
                case 'u':
                {
                    long v = Convert.ToInt64(arg ?? 0, CultureInfo.InvariantCulture);
                    s = Math.Abs(v).ToString(CultureInfo.InvariantCulture);
                    if (precision >= 0)
                    {
                        s = s.PadLeft(precision, '0');
                    }
                    string sign = v < 0 ? "-" : (plus ? "+" : (space ? " " : ""));
                    if (zero && !left && precision < 0)
                    {
                        s = s.PadLeft(Math.Max(0, width - sign.Length), '0');
                    }
                    s = sign + s;
                    break;
                }
                case 'x':
                case 'X':
                {
                    long v = Convert.ToInt64(arg ?? 0, CultureInfo.InvariantCulture);
                    s = ((uint)v).ToString(conv == 'x' ? "x" : "X", CultureInfo.InvariantCulture);
                    if (zero && !left)
                    {
                        s = s.PadLeft(width, '0');
                    }
                    break;
                }
                case 'c':
                    s = arg is char ch ? ch.ToString() : ((char)Convert.ToInt32(arg ?? 0)).ToString();
                    break;
                case 's':
                case 'S':
                    s = arg?.ToString() ?? "(null)";
                    if (precision >= 0 && s.Length > precision)
                    {
                        s = s.Substring(0, precision);
                    }
                    break;
                case 'f':
                case 'F':
                {
                    double v = Convert.ToDouble(arg ?? 0.0, CultureInfo.InvariantCulture);
                    s = v.ToString("F" + (precision < 0 ? 6 : precision), CultureInfo.InvariantCulture);
                    if (plus && v >= 0)
                    {
                        s = "+" + s;
                    }
                    if (zero && !left)
                    {
                        s = s.PadLeft(width, '0');
                    }
                    break;
                }
                case 'e':
                case 'E':
                {
                    double v = Convert.ToDouble(arg ?? 0.0, CultureInfo.InvariantCulture);
                    s = v.ToString((conv == 'e' ? "e" : "E") + (precision < 0 ? 6 : precision), CultureInfo.InvariantCulture);
                    break;
                }
                case 'g':
                case 'G':
                {
                    double v = Convert.ToDouble(arg ?? 0.0, CultureInfo.InvariantCulture);
                    s = v.ToString("G" + (precision < 0 ? 6 : Math.Max(precision, 1)), CultureInfo.InvariantCulture);
                    break;
                }
                default:
                    s = arg?.ToString() ?? "";
                    break;
                }
                if (s.Length < width)
                {
                    s = left ? s.PadRight(width) : s.PadLeft(width);
                }
                sb.Append(s);
            }
            return sb.ToString();
        }
    }
}
