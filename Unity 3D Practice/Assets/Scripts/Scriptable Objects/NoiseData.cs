using UnityEngine;

[CreateAssetMenu(fileName = "New Noise Data", menuName = "Procedural Generation/Noise Data")]
public class NoiseData : UpdatableData
{
	[Header("NORMALIZE MODE"), Tooltip("Should the noise height range be persistent or unique?")]
	public NormalizeMode normalizeMode;

	[Header("MAP SEED"), Tooltip("A unique seed to generate a different map each time."), Space]
	public int seed;

	[Header("NOISE SETTINGS"), Space]
	[Min(.0001f)] public float scale;
	[Range(1, 10), Tooltip("The amount of octaves. Refers to the individual layers of the noise.")]
	public int octaves = 4;

	[Range(0f, 1f), Tooltip("Controls the Decreasing in Amplitude of each octave.")]
	public float persistence = .5f;

	[Min(1f), Tooltip("Controls the Increasing in Frequency of each octave.")]
	public float lacunarity = 2;

	[Tooltip("The offset values of sample points")]
	public Vector2 offset;
}
