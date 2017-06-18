using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon {

	[Header("Firing Info")]
	public bool							automatic;
	public float						firerate;
	public float						timeLastFired;
	public int							burstCount;
	public float						burstDelay;

	[Space(10)]
	[Header("Accuracy Info")]
	public float						accuracyCurrent;				// The current accuracy of the weapon
	[Range (0, 1)]
	public float						accuracyMax;                    // The maximum accuracy the weapon can have
	[Range(0, 1)]
	public float						accuracyMin;                    // The minimum accuracy the weapon can have
	public float						accuracyIncrement;				// The amount of accuracy added to accuracyCurrent per second
	public float						accuracyDecrement;              // The amount of accuracy subtracted from accuracyCurrent per fire

	[Space(10)]
	[Header("Recoil Info")]
	public float						recoilLinear;					// The amount of velocity added to the weapon when fired
	public float						recoilAngular;					// The amount of angularVelocity added to the weapon when fired

	[Space(10)]
	[Header("Projectile Info")]
	public Vector2[]					projectileSpreads;              // The projectile spreads of the weapon. Each index represents an individual spread the the rotation of that index being applied to it. Used for multi-projectile based weapons (ie: shotguns)
	public SpreadType					projectileSpreadType;
	public enum SpreadType				{ Circular, Custom }
	public float						projectileSpreadDeviation;		// The random rotation deviation given to each projectile in projectile spreads
	public int							projectileRicochetCount;		// The maximum number of times a projectile can ricochet
	public float						projectileRicochetAngleMax;		// The maximum angle projectiles can ricochet off of
	public float						projectileVelocity;             // The forward velocity applied to all projectiles when fired
	public float						projectileGravity;				// The percentage of gravity the projectile has
	public Projectile.DecelerationType	projectileDecelerationType;		// The deceleration type of the projectile (normal or logarithmic)
	public float						projectileDeceleration;			// The deceleration applied to the projectiles
	public GameObject					projectile;						// The projectile prefab which is instantiated when firing the weapon

	[Space(10)]
	[Header("Audio Info")]
	public AudioClip		soundFireNormal;                // The normal audio clip player when firing the weapon

}
