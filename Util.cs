using System;

namespace ArcReactor {
    class Util {
        // TODO re-evaluate what needs to be in here
        public static int WindowBitsFromFilesize(int filesize, int maxWindowBits) {
            int bits = 0;
            while (filesize != 0) {
                bits++;
                filesize >>= 1;
            }
            return Math.Min(bits, maxWindowBits);
        }

        public static string Percent(int amt, int total) {
            double percentage = (double)amt / total * 100d;
            return string.Format($"{percentage:0.0#}%");
        }
    }
}
