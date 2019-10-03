namespace ArcReactor {
    public sealed class UnLZA {

        BitPredictor[] SymbolProbabilities = new BitPredictor[256 * 512];
        BitPredictor[] DistanceSizeProbabilities = new BitPredictor[32];
        BitDecoder Decoder;

        public byte[] Decompress(byte[] input, int len) {
            for (int i = 0; i < SymbolProbabilities.Length; i++) SymbolProbabilities[i].Init();
            for (int i = 0; i < DistanceSizeProbabilities.Length; i++) DistanceSizeProbabilities[i].Init();

            Decoder = new BitDecoder(input);
            var output = new byte[len];

            output[0] = (byte)DecodeSymbol(0);
            for (int i = 1; i < len;) {
                int symbol = DecodeSymbol(output[i - 1]);
                if (symbol < 256) { // Literal
                    output[i++] = (byte)symbol;
                } else { // Match
                    int matchLen = symbol - 256;
                    int distance = DecodeDistance();
                    while (matchLen-- > 0) {
                        byte data = output[i - distance];
                        output[i++] = data;
                    }
                }
            }
            return output;
        }

        int DecodeSymbol(byte context) {
            int ctx = 1;
            while (ctx < 512) {
                ctx += ctx + Decoder.Decode(ref SymbolProbabilities[(context * 512) + ctx]);
            }
            return ctx - 512;
        }

        int DecodeFixedBits(int bits) {
            int val = 0;
            for (int shift = 0; bits > 0; shift++, bits--) {
                int bit = Decoder.DecodeFixed();
                val |= bit << shift;
            }
            return val;
        }

        int DecodeDistanceScale() {
            int ctx = 1;
            while (ctx < 32) {
                ctx += ctx + Decoder.Decode(ref DistanceSizeProbabilities[ctx]);
            }
            return ctx - 32;
        }

        int DecodeDistance() {
            int bits = DecodeDistanceScale();
            return DecodeFixedBits(bits) | (1 << bits);
        }
    }
}
