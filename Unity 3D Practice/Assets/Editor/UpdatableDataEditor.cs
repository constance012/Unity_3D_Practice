using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpdatableData), true)]
public class UpdatableDataEditor : Editor
{
	private UpdatableData _updatable;

	private void OnEnable()
	{
		_updatable = (UpdatableData)target;
	}

	public override void OnInspectorGUI()
	{
		bool onValuesChanged = DrawDefaultInspector();

		if (onValuesChanged && _updatable.autoUpdate)
			_updatable.Notify();

		GUILayout.Space(5f);

		if (GUILayout.Button("Update"))
			_updatable.Notify();
	}
}
