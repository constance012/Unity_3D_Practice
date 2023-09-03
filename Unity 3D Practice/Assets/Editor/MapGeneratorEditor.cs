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
		bool onValueChanged = DrawDefaultInspector();

		if (onValueChanged && _generator.autoUpdate)
		{
			ManagePreviewObjects();

			if (_generator.useFalloffMap || _generator.drawMode == MapDrawMode.Falloff)
				_generator.GenerateFalloff();

			_generator.DrawMapInEditor();
		}

		GUILayout.Space(5f);

		if (GUILayout.Button("Generate"))
		{
			ManagePreviewObjects();

			if (_generator.useFalloffMap || _generator.drawMode == MapDrawMode.Falloff)
				_generator.GenerateFalloff();

			_generator.DrawMapInEditor();
		}
	}

	private void ManagePreviewObjects()
	{
		_previewer.ClearWater();

		if (_previewer != null)
		{
			if (_generator.drawMode == MapDrawMode.Mesh)
			{
				_previewer.textureRenderer.gameObject.SetActive(false);
				_previewer.meshRenderer.gameObject.SetActive(true);
			}
			else
			{
				_previewer.textureRenderer.gameObject.SetActive(true);
				_previewer.meshRenderer.gameObject.SetActive(false);
			}
		}
	}
}