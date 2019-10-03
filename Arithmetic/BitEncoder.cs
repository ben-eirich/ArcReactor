using System.Collections.Generic;

namespace ArcReactor {
    sealed class BitEncoder {
        uint Low;
        uint High;
        List<byte> Buffer;

        public BitEncoder() {
            Low = 0;
            High = uint.MaxValue;
            Buffer = new List<byte>();
        }

        public void EncodeBit(bool bit, ref BitPredictor predictor) {
            uint mid = Low + (uint)(((ulong)High - Low) * (predictor.P << 15) >> 32);
            if (bit) {
                High = mid;
                predictor.Update1();
            } else {
                Low = mid + 1;
                predictor.Update0();
            }

            while ((Low ^ High) < (1 << 24)) {
                Buffer.Add((byte)(Low >> 24));
                Low <<= 8;
                High = (High << 8) | 255;
            }
        }

        public void EncodeBitFixed(bool bit) {
            uint mid = Low + (uint)(((ulong)High - Low) * (65535 << 15) >> 32);
            if (bit) {
                High = mid;
            } else {
                Low = mid + 1;
            }

            while ((Low ^ High) < (1 << 24)) {
                Buffer.Add((byte)(Low >> 24));
                Low <<= 8;
                High = (High << 8) | 255;
            }
        }

        public byte[] GetBytes() {
            for (int i = 0; i < 4; i++) { // Flush arithmetic stream
                Buffer.Add((byte)(Low >> 24));
                Low <<= 8;
            }
            return Buffer.ToArray();
        }
    }
}
