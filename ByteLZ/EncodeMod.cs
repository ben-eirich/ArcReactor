using System.Collections.Generic;

namespace ArcReactor {
    static class EncodeMod {
        public static int SizeEncodeMod5(int val) {
            if (val < 224) return 1;
            if (val < 7392) return 2;
            if (val < 236768) return 3;
            return 4;
        }

        public static int SizeEncodeMod7(int val) {
            if (val < 128) return 1;
            if (val < 16512) return 2;
            if (val < 2113664) return 3;
            return 4;
        }

        public static void EncodeMod5(this List<byte> buffer, int val) {
            const int upper = 256 - (1 << 5);
            while (val >= upper) {
                buffer.Add((byte) (upper | val));
                val = (val - upper) >> 5;
            }
            buffer.Add((byte)val);
        }

        public static void EncodeMod7(this List<byte> buffer, int val) {
            const int upper = 256 - (1 << 7);
            while (val >= upper) {
                buffer.Add((byte) (upper | val));
                val = (val - upper) >> 7;
            }
            buffer.Add((byte)val);
        }
    }
}
