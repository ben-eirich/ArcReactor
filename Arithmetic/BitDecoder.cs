namespace ArcReactor {
    sealed class BitDecoder {
        public uint Code;
        public uint Low;
        public uint High;
        public byte[] Buffer;
        public int BufPos;

        public BitDecoder(byte[] buffer) {
            Buffer = buffer;
            BufPos = 0;
            Low = 0;
            High = uint.MaxValue;
            Code = 0;
            for (int i = 0; i < 4; i++) {
                Code = (Code << 8) | Buffer[BufPos++];
            }
        }

        public int Decode(ref BitPredictor predictor) {
            uint mid = Low + (uint)(((ulong)High - Low) * (predictor.P << 15) >> 32);
            bool bit = (Code <= mid);
            if (bit) {
                High = mid;
                predictor.Update1();
            } else {
                Low = mid + 1;
                predictor.Update0();
            }

            while ((Low ^ High) < (1 << 24)) {
                Code = (Code << 8) | Buffer[BufPos++];
                Low <<= 8;
                High = (High << 8) | 255;
            }
            return bit ? 1 : 0;
        }

        public int DecodeFixed() {
            uint mid = Low + (uint)(((ulong)High - Low) * (65535 << 15) >> 32);
            bool bit = (Code <= mid);
            if (bit) {
                High = mid;
            } else {
                Low = mid + 1;
            }

            while ((Low ^ High) < (1 << 24)) {
                Code = (Code << 8) | Buffer[BufPos++];
                Low <<= 8;
                High = (High << 8) | 255;
            }
            return bit ? 1 : 0;
        }
    }
}
