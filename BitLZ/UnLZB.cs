namespace ArcReactor {
    public sealed class UnLZB {
        BitReaderFast reader;

        public byte[] Decompress(byte[] input, int length) {
            reader = new BitReaderFast(input);
            byte[] output = new byte[length];
            int outputOffset = 0;
            while (outputOffset < length) {
                if (reader.ReadBits(1) == 0) { // Literal
                    output[outputOffset++] = (byte)reader.ReadBits(8);
                } else { // Match
                    int matchLen = DecodeMatchLength();
                    int offset = DecodeMatchOffset();
                    while (matchLen-- > 0) {
                        output[outputOffset] = output[outputOffset - offset];
                        outputOffset++;
                    }
                }
            }
            return output;
        }

        int DecodeMatchOffset() {
            int len2decode = reader.ReadBits(4);
            int bits2read = len2decode > 0 ? len2decode + 5 : 6;
            int rawoffset = reader.ReadBits(bits2read);
            if (len2decode > 0) {
                rawoffset |= 1 << bits2read;
            }
            return rawoffset + 1;
        }
               
        int DecodeMatchLength() {
            const int A_BITS = 2; // 1 xx
            const int B_BITS = 2; // 01 xx
            const int C_BITS = 2; // 001 xx
            const int D_BITS = 3; // 0001 xxx
            const int E_BITS = 5; // 00001 xxxxx
            const int F_BITS = 9; // 00000 xxxxxxxxx

            const int A = (1 << A_BITS);
            const int B = (1 << B_BITS) + A;
            const int C = (1 << C_BITS) + B;
            const int D = (1 << D_BITS) + C;
            const int E = (1 << E_BITS) + D;
            const int F = (1 << F_BITS) + E;
            const int MIN_MATCH = 3;

            int length = 0;
                 if (reader.ReadBits(1) != 0) length = reader.ReadBits(A_BITS);
            else if (reader.ReadBits(1) != 0) length = reader.ReadBits(B_BITS) + A;
            else if (reader.ReadBits(1) != 0) length = reader.ReadBits(C_BITS) + B;
            else if (reader.ReadBits(1) != 0) length = reader.ReadBits(D_BITS) + C;
            else if (reader.ReadBits(1) != 0) length = reader.ReadBits(E_BITS) + D;
            else                              length = reader.ReadBits(F_BITS) + E;
            return length + MIN_MATCH;
        }
    }
}
