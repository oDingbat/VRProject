using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProjectileAttributes {
	// Information that is passed onto the Projectile class to initialize a Projectile
	// Does not change after projectile is created

	[Space(10)][Header("Attributes")]
	public GameObject					prefabProjectile;		// The prefab of the projectile
	public int							damage;					// The base damage of the projectile
	public int							ricochetCountInitial;	// The maximum number of times a projectile can ricochet
	public float						ricochetAngleMax;		// The maximum angle projectiles can ricochet off of
	public float						velocityInitial;		// The forward velocity applied to all projectiles when initially fired
	public float						gravity;				// The percentage of gravity the projectile has
	public DecelerationType				decelerationType;		// The deceleration type of the projectile (normal or logarithmic)
	public float						deceleration;			// The deceleration applied to the projectiles
	public float						lifespan;				// The amount of time this projectile can exist before it breaks itself
	public bool							isSticky;				// Is this projectile sticky? (ie: arrows, sticky grenades, etc)
	public bool							isPhysicsBased;         // Does this projectile have realistic physics (ie: grandes)
	
	[Space(10)][Header("Sub Projectile Information")]
	public Projectile.ProjectileSpreadAttributes subProjectileSpreadAttributes;
	public ProjectileAttributes			subProjectileAttributes;
	public GameObject					subProjectile;
	
	public enum DecelerationType { Normal, Smooth }

	public ProjectileAttributes () {
		damage = 1;
		ricochetCountInitial = 0;
		ricochetAngleMax = 90;
		velocityInitial = 100;
		gravity = 0;
		decelerationType = DecelerationType.Normal;
		deceleration = 0;
		lifespan = 0;
		isSticky = false;
		isPhysicsBased = false;
	}

	public ProjectileAttributes (ProjectileAttributes copiedProjectileAttributes) {
		damage = copiedProjectileAttributes.damage;
		ricochetCountInitial = copiedProjectileAttributes.ricochetCountInitial;
		ricochetAngleMax = copiedProjectileAttributes.ricochetAngleMax;
		velocityInitial = copiedProjectileAttributes.velocityInitial;
		gravity = copiedProjectileAttributes.gravity;
		decelerationType = copiedProjectileAttributes.decelerationType;
		deceleration = copiedProjectileAttributes.deceleration;
		lifespan = copiedProjectileAttributes.lifespan;
		isSticky = copiedProjectileAttributes.isSticky;
		isPhysicsBased = copiedProjectileAttributes.isPhysicsBased;
	}

}
