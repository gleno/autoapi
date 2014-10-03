using System;
using System.Security.Cryptography;

namespace zeco.autoapi.Components
{
    public class CryptoRandom : Random
    {
        private readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private readonly byte[] _uint32Buffer = new byte[4];

        public override Int32 Next()
        {
            _rng.GetBytes(_uint32Buffer);
            return BitConverter.ToInt32(_uint32Buffer, 0) & 0x7FFFFFFF;
        }

        public override Int32 Next(Int32 maxValue)
        {
            if (maxValue < 0) throw new ArgumentOutOfRangeException("maxValue");
            return Next(0, maxValue);
        }

        public override Int32 Next(Int32 minValue, Int32 maxValue)
        {

            const long max = (1 + (Int64)UInt32.MaxValue);

            if (minValue > maxValue) throw new ArgumentOutOfRangeException("minValue");
            if (minValue == maxValue) return minValue;
            Int64 diff = maxValue - minValue;

            while (true)
            {
                _rng.GetBytes(_uint32Buffer);
                var rand = BitConverter.ToUInt32(_uint32Buffer, 0);

                var remainder = max%diff;
                if (rand < max - remainder)
                    return (Int32) (minValue + (rand%diff));
            }
        }

        public override double NextDouble()
        {
            _rng.GetBytes(_uint32Buffer);
            var rand = BitConverter.ToUInt32(_uint32Buffer, 0);
            return rand/(1.0 + UInt32.MaxValue);
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            _rng.GetBytes(buffer);
        }
    }
}