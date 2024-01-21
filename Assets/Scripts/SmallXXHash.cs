/**
  * A variant of the xxHash constants by Yann Collet.
  * Skips the algorithms steps 2, 3, and 4.
  */
public struct SmallXXHash
{
    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    // store hashbits in an accumulator
    uint accumulator;

    public SmallXXHash(int seed)
    {
        // initialize accumulator  with a seed number + prime E
        accumulator = (uint)seed + primeE;
    }

    // get final uint hash value and return the accumulator
    // implicit to directly assign a SmallXXHash value to a uint and convert implicitly
    //without having to write uint infront of it
    public static implicit operator uint(SmallXXHash hash) => hash.accumulator;
}

