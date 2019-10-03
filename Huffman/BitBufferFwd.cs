using System.Collections.Generic;

namespace ArcReactor {
    public sealed class BitWriterFwd {
        List<byte> Buffer = new List<byte>();
        int bitOffset = 7;
        byte current;

        public void WriteBits(int val, int bits) {
            for (int i = 0; i < bits; i++) {
                int bit = (val >> (bits - 1)) & 1;
                val <<= 1;

                if (bitOffset < 0) {
                    Buffer.Add(current);
                    current = 0;
                    bitOffset = 7;
                }
                current |= (byte)(bit << bitOffset);
                bitOffset--;
            }
        }

        public byte[] GetBuffer() {
            if (bitOffset != 7) {
                Buffer.Add(current);
            }
            return Buffer.ToArray();
        }
    }

    public sealed class BitReaderFwd {
        byte[] Buffer;
        int byteOffset;
        int bitsRemaining;
        byte current;

        public BitReaderFwd(byte[] buffer) {
            Buffer = buffer;
            byteOffset = 0;
            bitsRemaining = 8;
            current = Buffer[0];
        }

        public int ReadBits(int bits) {
            int val = 0;
            for (int i = 0; i < bits; i++) {
                if (bitsRemaining == 0) {
                    current = byteOffset + 1 < Buffer.Length ? (byte)Buffer[++byteOffset] : (byte)0;
                    bitsRemaining = 8;
                }
                int bit = (current >> 7) & 1;
                current <<= 1;
                bitsRemaining--;
                val <<= 1;
                val |= bit;
            }
            return val;
        }

        public int PreviewBits(int bits) {
            int byteOffset = this.byteOffset;
            int bitsRemaining = this.bitsRemaining;
            byte current = this.current;
            int val = 0;

            for (int i = 0; i < bits; i++) {
                if (bitsRemaining == 0) {
                    current = byteOffset + 1 < Buffer.Length ? (byte)Buffer[++byteOffset] : (byte)0;
                    bitsRemaining = 8;
                }
                int bit = (current >> 7) & 1;
                current <<= 1;
                bitsRemaining--;
                val <<= 1;
                val |= bit;
            }
            return val;
        }

        public void AdvanceBits(int bits) {
            while (bits > bitsRemaining) {
                bits -= bitsRemaining;
                current = byteOffset + 1 < Buffer.Length ? (byte)Buffer[++byteOffset] : (byte)0;
                bitsRemaining = 8;
            }

            if (bits <= bitsRemaining) {
                bitsRemaining -= bits;
                current <<= bits;
            }
        }

        public bool EOF() {
            return (byteOffset >= Buffer.Length);
        }
    }
}
