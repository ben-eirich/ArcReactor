using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArcReactor {
    public sealed class HuffmanEncoder {

        public int MaxCodeLength { get; private set; }
        public int NumSymbols { get; private set; }
        public HuffmanTableEntry[] Symbols;

        public HuffmanEncoder(int[] frequencies, int maxCodeLength) {
            MaxCodeLength = maxCodeLength;
            NumSymbols = frequencies.Length;
            PopulateSymbolTable(frequencies);
            ConstructTree();
            CalculateCodeLengths();
            LimitCodeLengths();
            GenerateCodes();
        }

        void PopulateSymbolTable(int[] frequencies) {
            Symbols = new HuffmanTableEntry[NumSymbols];
            for (int i = 0; i < NumSymbols; i++) {
                Symbols[i] = new HuffmanTableEntry { Symbol = i, Count = frequencies[i] };
            }
        }

        // TODO: This completely fails if there is only 1 observed symbol. We should fix this. 
        // However, in practice there should always be at least one literal symbol + EOF symbol (shouldn't be compressing 0 byte files).
        public void ConstructTree() {
            var symbolsToProcess = new List<HuffmanTableEntry>(Symbols.Where(s => s.Count > 0));
            while (symbolsToProcess.Count > 1) {
                // Pop the lowest count symbols off the list, and insert a tree node each time you loop.
                // Loop until you only have one symbol, the top of the tree.
                var lowest1 = symbolsToProcess[0];
                var lowest2 = symbolsToProcess[1];
                if (lowest1.Count > lowest2.Count) {
                    lowest1 = symbolsToProcess[1];
                    lowest2 = symbolsToProcess[0];
                }

                for (int i = 2; i < symbolsToProcess.Count; i++) {
                    if (symbolsToProcess[i].Count < lowest1.Count) {
                        lowest2 = lowest1;
                        lowest1 = symbolsToProcess[i];
                    } else if (symbolsToProcess[i].Count < lowest2.Count) {
                        lowest2 = symbolsToProcess[i];
                    }
                }
                // Now, lowest1 and lowest2 should point to the lowest two probability items in the list.
                // So, create a merged node with lowest two probability items.
                var merged = new HuffmanTableEntry {
                    Symbol = -1,
                    Count = lowest1.Count + lowest2.Count
                };
                lowest1.Parent = merged;
                lowest2.Parent = merged;

                // Insert merged node and pop off lowest
                symbolsToProcess.Remove(lowest1);
                symbolsToProcess.Remove(lowest2);
                symbolsToProcess.Add(merged);
            }
        }

        void CalculateCodeLengths() {
            foreach (var symbol in Symbols) {
                int codeLen = 0;
                var sym = symbol;
                while (sym.Parent != null) {
                    codeLen++;
                    sym = sym.Parent;
                }
                symbol.Length = codeLen;
            }
        }

        void LimitCodeLengths() {
            var symbols = new List<HuffmanTableEntry>(Symbols.Where(h => h.Count > 0).OrderBy(h => h.Length).ThenBy(h => h.Count));
            int numAppearingSymbols = symbols.Count;

            int maxk = (1 << MaxCodeLength);
            int k = 0;

            // Just aribitrarily shorten symbols with a longer length than desired.
            foreach (var sym in symbols) {
                sym.Length = Math.Min(sym.Length, MaxCodeLength);
                k += 1 << (MaxCodeLength - sym.Length);
            }

            // Now increase length of infrequently used symbols to make a valid tree.
            // TODO, this always takes at least one symbol all the way up to max, relying on step 3 to
            // fix it up. Can we adjust this to only increase by the minimum amount?
            for (int i = numAppearingSymbols - 1; i >= 0 && k > maxk; i--) {
                while (symbols[i].Length < MaxCodeLength) {
                    symbols[i].Length++;
                    k -= 1 << (MaxCodeLength - symbols[i].Length);
                }
            }

            // We now have a sub-optimal, but valid length-limited tree. The 3rd pass is to improve optimality only.
            for (int i = 0; i < numAppearingSymbols; i++) {
                while (k + (1 << (MaxCodeLength - symbols[i].Length)) <= maxk) {
                    k += 1 << (MaxCodeLength - symbols[i].Length);
                    symbols[i].Length--;
                }
            }
        }
        void GenerateCodes() {
            int minLength = 999;
            int maxLength = 0;

            var numCodesOfLength = new int[MaxCodeLength + 1];
            var codePrefix = new int[MaxCodeLength + 1];
            foreach (var symbol in Symbols) {
                int length = symbol.Length;
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

            foreach (var symbol in Symbols) {
                int length = symbol.Length;
                if (length == 0) continue;
                symbol.Code = codePrefix[length];
                codePrefix[length]++;
            }
        }

        public void Encode(BitWriterFwd writer, int symbol) {
            var sym = Symbols[symbol];
            writer.WriteBits(sym.Code, sym.Length);
        }
    }

    public class HuffmanTableEntry {
        public int Symbol;
        public int Count;
        public int Length;
        public int Code;
        public HuffmanTableEntry Parent;

        public string CodeString() {
            int code = Code;
            int length = Length;
            if (length == 0) return "-";

            var buf = new StringBuilder();
            int mask = 1 << (length - 1);
            for (int i = 0; i < length; i++) {
                buf.Append(((code & mask) == 0) ? '0' : '1');
                code <<= 1;
            }
            return buf.ToString();
        }
    }
}
