using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon {

	public bool				automatic;
	public float			firerate;
	public float			timeLastFired;
	public float			projectileVelocity;
	public GameObject		projectile;

}
