namespace AlloyClient.Networking;

public class RandomSeed {
    private uint _seed;

    public RandomSeed(uint seed) {
        _seed = seed;
    }

    public uint NextInt() => Gen();

    public double NextDouble() => Gen() / 2147483647.0;

    public uint NextIntRange(uint min, uint max) {
        return min == max ? min : min + Gen() % (max - min);
    }

    public double NextDoubleRange(double min, double max) {
        return min + (max - min) * NextDouble();
    }

    private uint Gen() {
        uint lb = 16807 * (_seed & 0xFFFF);
        uint hb = 16807 * (uint)((int)_seed >> 16);
        lb += (hb & 32767) << 16;
        lb += (uint)((int)hb >> 15);
        if (lb > 2147483647)
            lb -= 2147483647;
        return _seed = lb;
    }
}