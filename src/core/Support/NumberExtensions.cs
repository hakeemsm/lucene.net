using System;
using System.Text;

namespace Lucene.Net.Support
{
    public static class NumberExtensions
    {
        public static int Signum(this long i)
        {
            if (i == 0)
                return 0;

            return i > 0 ? 1 : -1;
        }

        public static int NumberOfLeadingZeros(this int num)
        {
            if (num == 0)
                return 32;

            uint unum = (uint)num;
            int count = 0;
            int i;

            for (i = 0; i < 32; ++i)
            {
                if ((unum & 0x80000000) == 0x80000000)
                    break;

                count++;
                unum <<= 1;
            }

            return count;
        }

        public static int NumberOfLeadingZeros(this long num)
        {
            if (num == 0)
                return 64;

            ulong unum = (ulong)num;
            int count = 0;
            int i;

            for (i = 0; i < 64; ++i)
            {
                if ((unum & 0x8000000000000000L) == 0x8000000000000000L)
                    break;

                count++;
                unum <<= 1;
            }

            return count;
        }

        public static int NumberOfTrailingZeros(this int num)
        {
            if (num == 0)
                return 32;

            uint unum = (uint)num;
            int count = 0;
            int i;

            for (i = 0; i < 32; ++i)
            {
                if ((unum & 1) == 1)
                    break;

                count++;
                unum >>= 1;
            }

            return count;
        }

        public static int NumberOfTrailingZeros(this long num)
        {
            if (num == 0)
                return 64;

            ulong unum = (ulong)num;
            int count = 0;
            int i;

            for (i = 0; i < 64; ++i)
            {
                if ((unum & 1L) == 1L)
                    break;

                count++;
                unum >>= 1;
            }

            return count;
        }

        public static string ToBinaryString(this int value)
        {
            StringBuilder sb = new StringBuilder();

            var uval = (uint)value;

            for (int i = 0; i < 32; i++)
            {
                if ((uval & 0x80000000) == 0x80000000)
                    sb.Append('1');
                else
                    sb.Append('0');

                uval <<= 1;
            }

            return sb.ToString();
        }

        public static float IntBitsToFloat(this int value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }

        public static int FloatToIntBits(this float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }

        public static long DoubleToLongBits(this double value)
        {
            return BitConverter.ToInt64(BitConverter.GetBytes(value), 0);
        }

        public static long ToRawLongBits(this double value)
        {
            return BitConverter.DoubleToInt64Bits(value);
        }

        public static double LongBitsToDouble(this long value)
        {
            return BitConverter.Int64BitsToDouble(value);
        }

        public static int BitCount(this long value)
        {
            value = value - ((value >> 1) & 0x5555555555555555);
            value = (value & 0x3333333333333333) + ((value >> 2) & 0x3333333333333333);
            return (int)(unchecked(((value + (value >> 4)) & 0xF0F0F0F0F0F0F0F) * 0x101010101010101) >> 56);
        }
    }
}
