using System;
using System.Collections.Generic;
using System.IO;

/*
    Projects:
      General:
       + Implement archiver CLI
       + Design archiver file format
       + Implement Binary Search Tree and benchmark; at what probe count do Hash-chains lose to binary trees?
        
      Huffman:
       X Fix bug in Huffman tree generator and re-benchmark Huffman
       X Find the better way of encoding offsets and re-benchmark our LZH
       X Figure out htf to cost the LZH
     
      Arithmetic:
       X Prove that arithmetic-coding bits with 50% probability
       X Visualize/get greater understanding of probability adaptivity
       X Implement a classic LZ (non-ROLZ) with binary arithmetic coding
       + Properly implement BALZ/ROLZ and implement a decoder
         + For BALZ, determine filesize cutoff where Context Modelling is better than no context modelling
       + Implement a more competitive tryhard LZArithmethic algorithm, possibly melding LZ with ROLZ
 */

    // TODO: tweak/set additional compression levels. LZB levels are out of whack, etc
    // TODO: Probably re-visit all the compression levels after we have binary search trees working
    // TODO: Attempt to unify our BitWriters and only have one.  We can use the Forward one universally, but the Fast one is more elegant.... its just backwards...

namespace ArcReactor {
    class Program {
        static void Main(string[] args) {
            var files = new List<string> {
                "w:/benchmark/readme.md",
                "w:/benchmark/lua.c",
                "w:/benchmark/dickens",
                "w:/benchmark/mozilla",
                "w:/benchmark/mr",
                "w:/benchmark/nci",
                "w:/benchmark/ooffice",
                "w:/benchmark/osdb",
                "w:/benchmark/reymont",
                "w:/benchmark/samba",
                "w:/benchmark/sao",
                "w:/benchmark/webster",
                "w:/benchmark/xml",
                "w:/benchmark/x-ray",
                "w:/benchmark/enwik8",
                //"w:/benchmark/enwik9",
            };

            var overallStart = DateTime.Now;
            int totalSize = 0;
            int totalCompressed = 0;

            const bool validate = false;

            foreach (var file in files) {
                LogLine($"=== {file} ========================");
                var input = File.ReadAllBytes(file);
                LogLine($"                 Size: {input.Length:n0}");
                var start = DateTime.Now;
                //var output = new NibLZ().Compress(input);
                var output = new LZH().Compress(input, mode: LZH.LZHPerformance.BETTER);
                var end = DateTime.Now;
                LogLine($"           Compressed: {output.Length:n0} ({Util.Percent(output.Length, input.Length)})");
                LogLine($"                 Time: {end - start}");

                totalSize += input.Length;
                totalCompressed += output.Length;

                if (validate) {
                    var decomp = new UnLZH();
                    var decompressed = decomp.Decompress(output);
                    //var decompressed = decomp.Decompress(output, input.Length);
                    Console.WriteLine("round-tripped: " + decompressed.Length);
                    if (input.Length != decompressed.Length) throw new Exception("Validation failed");
                }
            }
            var overallEnd = DateTime.Now;
            LogLine($"Duration: {overallEnd - overallStart}, TotalCompressed {totalCompressed:n0} ({Util.Percent(totalCompressed, totalSize)})");
        }

        public static void LogLine(string msg) {
            Console.WriteLine(msg);
        }
    }
}
