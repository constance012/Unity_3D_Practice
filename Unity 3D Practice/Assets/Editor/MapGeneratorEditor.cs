using UnityEditor;
using UnityEngine;
using static MapGenerator;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
	private MapGenerator _generator;

	//private SerializedObject _generatorObj;
	//private SerializedProperty _mapWidth;
	//private SerializedProperty _mapHeight;

	private void OnEnable()
	{
		_generator = (MapGenerator)target;
		
		//_generatorObj = new SerializedObject(_generator);
		//_mapWidth = _generatorObj.FindProperty("mapWidth");
		//_mapHeight = _generatorObj.FindProperty("mapHeight");
	}

	public override void OnInspectorGUI()
	{
		bool onValueChanged = DrawDefaultInspector();

		if (onValueChanged && _generator.autoUpdate)
		{
			if (_generator.useFalloffMap || _generator.drawMode == MapDrawMode.Falloff)
				_generator.GenerateFalloff();

			_generator.DrawMapInEditor();
		}

		GUILayout.Space(5f);

		if (GUILayout.Button("Generate"))
		{
			if (_generator.useFalloffMap || _generator.drawMode == MapGenerator.MapDrawMode.Falloff)
				_generator.GenerateFalloff();

			_generator.DrawMapInEditor();
		}
	}
}