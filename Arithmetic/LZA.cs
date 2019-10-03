// Primary alphabet is 512 symbols.
//   0..255: Literals
// 256..512: encode match and length of match.
// Secondary 32-symbol alphabet to encode match offset scales.

namespace ArcReactor {
    public sealed class LZA {

        // ---[ Arithmetic Coding ]-------------------------------------

        BitPredictor[] SymbolProbabilities = new BitPredictor[256 * 512];
        BitPredictor[] DistanceScaleProbabilities = new BitPredictor[32];
        BitEncoder Output;

        // ---[ Match Data ]--------------------------------------------

        int CachedPosition = -1;
        MatchInfo CachedMatch;
        IMatchFinder Matcher;

        struct MatchInfo {
            public int Length;
            public int Distance;
            public int Benefit;
        }

        // =============================================================
        //  Code
        // =============================================================

        public byte[] Compress(byte[] input) {
            Output = new BitEncoder();
            Matcher = new HashChain8(input, 26, 32);
            for (int i = 0; i < SymbolProbabilities.Length; i++) SymbolProbabilities[i].Init();
            for (int i = 0; i < DistanceScaleProbabilities.Length; i++) DistanceScaleProbabilities[i].Init();
            
            int inputSize = input.Length;
            int inputOffset = 1;

            EncodeSymbol(input[0], 0); // Emit the first byte as a literal with no context.

            while (inputOffset <= inputSize) {
                var match = FindBestMatch(inputOffset);
                bool matchAvailable = match.Benefit > 0;
                
                if (matchAvailable) {
                    var nextMatch = PeekNextMatch(inputOffset + 1);
                    if (nextMatch.Benefit > match.Benefit)
                        matchAvailable = false;
                }

                if (matchAvailable) {
                    if (matchAvailable) {
                        if (match.Length > 255)
                            match.Length = 255;
                        EncodeSymbol(256 + match.Length, input[inputOffset - 1]);
                        EncodeDistance(match.Distance);
                        inputOffset += match.Length;
                    }
                } else {
                    EncodeSymbol(input[inputOffset], input[inputOffset - 1]);
                    inputOffset++;
                }
                if (inputOffset == inputSize) break;
            }
            return Output.GetBytes();
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
            return (matchLength * 4) - MatchCost(matchDistance);
        }

        int MatchCost(int matchDistance) {
            int bits = CountBits(matchDistance);
            return 4 + (bits - 1);
        }

        static int CountBits(int offset) {
            int bits = 0;
            while (offset != 0) {
                bits++;
                offset >>= 1;
            }
            return bits;
        }

        void EncodeSymbol(int sym, byte context) {
            int ctx = 1;
            while (ctx < 512) {
                bool bit = (sym & 256) != 0;
                sym += sym;
                Output.EncodeBit(bit, ref SymbolProbabilities[(context * 512) + ctx]);
                ctx += ctx + (bit ? 1 : 0);
            }
        }

        void EncodeFixedBits(int number, int bits) {
            while (bits-- > 0) {
                Output.EncodeBitFixed((number & 1) != 0);
                number >>= 1;
            }
        }

        void EncodeDistance(int offset) {
            int bits = CountBits(offset) - 1;
            EncodeDistanceScale(bits);
            EncodeFixedBits(offset, bits);
        }

        void EncodeDistanceScale(int len) {
            int ctx = 1;
            while (ctx < 32) {
                bool bit = (len & 16) != 0;
                len += len;
                Output.EncodeBit(bit, ref DistanceScaleProbabilities[ctx]);
                ctx += ctx + (bit ? 1 : 0);
            }
        }
    }
}
