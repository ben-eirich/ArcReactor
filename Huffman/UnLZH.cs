using System.Collections.Generic;

namespace ArcReactor {
    public sealed class UnLZH {
        const int NUM_SYMBOLS = 291;
        const int SYM_EOF = 256;
        const int SYM_NEWTREE = 257;
        const int SYM_BEGINMATCH = 258;

        BitReaderFwd Reader;
        HuffmanDecoder Decoder = new HuffmanDecoder();

        public byte[] Decompress(byte[] input) {
            Reader = new BitReaderFwd(input);
            var output = new List<byte>();
            int offset = 0;

            ReadHuffmanTable();
            while (true) {
                int symbol = Decoder.DecodeSymbol(Reader);
                if (symbol < 256) {
                    output.Add((byte)symbol);
                    offset++;
                } else if (symbol >= SYM_BEGINMATCH) {
                    int bits = symbol - SYM_BEGINMATCH;
                    int distance = Reader.ReadBits(bits) | (1 << bits);
                    int length = DecodeMatchLength();
                    while (length-- > 0) {
                        output.Add(output[offset - distance]);
                        offset++;
                    }
                } else if (symbol == SYM_NEWTREE) {
                    ReadHuffmanTable();
                } else if (symbol == SYM_EOF) {
                    break;
                }
            }
            return output.ToArray();
        }

        int DecodeMatchLength() {
            const int A_BITS = 2; //     1 xx
            const int B_BITS = 2; //    01 xx
            const int C_BITS = 2; //   001 xx
            const int D_BITS = 3; //  0001 xxx
            const int E_BITS = 5; // 00001 xxxxx
            const int F_BITS = 9; // 00000 xxxxxxxxx

            const int A = 1 << A_BITS;
            const int B = (1 << B_BITS) + A;
            const int C = (1 << C_BITS) + B;
            const int D = (1 << D_BITS) + C;
            const int E = (1 << E_BITS) + D;
            const int F = (1 << F_BITS) + E;

            int length = 0;
                 if (Reader.ReadBits(1) != 0) length = Reader.ReadBits(A_BITS);
            else if (Reader.ReadBits(1) != 0) length = Reader.ReadBits(B_BITS) + A;
            else if (Reader.ReadBits(1) != 0) length = Reader.ReadBits(C_BITS) + B;
            else if (Reader.ReadBits(1) != 0) length = Reader.ReadBits(D_BITS) + C;
            else if (Reader.ReadBits(1) != 0) length = Reader.ReadBits(E_BITS) + D;
            else                              length = Reader.ReadBits(F_BITS) + E;
            return length + 3; // MIN_MATCH
        }

        void ReadHuffmanTable() {
            var lengths = new int[NUM_SYMBOLS];
            int lastEmittedLength = -1;
            for (int i = 0; i < NUM_SYMBOLS;) {
                int readLength = Reader.ReadBits(4);
                if (readLength == 15) { // RLE code
                    int runLength = Reader.ReadBits(4) + 3;
                    while (runLength > 0) {
                        lengths[i++] = lastEmittedLength;
                        runLength--;
                    }
                } else {
                    lengths[i++] = readLength;
                    lastEmittedLength = readLength;
                }
            }
            Decoder.ApplyNewTableFromCodeLengths(lengths);
        }
    }
}
