using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon {

	[Header("Firing Info")]
	public bool				automatic;
	public float			firerate;
	public float			timeLastFired;
	public float			kickLinear;
	public float			kickAngular;
	public int				burstCount;
	public float			burstDelay;

	[Space(10)]
	[Header("Projectile Info")]
	public float			projectileVelocity;
	public GameObject		projectile;

}
