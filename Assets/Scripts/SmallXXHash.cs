/**
  * A variant of the xxHash constants by Yann Collet.
  * Skips the algorithms steps 2, 3, and 4.
  */
public readonly struct SmallXXHash
{
    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    // store hashbits in an accumulator
    readonly uint accumulator;

    public SmallXXHash(uint accumulator)
    {
        // initialize accumulator  with a seed number + prime E
        this.accumulator = accumulator;
    }

    public static implicit operator SmallXXHash(uint accumulator) =>
        new SmallXXHash(accumulator);

    public static SmallXXHash Seed(int seed) => (uint)seed + primeE;

    // use avalanche effect, XXHash algorithm to mix the bits of the accumulator
    public static implicit operator uint(SmallXXHash hash)
    {
        uint avalanche = hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= primeB;
        avalanche ^= avalanche >> 13;
        avalanche *= primeC;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }

    public SmallXXHash Eat (int data) =>
        RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;

    // other variant of Eat
    public SmallXXHash Eat(byte data) => RotateLeft(accumulator + data * primeE, 11) * primeA;

    // vectcorized rotate left instruction
    // use bitshifting and place the overflow data to the back using OR
    static uint RotateLeft(uint data, int steps) => 
        (data << steps) | (data >> 32 - steps);
}

