namespace ArcReactor {
    public sealed class MetaHT3HC4 : IMatchFinder {
        HashTable3 ht3;
        HashChain4 hc4;
        int probeOffset;

        public MetaHT3HC4(byte[] data, int windowBits = 21, int maxProbes = 16) {
            Initialize(data, windowBits, maxProbes, windowBits);
        }

        public MetaHT3HC4(byte[] data, int windowBits, int maxProbes, int hashBits) {
            Initialize(data, windowBits, maxProbes, hashBits);
        }

        void Initialize(byte[] data, int windowBits, int maxProbes, int hashBits) {
            ht3 = new HashTable3(data, windowBits);
            hc4 = new HashChain4(data, windowBits, maxProbes, hashBits);
        }

        public bool FindFirst(int inputOffset, out int length, out int distance) {
            bool result = ht3.FindFirst(inputOffset, out length, out distance);
            if (result) {
                probeOffset = inputOffset;
                return true;
            } else {
                probeOffset = -1;
                return hc4.FindFirst(inputOffset, out length, out distance);
            }
        }

        public bool FindNext(out int length, out int distance) {
            if (probeOffset != -1) {
                bool result = hc4.FindFirst(probeOffset, out length, out distance);
                probeOffset = -1;
                return result;
            } else {

                return hc4.FindNext(out length, out distance);
            }
        }
    }
}
