using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static GenerableObject;

public class PrefabGenerator : Singleton<PrefabGenerator>
{
	[Header("Generable Objects Data"), Space]
	public List<GenerableObject> generables;

	[Header("Configurations"), Space]
	public Transform rootParent;

	public GenerableData GenerateData(int index, Bounds rendererBound, AnimationCurve heightCurve, float heightMultiplier)
	{
		if (generables.Count == 0)
		{
			Debug.LogError("Please asign all the fields above before hitting Generate.");
			return default;
		}

		AnimationCurve threadSafeCurve = new AnimationCurve(heightCurve.keys);
		GenerableObject generable = generables[index];

		generable.SetSampleRange(rendererBound);

		Vector2 heightRange = generable.CalculateHeightRange(threadSafeCurve, heightMultiplier);
		
		System.Random prng = new System.Random();

		if (generable.sampleMode == SampleMode.Random)
		{
			Vector2[] samplePoints = new Vector2[generable.density];
			
			for (int i = 0; i < generable.density; i++)
			{
				float sampleX = prng.NextFloat(generable.sampleXRange.x, generable.sampleXRange.y);
				float sampleZ = prng.NextFloat(generable.sampleZRange.x, generable.sampleZRange.y);

				samplePoints[i] = new Vector2(sampleX, sampleZ);
			}
			
			return new GenerableData(index, samplePoints, heightRange);
		}

		else
		{
			List<Vector2> samplePoints = new List<Vector2>();

			float chunkScale = MapGenerator.Instance.terrainData.chunkScale;

			int minHeight = (int)(generable.sampleZRange.x / chunkScale);
			int maxHeight = (int)(generable.sampleZRange.y / chunkScale);

			int minWidth = (int)(generable.sampleXRange.x / chunkScale);
			int maxWidth = (int)(generable.sampleXRange.y / chunkScale);

			for (int y = minHeight; y < maxHeight; y++)
			{
				for (int x = minWidth; x < maxWidth; x++)
				{
					Vector2 samplePoint = new Vector2(x * chunkScale, y * chunkScale);
					samplePoints.Add(samplePoint);
				}
			}

			return new GenerableData(index, samplePoints, heightRange);
		}
	}

	#if UNITY_EDITOR
	public void GenerateInEditor(Bounds rendererBound, AnimationCurve heightCurve, float heightMultiplier)
	{
		if (generables.Count == 0 || rootParent == null)
		{
			Debug.LogError("Please asign all the fields above before hitting Generate.");
			return;
		}

		ClearAll();

		for	(int i = 0; i < generables.Count; i++)
		{
			GenerableData data = GenerateData(i, rendererBound, heightCurve, heightMultiplier);
			generables[i].Generate(rootParent, data);
		}
	}

	public void ClearAll()
	{
		while (rootParent.childCount != 0)
		{
			DestroyImmediate(rootParent.GetChild(0).gameObject);
		}

		GrassPainter editorPainter = FindObjectOfType<GrassPainter>();

		editorPainter.ClearMesh();
	}
	#endif
}
