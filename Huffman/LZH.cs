using System;
using System.Collections.Generic;

// Huffman alphabet:
//     0..255 = Literals
//        256 = End of File
//        257 = New Huffman Table follows
//   258..290 = Match with 1 bit distance .. 26 bit distance
// For encoding match lengths, we use the static-huffman-style length encoding used in CRUSH.

namespace ArcReactor {
    public sealed class LZH {

        // ---[ Configuration ]-----------------------------------------

        bool LazyParse = true;    // TODO remove
        IMatchFinder Matcher;
        BitWriterFwd Output;

        // ---[ Huffman Configuration ]---------------------------------

        const int NUM_SYMBOLS = 291;
        const int CHUNK_SIZE = 256 * 1024;

        const int SYM_EOF = 256;
        const int SYM_NEWTREE = 257;
        const int SYM_MATCH = 258;

        // ---[ Match Length Coding ]-----------------------------------

        const int A_BITS = 2; //     1 xx
        const int B_BITS = 2; //    01 xx
        const int C_BITS = 2; //   001 xx
        const int D_BITS = 3; //  0001 xxx
        const int E_BITS = 5; // 00001 xxxxx
        const int F_BITS = 9; // 00000 xxxxxxxxx

        const int A = (1 << A_BITS);
        const int B = (1 << B_BITS) + A;
        const int C = (1 << C_BITS) + B;
        const int D = (1 << D_BITS) + C;
        const int E = (1 << E_BITS) + D;
        const int F = (1 << F_BITS) + E;

        const int MIN_MATCH = 3;
        const int MAX_MATCH = (F - 1) + MIN_MATCH;

        // ---[ Match Data ]--------------------------------------------

        struct MatchInfo {
            public int Length;
            public int Distance;
            public int Benefit;
        }

        int CachedPosition;
        MatchInfo CachedMatch;

        // ---[ Chunk Encoding ]----------------------------------------

        struct MatchData {
            public int Distance;
            public short Length;
            public byte DistanceBits;
        }

        int[] SymbolFreqs;
        ushort[] ChunkBuffer;
        int ChunkEnd;
        int ChunkOffset;

        Queue<MatchData> MatchEncodes;

        // =============================================================
        //  Code
        // =============================================================

        public enum LZHPerformance { L1_VeryFast, L2_VeryFast, L3_Fast, L4_Fast, NORMAL, BETTER, BEST }

        public byte[] Compress(byte[] input, int windowBits = 26, LZHPerformance mode = LZHPerformance.NORMAL) {
            switch (mode) {
                case LZHPerformance.L1_VeryFast: Matcher = new HashTable3(input, windowBits); LazyParse = false; break;
                case LZHPerformance.L2_VeryFast: Matcher = new HashTable3(input, windowBits); LazyParse = true;  break;
                case LZHPerformance.L3_Fast:     Matcher = new MetaHT3HC4(input, windowBits, 4); LazyParse = false; break;
                case LZHPerformance.L4_Fast:     Matcher = new MetaHT3HC4(input, windowBits, 4); LazyParse = true; break;
                case LZHPerformance.NORMAL:      Matcher = new MetaHT3HC4HC8(input, 26, 16); LazyParse = false; break;
                case LZHPerformance.BETTER:      Matcher = new MetaHT3HC4HC8(input, 26, 32); LazyParse = true; break;
            }

            int inputSize = input.Length;
            int inputOffset = 0;

            Output = new BitWriterFwd();
            CachedPosition = -1;
            CachedMatch = default;
            SymbolFreqs = new int[NUM_SYMBOLS];
            ChunkBuffer = new ushort[Math.Min(inputSize, CHUNK_SIZE)];
            MatchEncodes = new Queue<MatchData>();

            CalcChunkEnd(inputOffset, inputSize);
            while (true) {
                var match = FindBestMatch(inputOffset);
                bool matchAvailable = match.Benefit > 0;

                if (matchAvailable && LazyParse) {
                    var nextMatch = PeekNextMatch(inputOffset + 1);
                    if (nextMatch.Benefit > match.Benefit)
                        matchAvailable = false;
                }

                if (matchAvailable) {
                    if (match.Length > MAX_MATCH) match.Length = MAX_MATCH;
                    EmitMatch(match.Length, match.Distance);
                    inputOffset += match.Length;
                } else { // No match available, emit literal
                    EmitSymbol(input[inputOffset++]);
                }

                // Encode chunk if we've reached end of chunk
                if (inputOffset >= ChunkEnd) {
                    if (ChunkEnd == inputSize)
                        EmitSymbol(SYM_EOF);
                    else
                        EmitSymbol(SYM_NEWTREE);

                    EncodeChunk();
                    if (inputOffset >= inputSize) break;

                    CalcChunkEnd(inputOffset, inputSize);
                }
            }
            return Output.GetBuffer();
        }


        MatchInfo FindBestMatch(int offset) {
            if (offset == CachedPosition)
                return CachedMatch;
            var best = new MatchInfo();
            int length, distance;
            if (Matcher.FindFirst(offset, out length, out distance)) {
                best.Length = length;
                best.Distance = distance;
                best.Benefit = CalculateMatchBenefit(length, distance);

                while (Matcher.FindNext(out length, out distance)) {
                    int benefit = CalculateMatchBenefit(length, distance);
                    if (benefit > best.Benefit) {
                        best.Length = length;
                        best.Distance = distance;
                        best.Benefit = benefit;
                    }
                }
                return best;
            }
            return new MatchInfo();
        }

        MatchInfo PeekNextMatch(int offset) {
            var match = FindBestMatch(offset);
            CachedPosition = offset;
            CachedMatch = match;
            return match;
        }

        int CalculateMatchBenefit(int matchLength, int matchDistance) {
            if (matchLength < 3) return 0;
            int estLiteralCost = matchLength * 6;
            int distanceBits = CountBits(matchDistance) - 1;
            int estMatchCost = 6 + distanceBits + MatchLengthCost(matchLength);
            return estLiteralCost - estMatchCost;
        }

        void EmitSymbol(int sym) {
            ChunkBuffer[ChunkOffset++] = (ushort)sym;
            SymbolFreqs[sym]++;
        }

        void EmitMatch(int len, int distance) {
            int bits = CountBits(distance) - 1;
            EmitSymbol(SYM_MATCH + bits);
            MatchEncodes.Enqueue(new MatchData {
                Distance = distance,
                DistanceBits = (byte)bits,
                Length = (short)len
            });
        }

        void CalcChunkEnd(int inputOffset, int inputSize) {
            int bytesRemaining = inputSize - inputOffset;
            if (bytesRemaining < CHUNK_SIZE) {
                ChunkEnd = inputSize;
            } else if (bytesRemaining < CHUNK_SIZE * 2) {
                // Split the difference of the last two chunks, to avoid very small chunks
                ChunkEnd = inputOffset + (bytesRemaining / 2);
            } else {
                ChunkEnd = inputOffset + CHUNK_SIZE;
            }
            ChunkOffset = 0;
        }

        void EncodeMatchLength(int length) {
            int len = length - MIN_MATCH;
            if (len < A) {
                Output.WriteBits(1, 1); // 1
                Output.WriteBits(len, A_BITS);
            } else if (len < B) {
                Output.WriteBits(1, 2); // 01
                Output.WriteBits(len - A, B_BITS);
            } else if (len < C) {
                Output.WriteBits(1, 3); // 001
                Output.WriteBits(len - B, C_BITS);
            } else if (len < D) {
                Output.WriteBits(1, 4); // 0001
                Output.WriteBits(len - C, D_BITS);
            } else if (len < E) {
                Output.WriteBits(1, 5); // 00001
                Output.WriteBits(len - D, E_BITS);
            } else {
                Output.WriteBits(0, 5); // 00000
                Output.WriteBits(len - E, F_BITS);
            }
        }

        int MatchLengthCost(int length) {
            length -= MIN_MATCH;
            if (length < A) return 1 + A_BITS;
            if (length < B) return 2 + B_BITS;
            if (length < C) return 3 + C_BITS;
            if (length < D) return 4 + D_BITS;
            if (length < E) return 5 + E_BITS;
            return 5 + F_BITS;
        }

        static int CountBits(int offset) {
            int bits = 0;
            while (offset != 0) {
                bits++;
                offset >>= 1;
            }
            return bits;
        }

        // Huffman Tree Encoding:
        // - Max 14-bit symbol length = One nibble transmitted per symbol
        // - 0 means no code
        // - 15 is our RLE start symbol
        // - An RLE run would go as 3 nibbles: <Symbol> <RLE Command: 15> <Length of run>
        // - The lengh of run is transmitted as a nibble. 
        // - This takes a minimum of 2 nibbles so 3 is the shortest run worth encoding. 
        // - Therefore the range of the length of the run symbol is 3..18.

        // We don't try to do tree elision. The entire chunking thing is entirely an encoder-side
        // concept. We can emit a New Tree command whenever we want, so a more serious encoder could take
        // much greater efforts to be fancier with when to emit a new huffman tree.

        void EncodeHuffTree(HuffmanEncoder huffman) {
            int lastEmittedLength = -1;
            for (int i = 0; i < NUM_SYMBOLS;) {

                if (i < NUM_SYMBOLS - 4 &&
                    huffman.Symbols[i].Length == lastEmittedLength &&
                    huffman.Symbols[i + 1].Length == lastEmittedLength &&
                    huffman.Symbols[i + 2].Length == lastEmittedLength) {

                    Output.WriteBits(15, 4);  // Emit an RLE token
                    int runLength = 3;        // Determine run length 
                    for (; runLength < 18 && i + runLength < NUM_SYMBOLS; runLength++) {
                        if (huffman.Symbols[i + runLength].Length != lastEmittedLength) break;
                    }
                    Output.WriteBits(runLength - 3, 4);
                    i += runLength;
                } else {
                    int length = huffman.Symbols[i].Length;
                    lastEmittedLength = length;
                    Output.WriteBits(length, 4);
                    i++;
                }
            }
        }

        void EncodeChunk() {
            var huff = new HuffmanEncoder(SymbolFreqs, 14);
            EncodeHuffTree(huff);
            for (int i = 0; i < ChunkOffset; i++) {
                int symbol = ChunkBuffer[i];
                var sym = huff.Symbols[symbol];
                Output.WriteBits(sym.Code, sym.Length);
                if (symbol >= SYM_MATCH) {
                    var match = MatchEncodes.Dequeue();
                    Output.WriteBits(match.Distance, match.DistanceBits);
                    EncodeMatchLength(match.Length);
                }
            }
        }
    }
}
