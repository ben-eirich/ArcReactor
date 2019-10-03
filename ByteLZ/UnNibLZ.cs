using System.Collections.Generic;

namespace ArcReactor {
    public sealed class UnNibLZ {
        
        // =============================================================
        //  Data
        // =============================================================

        byte[] Input;
        int InputSize;
        int InputOffset;
        int Nibble1, Nibble2;

        // =============================================================
        //  Code
        // =============================================================

        public byte[] Decompress(byte[] input) {
            Input = input;
            InputSize = input.Length;
            InputOffset = 0;
            var output = new List<byte>();
            int outputOffset = 0;
            Nibble1 = -1;
            Nibble2 = -1;

            while (InputOffset < InputSize) {
                int command = GetCommandNibble();

                if (command <= 7) { // Literal command
                    int count;
                    if (command < 7)
                        count = command + 1;
                    else
                        count = 8 + DecodeMod5();
                    if (InputOffset == InputSize) break;

                    while (count-- > 0) {
                        output.Add(input[InputOffset++]);
                        outputOffset++;
                    }
                } else { // Match command
                    command -= 8;
                    int length;
                    if (command < 7)
                        length = command + 3;
                    else
                        length = 10 + DecodeMod5();
                    int distance = DecodeMod7() + 1;

                    while (length-- > 0) {
                        byte data = output[outputOffset - distance];
                        output.Add(data);
                        outputOffset++;
                    }
                }
            }
            return output.ToArray();
        }

        int GetCommandNibble() {
            if (Nibble2 == -1) {
                int commandByte = Input[InputOffset++];
                Nibble1 = commandByte & 0x0F;
                Nibble2 = (commandByte >> 4) & 0x0F;
                return Nibble1;
            } else {
                int ret = Nibble2;
                Nibble2 = -1;
                return ret;
            }
        }

        int DecodeMod5() {
            const int upper = 256 - (1 << 5);
            int shift = 0;
            int val = 0;
            while (true) {
                byte b = Input[InputOffset++];
                val += b << shift;
                if (b < upper)
                    return val;
                shift += 5;
            }
        }

        int DecodeMod7() {
            const int upper = 256 - (1 << 7);
            int shift = 0;
            int val = 0;
            while (true) {
                byte b = Input[InputOffset++];
                val += b << shift;
                if (b < upper)
                    return val;
                shift += 7;
            }
        }
    }
}
