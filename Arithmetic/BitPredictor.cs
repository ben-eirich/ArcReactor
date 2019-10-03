// Models the probability of a single bit being 0 or 1.
// Two separate probabilities are tracked, a fast-adjusting probability and a slow-adjusting probability.
// The returned probability is a blend of the two values.

namespace ArcReactor {
    struct BitPredictor {
        public ushort FastP;
        public ushort SlowP;

        // Initializes probability to initial state; 50/50 probability of bit being 0 or 1
        public void Init() {
            FastP = 0x8000;
            SlowP = 0x8000;
        }

        // Returns the current probability estimate for the bit
        public uint P {
            get => (uint)(FastP + SlowP);
        }

        // Updates the probability counter to record the observation of a 0-bit
        public void Update0() {
            FastP -= (ushort)(FastP >> 3);
            SlowP -= (ushort)(SlowP >> 6);
        }

        // Updates the probability counter to record the observation of a 1-bit
        public void Update1() {
            FastP += (ushort)((FastP ^ 0xFFFF) >> 3);
            SlowP += (ushort)((SlowP ^ 0xFFFF) >> 6);
        }
    }
}
