using UnityEngine;
using static MapGenerator;

[CreateAssetMenu(fileName = "New Terrain Data", menuName = "Procedural Generation/Terrain Data")]
public class TerrainData : UpdatableData
{
	[Header("MESH CONFIGURATIONS"), Space]
	[Min(1f), Tooltip("How big would each terrain chunk be?")]
	public float chunkScale = 2f;

	[Min(0f), Tooltip("A multiplier for the height of each vertex of the mesh.")]
	public float meshHeight;

	[Min(1f), Tooltip("GLOBAL NORMALIZE MODE ONLY: An estimate value to reduce the maximum possible noise height of the map.")]
	public float maxPossibleHeightCutoff;

	[Tooltip("Specifies how much the different terrain heights should be affected by the multiplier.")]
	public AnimationCurve meshHeightCurve;
	public bool useFlatShading;
	
	[Header("FALLOFF MAP"), Space]
	public FalloffShape falloffShape;
	[Tooltip("CUSTOM SHAPE ONLY: A curve to customize the shape of the falloff.")]
	public AnimationCurve customShapeCurve;

	[Range(1f, 10f)] public float falloffSmoothness;
	[Range(.1f, 10f)] public float falloffIntensity;

	[Tooltip("Invert the black and white areas of the map.")]
	public bool inverseMap;
	public bool useFalloffMap;
}
