using System;

namespace ArcReactor {
    public sealed class LZB {

        // ---[ Configuration ]-----------------------------------------

        bool LazyParse;
        IMatchFinder Matcher;
        BitWriterFast Writer;

        // ---[ Match Data ]--------------------------------------------

        struct MatchInfo {
            public int Length;
            public int Distance;
            public int Benefit;
        }

        int CachedPosition;
        MatchInfo CachedMatch;

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

        // =============================================================
        //  Code
        // =============================================================

        public enum LZBPerformance { L1_VeryFast, L2_VeryFast, L3_Fast, NORMAL, BETTER, BEST }

        public byte[] Compress(byte[] input, LZBPerformance mode = LZBPerformance.NORMAL) {
            switch (mode) {
                case LZBPerformance.L1_VeryFast: Matcher = new HashTable3(input, 21);        LazyParse = false; break;
                case LZBPerformance.L2_VeryFast: Matcher = new HashTable3(input, 21);        LazyParse = true;  break;
                case LZBPerformance.NORMAL:      Matcher = new MetaHT3HC4HC8(input, 21, 32); LazyParse = true;  break;
            }

            Writer = new BitWriterFast();
            CachedPosition = -1;
            int inputSize = input.Length;
            int inputOffset = 0;
            while (inputOffset < inputSize) {
                var match = FindBestMatch(inputOffset);
                bool matchAvailable = match.Benefit > 0;

                if (LazyParse && matchAvailable) {
                    var nextMatch = PeekNextMatch(inputOffset + 1);
                    if (nextMatch.Benefit > match.Benefit + 9)
                        matchAvailable = false;
                }

                if (matchAvailable) {
                    if (match.Length > MAX_MATCH)
                        match.Length = MAX_MATCH;
                    Writer.WriteBits(1, 1);
                    EncodeMatchLength(match.Length);
                    EncodeMatchOffset(match.Distance);
                    inputOffset += match.Length;
                } else {
                    Writer.WriteBits(input[inputOffset++] << 1, 9);
                }
            }
            return Writer.GetBuffer();
        }

        MatchInfo FindBestMatch(int offset) {
            if (offset == CachedPosition) return CachedMatch;
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
            int actualMatchLength = Math.Min(matchLength, MAX_MATCH);
            int literalCost = actualMatchLength * 9;
            int matchCost = 1 + MatchLengthCost(matchLength) + MatchOffsetCost(matchDistance);
            return literalCost - matchCost;
        }

        void EncodeMatchLength(int length) {
            int len = length - MIN_MATCH;
            if (len < A) {
                Writer.WriteBits(1, 1); // 1
                Writer.WriteBits(len, A_BITS);
            } else if (len < B) {
                Writer.WriteBits(1 << 1, 2); // 01
                Writer.WriteBits(len - A, B_BITS);
            } else if (len < C) {
                Writer.WriteBits(1 << 2, 3); // 001
                Writer.WriteBits(len - B, C_BITS);
            } else if (len < D) {
                Writer.WriteBits(1 << 3, 4); // 0001
                Writer.WriteBits(len - C, D_BITS);
            } else if (len < E) {
                Writer.WriteBits(1 << 4, 5); // 00001
                Writer.WriteBits(len - D, E_BITS);
            } else {
                Writer.WriteBits(0, 5); // 00000
                Writer.WriteBits(len - E, F_BITS);
            }
        }

        static int MatchLengthCost(int length) {
            length -= MIN_MATCH;
            if (length < A) return 1 + A_BITS;
            if (length < B) return 2 + B_BITS;
            if (length < C) return 3 + C_BITS;
            if (length < D) return 4 + D_BITS;
            if (length < E) return 5 + E_BITS;
            return 5 + F_BITS;
        }

        void EncodeMatchOffset(int offset) {
            offset--;
            int bits = CountBits(offset) - 6;
            if (bits < 0) bits = 0;
            Writer.WriteBits(bits, 4);
            Writer.WriteBits(offset, bits == 0 ? 6 : bits + 5);
        }

        static int MatchOffsetCost(int offset) {
            int offsetBits = CountBits(offset) - 6;
            if (offsetBits < 0) offsetBits = 0;
            return 4 + (offsetBits == 0 ? 6 : offsetBits + 5);
        }

        static int CountBits(int offset) {
            int bits = 0;
            while (offset != 0) {
                bits++;
                offset >>= 1;
            }
            return bits;
        }
    }
}

// L1_VeryFast:         Duration: 00:00:10.4152595, TotalCompressed 135,967,919 (43.58%)
// L2_VeryFast:         Duration: 00:00:13.6442469, TotalCompressed 128,119,831 (41.07%)

// Lazy HT3HC4HC8 32p : Duration: 00:01:43.7342536, TotalCompressed 96,906,987 (31.06%)