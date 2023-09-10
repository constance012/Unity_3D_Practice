using UnityEditor;
using UnityEngine;
using static MapGenerator;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
	private MapGenerator _generator;
	private MapPreviewer _previewer;

	//private SerializedObject _generatorObj;
	//private SerializedProperty _mapWidth;
	//private SerializedProperty _mapHeight;

	private void OnEnable()
	{
		_generator = (MapGenerator)target;
		_previewer = _generator.GetComponentInChildren<MapPreviewer>();
		
		//_generatorObj = new SerializedObject(_generator);
		//_mapWidth = _generatorObj.FindProperty("mapWidth");
		//_mapHeight = _generatorObj.FindProperty("mapHeight");
	}

	public override void OnInspectorGUI()
	{
		bool onValuesChanged = DrawDefaultInspector();

		if (onValuesChanged && _generator.autoUpdate)
		{
			_previewer.ManagePreviewObjects(_generator.drawMode);

			if (_generator.terrainData.useFalloffMap || _generator.drawMode == MapDrawMode.Falloff)
				_generator.GenerateFalloff();

			_generator.DrawMapInEditor();
		}

		GUILayout.Space(5f);

		if (GUILayout.Button("Generate"))
		{
			_previewer.ManagePreviewObjects(_generator.drawMode);

			if (_generator.terrainData.useFalloffMap || _generator.drawMode == MapDrawMode.Falloff)
				_generator.GenerateFalloff();

			_generator.DrawMapInEditor();
		}
	}
}