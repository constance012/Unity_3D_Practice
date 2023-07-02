using UnityEngine;
using static UnityEngine.ParticleSystem;

public static class ParticleSystemExtensions
{
	public static MainModule GetMainModuleInChildren(this ParticleSystem parent, string childName)
	{
		ParticleSystem child = parent.transform.Find(childName).GetComponent<ParticleSystem>();
		return child.main;
	}

	public static void AlignWithNormal(this ParticleSystem origin, Vector3 point, Vector3 normal)
	{
		origin.transform.position = point;
		origin.transform.forward = normal;
	}
}
