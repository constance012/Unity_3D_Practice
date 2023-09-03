using System;

public static class SystemRandomExtensions
{
	public static float NextFloat(this Random prng, float min, float max)
	{
		return (float)prng.NextDouble() * (max - min) + min;
	}

	public static float NextFloat(this Random prng)
	{
		return (float)prng.NextDouble();
	}

	public static double NextDouble(this Random prng, double min, double max)
	{
		return prng.NextDouble() * (max - min) + min;
	}
}
