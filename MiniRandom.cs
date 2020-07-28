// Tiny and not-so-great random number generator.
// We want to avoid System.Random.
struct MiniRandom
{
    private uint _val;

    public MiniRandom(uint seed)
    {
        _val = seed;
    }

    public uint Next()
    {
        _val ^= (_val << 13);
        _val ^= (_val >> 7);
        _val ^= (_val << 17);
        return _val;
    }
}
