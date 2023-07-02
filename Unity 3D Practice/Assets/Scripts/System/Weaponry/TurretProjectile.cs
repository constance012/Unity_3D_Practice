using UnityEngine;
using static UnityEngine.ParticleSystem;

public sealed class TurretProjectile : ProjectileBase
{
	private void OnCollisionEnter(Collision other)
	{
		flySpeed = 0f;

		ProcessCollision(other);

		Destroy(gameObject);
	}

	public override void ProcessCollision(Collision other)
	{
		ContactPoint contact = other.GetContact(0);

		if (other.rigidbody != null)
			other.rigidbody.AddForce(-contact.normal * impactForce);

		ParticleSystem impactFx = Instantiate(impactEffect);

		MainModule main = impactFx.GetMainModuleInChildren("Decal");
		main.customSimulationSpace = other.transform;

		impactFx.AlignWithNormal(contact.point, contact.normal);
		impactFx.Emit(1);
	}
}
