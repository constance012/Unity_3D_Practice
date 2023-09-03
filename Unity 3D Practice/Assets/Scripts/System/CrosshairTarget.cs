using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrosshairTarget : MonoBehaviour
{
	private Transform _mainCam;

	private void Awake()
	{
		_mainCam = Camera.main.transform;
	}

	void Update()
	{
		Ray ray = new Ray(_mainCam.position, _mainCam.forward);

		if (Physics.Raycast(ray, out RaycastHit target, 1000f, RangedWeapon.LAYER_TO_RAYCAST))
			transform.position = target.point;
		
		// If we shoot into the air, move the target really far along the ray direction.
		else
			transform.position = ray.origin + ray.direction * 1000f;
	}
}
