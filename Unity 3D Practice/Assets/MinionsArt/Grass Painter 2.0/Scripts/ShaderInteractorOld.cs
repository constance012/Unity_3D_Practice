using UnityEngine;

public class ShaderInteractorOld : MonoBehaviour
{
	// Update is called once per frame
	void Update()
	{
		Shader.SetGlobalVector("_PositionMoving", transform.position);
	}
}