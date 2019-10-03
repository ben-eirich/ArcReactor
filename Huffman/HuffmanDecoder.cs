using System;

// It would be nice to make this dynamically support huffman codes of an arbitry max length,
// but this implementation is currently fixed to a 14-bit max length. Would take a bit of work to generalize.

namespace ArcReactor {
    public sealed class HuffmanDecoder {
        const int MAX_SYMBOL_LENGTH = 14;
        int NumSymbols;
        DecodeEntry[] DecodeTable = new DecodeEntry[1 << MAX_SYMBOL_LENGTH];

        struct DecodeEntry {
            public short Symbol;
            public byte Bits;
        }

        public void ApplyNewTableFromCodeLengths(int[] lengths) {
            NumSymbols = lengths.Length;
            var codes = GenerateCodesFromLengths(lengths);
            GenerateDecodeTable(lengths, codes);

        }

        int[] GenerateCodesFromLengths(int[] lengths) {
            var codes = new int[NumSymbols];
            int minLength = 999;
            int maxLength = 0;

            var numCodesOfLength = new int[MAX_SYMBOL_LENGTH + 1];
            var codePrefix = new int[MAX_SYMBOL_LENGTH + 1];
            foreach (int length in lengths) {
                if (length == 0) continue;
                if (length < minLength) minLength = length;
                if (length > maxLength) maxLength = length;
                numCodesOfLength[length]++;
            }

            int nextCode = 0;
            for (int length = minLength; length <= maxLength; length++) {
                nextCode = (nextCode + numCodesOfLength[length - 1]) << 1;
                codePrefix[length] = nextCode;
            }

            for (int sym = 0; sym < NumSymbols; sym++) {
                int length = lengths[sym];
                if (length == 0) continue;
                codes[sym] = codePrefix[length];
                codePrefix[length]++;
            }
            return codes;
        }

        // This table is specific to MAX_LENGTH=14
        // If we make MAX_LENGTH variable, we'd need to dynamically generate it or replace it.
        static int[] leftOverBitsMask = {
            0x3FFF, 0x1FFF, 0x0FFF, 0x07FF, 0x03FF, 0x01FF, 0x00FF, 0x007F, 0x003F, 0x001F, 0x000F, 0x0007, 0x0003, 0x0001, 0x0000 };

        void GenerateDecodeTable(int[] lengths, int[] codes) {
            Array.Clear(DecodeTable, 0, DecodeTable.Length);
            for (int sym = 0; sym < NumSymbols; sym++) {
                int code = codes[sym];
                int length = lengths[sym];
                if (length == 0) continue;
                int leftAligned = code << (MAX_SYMBOL_LENGTH - length);
                int maxNumber = leftAligned | leftOverBitsMask[length];

                for (int i = leftAligned; i <= maxNumber; i++) {
                    DecodeTable[i] = new DecodeEntry {
                        Symbol = (short)sym,
                        Bits = (byte)length
                    };
                }
            }
        }

        public int DecodeSymbol(BitReaderFwd reader) {
            int preview = reader.PreviewBits(MAX_SYMBOL_LENGTH);
            var decode = DecodeTable[preview];
            reader.AdvanceBits(decode.Bits);
            return decode.Symbol;
        }
    }
}
