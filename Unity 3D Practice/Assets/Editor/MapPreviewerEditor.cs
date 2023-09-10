using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapPreviewer))]
public class MapPreviewerEditor : Editor
{
	private MapPreviewer _previewer;

	private SerializedObject _previewerObj;
	private SerializedProperty _showPreviewMesh;

	private void OnEnable()
	{
		_previewer = (MapPreviewer)target;

		_previewerObj = new SerializedObject(_previewer);
		_showPreviewMesh = _previewerObj.FindProperty("showPreviewMesh");
	}

	public override void OnInspectorGUI()
	{
		_previewerObj.Update();

		bool isShowed = EditorGUILayout.Toggle("Show Preview Mesh", _showPreviewMesh.boolValue);
		_showPreviewMesh.boolValue = isShowed;
		_previewer.meshRenderer.transform.parent.gameObject.SetActive(isShowed);

		GUI.enabled = isShowed;

		bool onValuesChanged = DrawDefaultInspector();

		if (onValuesChanged && _previewer.autoUpdate)
		{
			MapGenerator.Instance.OnValuesUpdated();
		}

		EditorGUILayout.Space(5f);

		if (GUILayout.Button("Regenerate Map"))
			MapGenerator.Instance.OnValuesUpdated();

		_previewerObj.ApplyModifiedProperties();
	}
}
