using System;
using System.IO;
using System.Text;

namespace Sexy
{
    public class DataReaderException : Exception
    {
        public DataReaderException() : base("DataReader: read past end of data")
        {
        }

        public DataReaderException(string theMessage) : base(theMessage)
        {
        }
    }

    /// <summary>
    /// Binary reader for save files, profiles and replays (Sexy::DataReader). Little endian;
    /// strings are a u16 byte count followed by the bytes; wide strings are UTF-32 (iOS wchar_t).
    /// Bits are packed LSB first into bytes.
    /// </summary>
    public class DataReader
    {
        private byte[] mData;                                     // 0x08

        private int mDataLen;                                     // 0x10

        private int mPos;                                         // 0x14

        private byte mBitByte;                                    // 0x19

        private int mBitPos = 7;                                  // 0x1c

        public bool OpenFile(string theFileName)
        {
            Close();
            try
            {
                mData = System.IO.File.ReadAllBytes(theFileName);
                mDataLen = mData.Length;
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public void OpenMemory(byte[] theData, int theLength, bool takeOwnership)
        {
            mData = theData;
            mDataLen = theLength;
            mPos = 0;
            mBitByte = 0;
            mBitPos = 7;
        }

        public void Close()
        {
            mData = null;
            mDataLen = 0;
            mPos = 0;
            mBitByte = 0;
            mBitPos = 7;
        }

        public bool CanReadBytes(int theNumBytes)
        {
            // Unsigned, as in the original: a huge length must not wrap around and pass.
            return unchecked((uint)(mPos + theNumBytes)) <= (uint)mDataLen;
        }

        public void ReadBytes(byte[] theDest, int theOffset, int theCount)
        {
            mBitPos = 0;
            if (mData == null || mPos + theCount > mDataLen)
            {
                throw new DataReaderException();
            }
            System.Buffer.BlockCopy(mData, mPos, theDest, theOffset, theCount);
            mPos += theCount;
        }

        public byte[] ReadBytesFromMem(int theCount)
        {
            byte[] result = new byte[theCount];
            ReadBytes(result, 0, theCount);
            return result;
        }

        public bool ReadBit()
        {
            int bitPos = mBitPos;
            if (bitPos == 0)
            {
                mBitByte = ReadByte();
                bitPos = 0;
            }
            mBitPos = bitPos > 6 ? 0 : bitPos + 1;
            return (mBitByte & (1 << bitPos)) != 0;
        }

        public void EndBit()
        {
            mBitPos = 0;
        }

        public byte ReadByte()
        {
            mBitPos = 0;
            if (mData == null || mPos + 1 > mDataLen)
            {
                throw new DataReaderException();
            }
            return mData[mPos++];
        }

        public int ReadLong()
        {
            byte[] b = new byte[4];
            ReadBytes(b, 0, 4);
            return BitConverter.ToInt32(b, 0);
        }

        public short ReadShort()
        {
            byte[] b = new byte[2];
            ReadBytes(b, 0, 2);
            return BitConverter.ToInt16(b, 0);
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public float ReadFloat()
        {
            byte[] b = new byte[4];
            ReadBytes(b, 0, 4);
            return BitConverter.ToSingle(b, 0);
        }

        public double ReadDouble()
        {
            byte[] b = new byte[8];
            ReadBytes(b, 0, 8);
            return BitConverter.ToDouble(b, 0);
        }

        /// <summary>Narrow string (std::string, Latin-1/UTF-8 bytes).</summary>
        public string ReadString()
        {
            int len = (ushort)ReadShort();
            byte[] b = new byte[len];
            ReadBytes(b, 0, len);
            return Encoding.UTF8.GetString(b);
        }

        /// <summary>Wide string (std::wstring): byte count then UTF-32LE code units.</summary>
        public string ReadWString()
        {
            int len = (ushort)ReadShort();
            byte[] b = new byte[len];
            ReadBytes(b, 0, len);
            return Encoding.UTF32.GetString(b, 0, len & ~3);
        }

        public string ReadStringToWString()
        {
            return ReadString();
        }
    }

    /// <summary>Binary writer counterpart of <see cref="DataReader"/> (Sexy::DataWriter).</summary>
    public class DataWriter
    {
        private FileStream mFile;                                 // 0x04

        private byte[] mData;                                     // 0x08

        private int mDataLen;                                     // 0x0c

        private int mCapacity;                                    // 0x10

        private byte mBitByte;                                    // 0x14

        private int mBitPos;                                      // 0x18

        public byte[] GetDataPtr()
        {
            return mData;
        }

        public int GetDataLen()
        {
            return mDataLen;
        }

        public int GetPos()
        {
            return mDataLen;
        }

        public bool OpenFile(string theFileName)
        {
            Close();
            try
            {
                string dir = Path.GetDirectoryName(theFileName);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                mFile = new FileStream(theFileName, FileMode.Create, FileAccess.Write);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public void OpenMemory(int theReserve = 0x20)
        {
            Close();
            if (theReserve < 0x20)
            {
                theReserve = 0x20;
            }
            mData = new byte[theReserve];
            mCapacity = theReserve;
        }

        public void Close()
        {
            if (mFile != null)
            {
                if (mBitPos != 0)
                {
                    mFile.WriteByte(mBitByte);
                }
                mFile.Dispose();
                mFile = null;
            }
            mBitPos = 0;
            mData = null;
            mDataLen = 0;
            mCapacity = 0;
            mBitByte = 0;
        }

        public void WriteBytes(byte[] theData, int theOffset, int theCount)
        {
            if (mData == null)
            {
                if (mFile != null)
                {
                    if (mBitPos != 0)
                    {
                        mBitPos = 0;
                        mFile.WriteByte(mBitByte);
                    }
                    mFile.Write(theData, theOffset, theCount);
                }
                return;
            }
            if (mCapacity < mDataLen + theCount)
            {
                int cap = mCapacity;
                do
                {
                    cap <<= 1;
                }
                while (cap < mDataLen + theCount);
                Array.Resize(ref mData, cap);
                mCapacity = cap;
            }
            System.Buffer.BlockCopy(theData, theOffset, mData, mDataLen, theCount);
            mDataLen += theCount;
            mBitPos = 0;
        }

        public void WriteBytes(byte[] theData)
        {
            WriteBytes(theData, 0, theData.Length);
        }

        public void WriteByte(byte theByte)
        {
            WriteBytes(new[] { theByte }, 0, 1);
        }

        public void WriteBit(bool theBit)
        {
            if (mData != null)
            {
                if (mBitPos == 0)
                {
                    WriteBytes(new byte[1], 0, 1);
                }
                if (theBit)
                {
                    mData[mDataLen - 1] |= (byte)(1 << mBitPos);
                }
                int next = mBitPos + 1;
                mBitPos = next > 7 ? 0 : next;
                return;
            }
            if (mFile != null)
            {
                if (theBit)
                {
                    mBitByte |= (byte)(1 << mBitPos);
                }
                mBitPos++;
                if (mBitPos > 7)
                {
                    mFile.WriteByte(mBitByte);
                    mBitByte = 0;
                    mBitPos = 0;
                }
            }
        }

        public void EndBit()
        {
            if (mBitPos != 0)
            {
                if (mFile != null)
                {
                    mFile.WriteByte(mBitByte);
                    mBitByte = 0;
                }
                mBitPos = 0;
            }
        }

        public void WriteLong(int theValue)
        {
            WriteBytes(BitConverter.GetBytes(theValue));
        }

        public void WriteShort(short theValue)
        {
            WriteBytes(BitConverter.GetBytes(theValue));
        }

        public void WriteBool(bool theValue)
        {
            WriteByte(theValue ? (byte)1 : (byte)0);
        }

        public void WriteFloat(float theValue)
        {
            WriteBytes(BitConverter.GetBytes(theValue));
        }

        public void WriteDouble(double theValue)
        {
            WriteBytes(BitConverter.GetBytes(theValue));
        }

        public void WriteString(string theString)
        {
            byte[] b = Encoding.UTF8.GetBytes(theString ?? "");
            WriteShort((short)b.Length);
            WriteBytes(b);
        }

        public void WriteWString(string theString)
        {
            byte[] b = Encoding.UTF32.GetBytes(theString ?? "");
            int len = Math.Min(b.Length, 0x3fff << 2);
            WriteShort((short)len);
            WriteBytes(b, 0, len);
        }

        public void SetLong(int theValue, int thePos)
        {
            if (mData != null && thePos + 4 <= mCapacity)
            {
                byte[] b = BitConverter.GetBytes(theValue);
                System.Buffer.BlockCopy(b, 0, mData, thePos, 4);
            }
        }
    }
}
