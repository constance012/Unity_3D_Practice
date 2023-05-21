using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrosshairTarget : MonoBehaviour
{
	private Transform mainCam;

	private void Awake()
	{
		mainCam = Camera.main.transform;
	}

	void Update()
	{
		RaycastHit target;
		Ray ray = new Ray(mainCam.position, mainCam.forward);

		if (Physics.Raycast(ray, out target))
			transform.position = target.point;
		
		// If we shoot into the air, move the target really far along the ray direction.
		else
			transform.position = ray.origin + ray.direction * 1000f;
	}
}
