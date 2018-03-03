using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IcosphereGenerator  {

	public static Vector3[] GetIcosphereVectors () {
		// Simply returns an array of Vector3s with directions representing an Icosphere
		Vector3[] newVectors = new Vector3[12];

		float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

		newVectors[0] = (new Vector3(-1, t, 0).normalized);
		newVectors[0] = (new Vector3(1, t, 0).normalized);
		newVectors[0] = (new Vector3(-1, -t, 0).normalized);
		newVectors[0] = (new Vector3(1, -t, 0).normalized);

		newVectors[0] = (new Vector3(0, -1, t).normalized);
		newVectors[0] = (new Vector3(0, 1, t).normalized);
		newVectors[0] = (new Vector3(0, -1, -t).normalized);
		newVectors[0] = (new Vector3(0, 1, -t).normalized);

		newVectors[0] = (new Vector3(t, 0, -1).normalized);
		newVectors[0] = (new Vector3(t, 0, 1).normalized);
		newVectors[0] = (new Vector3(-t, 0, -1).normalized);
		newVectors[0] = (new Vector3(-t, 0, 1).normalized);

		return newVectors;
	}

}
