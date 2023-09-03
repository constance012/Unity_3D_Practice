using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrefabGenerator))]
public class PrefabGeneratorEditor : Editor
{
	private PrefabGenerator _generator;
	private MapPreviewer _previewer;
	private MapGenerator _mapGenerator;

	private void OnEnable()
	{
		_generator = (PrefabGenerator)target;
		_previewer = _generator.GetComponentInSibling<MapPreviewer>();
		_mapGenerator = _generator.GetComponentInParent<MapGenerator>();
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		GUILayout.Space(10f);

		if (GUILayout.Button("Generate"))
		{
			Bounds previewBound = _previewer.meshRenderer.bounds;

			_generator.GenerateInEditor(previewBound, _mapGenerator.meshHeightCurve, _mapGenerator.meshHeight);
		}

		if (GUILayout.Button("Clear All"))
		{
			_generator.ClearAll();
		}
	}
}
