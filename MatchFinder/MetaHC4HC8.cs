namespace ArcReactor {
    public sealed class MetaHC4HC8 : IMatchFinder {
        HashChain4 hc4;
        HashChain8 hc8;
        int probeOffset;
        int bestLen;
        bool hc8Initialized;

        public MetaHC4HC8(byte[] data, int windowBits = 21, int maxProbes = 16) {
            Initialize(data, windowBits, maxProbes, windowBits);
        }

        public MetaHC4HC8(byte[] data, int windowBits, int maxProbes, int hashBits) {
            Initialize(data, windowBits, maxProbes, hashBits);
        }

        void Initialize(byte[] data, int windowBits, int maxProbes, int hashBits) {
            hc4 = new HashChain4(data, windowBits, maxProbes, hashBits);
            hc8 = new HashChain8(data, windowBits, maxProbes, hashBits);
        }

        public bool FindFirst(int inputOffset, out int length, out int distance) {
            bestLen = 0;
            hc8Initialized = false;
            probeOffset = inputOffset;
            bool result = hc4.FindFirst(inputOffset, out length, out distance);
            bestLen = length;
            return result;
        }

        public bool FindNext(out int length, out int distance) {
            if (bestLen < 8) {
                bool result = hc4.FindNext(out length, out distance);
                if (!result) {
                    bestLen = 9;
                    hc8Initialized = true;
                    return hc8.FindFirst(probeOffset, out length, out distance);
                }
                bestLen = length;
                return result;
            } else {
                if (!hc8Initialized) {
                    hc8Initialized = true;
                    return hc8.FindFirst(probeOffset, out length, out distance);
                } else {
                    return hc8.FindNext(out length, out distance);
                }
            }
        }
    }
}
