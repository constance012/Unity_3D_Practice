using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class PoissonDiscSampling
{
	public static List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize, int maxSamplingSteps = 30)
	{
		float cellSize = radius / Mathf.Sqrt(2f);
		
		int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
		List<Vector2> points = new List<Vector2>();
		List<Vector2> spawnPoints = new List<Vector2>();

		spawnPoints.Add(sampleRegionSize / 2);
		while (spawnPoints.Count > 0)
		{
			int index = Random.Range(0, spawnPoints.Count);
			Vector2 center = spawnPoints[index];

			bool validCandidate = false;
			for (int i = 0; i < maxSamplingSteps; i++)
			{
				float angle = Random.value * Mathf.PI * 2;
				Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				Vector2 candidatePoint = center + direction * Random.Range(radius, radius * 2);

				if (IsValid(candidatePoint, radius, cellSize, sampleRegionSize, grid, points))
				{
					points.Add(candidatePoint);
					spawnPoints.Add(candidatePoint);
					
					// Save this point index to the grid, with 1-based index.
					grid[(int)(candidatePoint.x / cellSize), (int)(candidatePoint.y / cellSize)] = points.Count;
					validCandidate = true;
					break;
				}
			}
			
			if (!validCandidate)
				spawnPoints.RemoveAt(index);
		}

		return points;
	}

	private static bool IsValid(Vector2 candidate, float radius, float cellSize, Vector2 sampleRegionSize, int[,] grid, List<Vector2> points)
	{
		if (candidate.x >= 0f && candidate.x < sampleRegionSize.x && candidate.y >= 0f && candidate.y < sampleRegionSize.y)
		{
			Vector2Int posOnGrid = new Vector2Int((int)(candidate.x / cellSize), (int)(candidate.y / cellSize));
			Vector2Int searchRangeX = new Vector2Int(Mathf.Max(0, posOnGrid.x - 2), Mathf.Min(grid.GetLength(0) - 1, posOnGrid.x + 2));
			Vector2Int searchRangeY = new Vector2Int(Mathf.Max(0, posOnGrid.y - 2), Mathf.Min(grid.GetLength(1) - 1, posOnGrid.y + 2));

			for (int x = searchRangeX.x; x <= searchRangeX.y; x++)
			{
				for (int y = searchRangeY.x; y <= searchRangeY.y; y++)
				{
					int currentIndex = grid[x, y] - 1;
					if (currentIndex != -1)
					{
						float sqrDistance = (candidate - points[currentIndex]).sqrMagnitude;
						
						// If the candidate is too close to the current point.
						if (sqrDistance < radius * radius)
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		return false;
	}
}