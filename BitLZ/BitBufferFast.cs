using System.Collections.Generic;

namespace ArcReactor {
    public sealed class BitWriterFast {
        int bitCount = 0;
        int bitBuffer = 0;
        List<byte> Buffer = new List<byte>();

        public void WriteBits(int val, int bits) {
            val &= (1 << bits) - 1;
            bitBuffer |= val << bitCount;
            bitCount += bits;
            while (bitCount >= 8) {
                Buffer.Add((byte)bitBuffer);
                bitBuffer >>= 8;
                bitCount -= 8;
            }
        }

        public byte[] GetBuffer() {
            WriteBits(0, 7);
            bitCount = 0;
            bitBuffer = 0;
            return Buffer.ToArray();
        }
    }

    public sealed class BitReaderFast {
        int bitCount = 0;
        int bitBuffer = 0;
        int bufferOffset = 0;
        byte[] Buffer;

        public BitReaderFast(byte[] buffer) {
            Buffer = buffer;
        }

        public int ReadBits(int bits) {
            while (bitCount < bits) {
                bitBuffer |= Buffer[bufferOffset++] << bitCount;
                bitCount += 8;
            }
            int val = bitBuffer & ((1 << bits) - 1);
            bitBuffer >>= bits;
            bitCount -= bits;
            return val;
        }
    }
}
