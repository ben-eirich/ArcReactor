namespace ArcReactor {
    public sealed class MetaHT3HC4HC8 : IMatchFinder {
        HashTable3 ht3;
        HashChain4 hc4;
        HashChain8 hc8;
        int probeOffset;
        int bestLen;
        bool hc4Initialized;
        bool hc8Initialized;

        public MetaHT3HC4HC8(byte[] data, int windowBits = 21, int maxProbes = 16) {
            Initialize(data, windowBits, maxProbes, windowBits);
        }

        public MetaHT3HC4HC8(byte[] data, int windowBits, int maxProbes, int hashBits) {
            Initialize(data, windowBits, maxProbes, hashBits);
        }

        void Initialize(byte[] data, int windowBits, int maxProbes, int hashBits) {
            ht3 = new HashTable3(data, windowBits);
            hc4 = new HashChain4(data, windowBits, maxProbes, hashBits);
            hc8 = new HashChain8(data, windowBits, maxProbes, hashBits);
        }

        public bool FindFirst(int inputOffset, out int length, out int distance) {
            bestLen = 0;
            hc4Initialized = false;
            hc8Initialized = false;
            probeOffset = inputOffset;
            bool result = ht3.FindFirst(inputOffset, out length, out distance);
            if (result) {
                bestLen = length;
                return true;
            } else {
                hc4Initialized = true;
                return hc4.FindFirst(inputOffset, out length, out distance);
            }
        }

        public bool FindNext(out int length, out int distance) {
            if (!hc4Initialized) {
                bool result = hc4.FindFirst(probeOffset, out length, out distance);
                bestLen = length;
                hc4Initialized = true;
                return result;
            } else {
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
}
