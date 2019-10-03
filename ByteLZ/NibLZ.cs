using System;
using System.Collections.Generic;

namespace ArcReactor {
    public sealed class NibLZ {

        // ---[ Configuration ]-----------------------------------------

        bool LazyParse;
        IMatchFinder Matcher;

        // ---[ Input/Outbut Buffers ]----------------------------------

        List<byte> Output = new List<byte>();

        // ---[ Match Data ]--------------------------------------------

        struct MatchInfo {
            public int Length;
            public int Distance;
            public int Benefit;
        }

        int CachedPosition;
        MatchInfo CachedMatch;
        const int MIN_MATCH_LENGTH = 3; // not modifiable

        // =============================================================
        //  Code
        // =============================================================

        public enum NibLZPerformance { VERY_FAST, FAST, NORMAL, BETTER, BEST }

        public byte[] Compress(byte[] input, int windowBits = 26, NibLZPerformance mode = NibLZPerformance.NORMAL) {
            if (windowBits < 15 || windowBits > 28) throw new ArgumentException("Invalid window size");

            switch (mode) {
                case NibLZPerformance.VERY_FAST: Matcher = new HashTable3(input, windowBits);        LazyParse = false; break;
                case NibLZPerformance.FAST:      Matcher = new HashChain3(input, windowBits, 4);     LazyParse = false; break;
                case NibLZPerformance.NORMAL:    Matcher = new MetaHT3HC4(input, windowBits, 16);    LazyParse = true;  break;
                case NibLZPerformance.BETTER:    Matcher = new MetaHT3HC4HC8(input, windowBits, 16); LazyParse = true;  break;
                case NibLZPerformance.BEST:      Matcher = new MetaHT3HC4HC8(input, windowBits, 64); LazyParse = true;  break;
            }

            // Initialize state vars
            Output.Clear();
            int inputSize = input.Length;
            CachedPosition = -1;
            SecondNibbleAvailable = false;
            SecondNibbleOffset = 0;

            int inputOffset = 0;
            int literalCount = 0;

            // Compression loop
            while (inputOffset <= inputSize) {
                var match = FindBestMatch(inputOffset);
                bool matchAvailable = match.Benefit > 0;

                if (matchAvailable && LazyParse) {
                    var nextMatch = PeekNextMatch(inputOffset + 1);
                    if (literalCount != 7 && nextMatch.Benefit > match.Benefit)
                        matchAvailable = false;
                }

                if (matchAvailable || (inputOffset == inputSize)) { // Time to write a command
                    if (literalCount > 0) { // Emit literal packet if we've accumulated any literals
                        EmitLiteralCommand(literalCount);
                        for (int literalOffset = inputOffset - literalCount; literalOffset < inputOffset; literalOffset++)
                            Output.Add(input[literalOffset]);
                        literalCount = 0;
                    }

                    if (matchAvailable) { // Emit match if available
                        EmitMatchCommand(match.Length);
                        Output.EncodeMod7(match.Distance - 1);
                        inputOffset += match.Length;
                    }
                    if (inputOffset == inputSize) break;
                } else {
                    inputOffset++;
                    literalCount++;
                }
            }
            return Output.ToArray();
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
            if (matchLength < MIN_MATCH_LENGTH)
                return 0;

            int matchCostNibbles = 1;
            int literalCostNibbles = matchLength * 2;

            if (matchLength >= MIN_MATCH_LENGTH + 7)
                matchCostNibbles += EncodeMod.SizeEncodeMod5(matchLength - 10) * 2;

            matchCostNibbles += EncodeMod.SizeEncodeMod7(matchDistance) * 2;
            return literalCostNibbles - matchCostNibbles;
        }

        // ---[ Commands ]----------------------------------------------

        void EmitLiteralCommand(int literalLen) {
            if (literalLen < 8) {
                EmitCommandNibble(literalLen - 1);
            } else {
                EmitCommandNibble(7);
                Output.EncodeMod5(literalLen - 8);
            }
        }

        void EmitMatchCommand(int matchLen) {
            matchLen -= MIN_MATCH_LENGTH;
            if (matchLen < 7) {
                EmitCommandNibble(matchLen + 8);
            } else {
                EmitCommandNibble(15);
                Output.EncodeMod5(matchLen - 7);
            }
        }

        bool SecondNibbleAvailable = false;
        int SecondNibbleOffset;

        void EmitCommandNibble(int nibble) {
            if (!SecondNibbleAvailable) {
                SecondNibbleOffset = Output.Count;
                Output.Add((byte)nibble);
                SecondNibbleAvailable = true;
            } else {
                int originalCommand = Output[SecondNibbleOffset];
                Output[SecondNibbleOffset] = (byte)(originalCommand | (nibble << 4));
                SecondNibbleAvailable = false;
            }
        }
    }
}

// VERY FAST:  Duration: 00:00:10.3481870, TotalCompressed 144,788,756 (46.41%)   (HT3 greedy)
// FAST:       Duration: 00:00:20.5427658, TotalCompressed 127,024,475 (40.72%)   (HC3 greedy 4 probes)
// NORMAL:     Duration: 00:01:10.6974630, TotalCompressed 110,637,627 (35.46%)   (HT3 HC4 lazy 16 probe)
// BETTER:     Duration: 00:01:51.5946884, TotalCompressed 105,466,334 (33.81%)   (HT3 HC4 HC8 lazy 16 probe)
// BEST:       Duration: 00:03:41.9011917, TotalCompressed 103,972,711 (33.33%)   (HT3 HC4 HC8 lazy 64 probes)