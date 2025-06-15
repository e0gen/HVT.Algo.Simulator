namespace HVT.Simulator.Extensions;

public static class RandomExtensions
{
    public static double NextGaussian(this Random random)
    {
        return Generate(random);

        // Box-Muller transformation for normal distribution
        static double Generate(Random r)
        {
            var u1 = 1.0 - r.NextDouble();
            var u2 = 1.0 - r.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
}