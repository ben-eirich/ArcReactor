using System;

namespace ArcReactor {
    public sealed class HashChain4 : IMatchFinder {
        int WindowBits;
        int WindowSize;
        int WindowMask;

        int HashBits;
        int HashSize;
        int HashMask;

        byte[] Data;
        int InputSize;
        int LastProcessedOffset;
        int MaxProbes;
        int ProbeOffset;
        int NumProbes;

        int[] HashTable;
        int[] ChainTable;

        int BestLength;
        int InputOffset;
        int Length;
        int Distance;

        public HashChain4(byte[] data, int windowBits = 21, int maxProbes = 16) {
            Initialize(data, windowBits, maxProbes, windowBits);
        }

        public HashChain4(byte[] data, int windowBits, int maxProbes, int hashBits) {
            Initialize(data, windowBits, maxProbes, hashBits);
        }

        void Initialize(byte[] data, int windowBits, int maxProbes, int hashBits) {
            Data = data;
            InputSize = data.Length;

            WindowBits = windowBits;
            WindowSize = 1 << WindowBits;
            WindowMask = WindowSize - 1;

            HashBits = hashBits;
            HashSize = 1 << HashBits;
            HashMask = HashSize - 1;

            HashTable = new int[HashSize];
            for (int i = 0; i < HashSize; i++)
                HashTable[i] = -1;

            ChainTable = new int[WindowSize];
            for (int i = 0; i < WindowSize; i++)
                ChainTable[i] = -1;

            LastProcessedOffset = -1;
            MaxProbes = maxProbes;
        }


        public bool FindFirst(int inputOffset, out int length, out int distance) {
            if (inputOffset < LastProcessedOffset) throw new Exception("Can't search backwards!");
            InputOffset = inputOffset;
            Length = -1;
            Distance = -1;
            NumProbes = 0;
            BestLength = 0;

            while (inputOffset - 1 > LastProcessedOffset) {
                LastProcessedOffset++;
                int hash = Hash(LastProcessedOffset);
                ChainTable[LastProcessedOffset & WindowMask] = HashTable[hash];
                HashTable[hash] = LastProcessedOffset;
            }

            bool hashFound = SearchHash();
            if (!hashFound) {
                // If the hash wasnt found at all, there's no point in continuing.
                length = 0;
                distance = 0;
                return false;
            }

            if (Length >= 4) {
                // We found a valid initial match, return it.
                BestLength = Length;
                length = Length;
                distance = Distance;
                return true;
            }
            // A hash was found, but a valid match was not found at that location (hash collision)
            // That does not mean there are no matches, it means we have to probe the chain.
            return FindNext(out length, out distance);
        }

        bool SearchHash() {
            int hash = Hash(InputOffset);
            ProbeOffset = HashTable[hash];
            if (ProbeOffset == -1) return false;
            Distance = InputOffset - ProbeOffset;
            if (Distance > WindowSize || ProbeOffset >= InputOffset) return false;
            Length = MatchLength(ProbeOffset);
            return true;
        }

        public bool FindNext(out int length, out int distance) {
            while (NumProbes < MaxProbes) {
                if (ChainTable[ProbeOffset & WindowMask] >= ProbeOffset) break;
                ProbeOffset = ChainTable[ProbeOffset & WindowMask];
                distance = InputOffset - ProbeOffset;
                if (distance > WindowSize || ProbeOffset < 0) break;
                length = MatchLength(ProbeOffset);
                NumProbes++;
                if (length > BestLength) {
                    BestLength = length;
                    return true;
                }
            }
            length = -1;
            distance = -1;
            return false;
        }

        int MatchLength(int offset) {
            int matchLength = 0;
            int inputOffset = InputOffset;
            while (inputOffset < InputSize && Data[offset++] == Data[inputOffset++])
                matchLength++;
            return matchLength;
        }

        int Hash(int idx) {
            if (idx > InputSize - 4) return 0;
            int val = Data[idx] | (Data[idx + 1] << 8) | (Data[idx + 2] << 16) | (Data[idx + 3] << 24);
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
