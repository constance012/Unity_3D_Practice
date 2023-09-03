using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PrefabGenerator : Singleton<PrefabGenerator>
{
	[Header("Generable Objects Data"), Space]
	public List<GenerableObject> generables;

	[Header("Configurations"), Space]
	public Transform rootParent;

	private Queue<ThreadInfo<GenerableData>> _generableDataThreadInfos = new Queue<ThreadInfo<GenerableData>>();

	private void Update()
	{
		if (_generableDataThreadInfos.Count > 0)
		{
			for (int i = 0; i < _generableDataThreadInfos.Count; i++)
			{
				ThreadInfo<GenerableData> threadInfo = _generableDataThreadInfos.Dequeue();
				threadInfo.ExecuteCallback();
			}
		}
	}

	public void RequestPrefabGeneration(Bounds rendererBound, AnimationCurve heightCurve, float heightMultiplier, Action<GenerableData> callback)
	{
		ThreadStart threadStart = () => PrefabGenerationThread(rendererBound, heightCurve, heightMultiplier, callback);
		new Thread(threadStart).Start();
	}

	public void PrefabGenerationThread(Bounds rendererBound, AnimationCurve heightCurve, float heightMultiplier, Action<GenerableData> callback)
	{
		lock (_generableDataThreadInfos)
		{
			AnimationCurve threadSafeCurve = new AnimationCurve(heightCurve.keys);

			for (int i = 0; i < generables.Count; i++)
			{
				GenerableData data = GenerateData(i, rendererBound, threadSafeCurve, heightMultiplier);

				ThreadInfo<GenerableData> generableThreadInfo = new ThreadInfo<GenerableData>(data, callback);

				_generableDataThreadInfos.Enqueue(generableThreadInfo);
			}
		}
	}

	public GenerableData GenerateData(int index, Bounds rendererBound, AnimationCurve heightCurve, float heightMultiplier)
	{
		if (generables.Count == 0)
		{
			Debug.LogError("Please asign all the fields above before hitting Generate.");
			return default;
		}		

		GenerableObject generable = generables[index];

		generable.SetSampleRange(rendererBound);

		Vector2 heightRange = generable.CalculateHeightRange(heightCurve, heightMultiplier);
		Vector2[] samplePoints = new Vector2[generable.density];
		
		System.Random prng = new System.Random();

		for (int i = 0; i < generable.density; i++)
		{
			float sampleX = prng.NextFloat(generable.sampleXRange.x, generable.sampleXRange.y);
			float sampleZ = prng.NextFloat(generable.sampleZRange.x, generable.sampleZRange.y);

			samplePoints[i] = new Vector2(sampleX, sampleZ);
		}

		return new GenerableData(index, samplePoints, heightRange);
	}

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
	}
}
