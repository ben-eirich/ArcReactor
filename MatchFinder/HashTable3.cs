namespace ArcReactor {
    public sealed class HashTable3 : IMatchFinder {
        int WindowBits;
        int WindowSize;

        int HashBits;
        int HashSize;
        int HashMask;

        byte[] Data;
        int InputSize;
        int LastProcessedOffset;

        int[] HashTable;

        public HashTable3(byte[] data, int windowBits = 21) {
            Initialize(data, windowBits, windowBits);
        }

        public HashTable3(byte[] data, int windowBits, int hashBits) {
            Initialize(data, windowBits, hashBits);
        }

        void Initialize(byte[] data, int windowBits, int hashBits) {
            Data = data;
            InputSize = data.Length;

            WindowBits = windowBits;
            WindowSize = 1 << WindowBits;

            HashBits = hashBits;
            HashSize = 1 << HashBits;
            HashMask = HashSize - 1;

            HashTable = new int[HashSize];
            for (int i = 0; i < HashSize; i++)
                HashTable[i] = -1;

            LastProcessedOffset = -1;
        }

        public bool FindFirst(int inputOffset, out int length, out int distance) {
            length = -1;
            distance = -1;

            while (inputOffset - 1 > LastProcessedOffset) {
                LastProcessedOffset++;
                HashTable[Hash(LastProcessedOffset)] = LastProcessedOffset;
            }

            int hash = Hash(inputOffset);
            int offset = HashTable[hash];
            if (offset == -1) return false;
            distance = inputOffset - offset;
            if (distance > WindowSize || offset >= inputOffset) return false;
            length = MatchLength(offset, inputOffset);
            return length >= 3;
        }

        public bool FindNext(out int length, out int distance) {
            length = -1;
            distance = -1;
            return false;
        }

        int MatchLength(int offset, int inputOffset) {
            int matchLength = 0;
            while (inputOffset < InputSize && Data[offset++] == Data[inputOffset++])
                matchLength++;
            return matchLength;
        }

        int Hash(int idx) {
            if (idx > InputSize - 3) return 0;
            int val = Data[idx] | (Data[idx + 1] << 8) | (Data[idx + 2] << 16);
            int x = val;
            x ^= x >> 18;
            x *= unchecked((int)0xA136AAAD);
            x ^= x >> 16;
            x *= unchecked((int)0x9F6D62D7);
            x ^= x >> 17;
            return x & HashMask;
        }
    }
}
