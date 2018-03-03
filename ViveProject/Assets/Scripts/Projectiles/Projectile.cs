using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Projectile : MonoBehaviour {

	[Space(10)][Header("Collision Masks")]
	public LayerMask			collisionMask;

	[Space(10)][Header("Attributes")]
	public ProjectileAttributes			projectileAttributes;
	public ProjectileSpreadAttributes	projectileSpreadAttributes;

	[Space(10)][Header("Variables")]
	public Vector3				velocityCurrent;
	public Collider				projectileCollider;
	public int					ricochetCount;
	public bool					isBroken;

	[Space(10)][Header("Prefabs")]
	public GameObject prefabProjectileShattered;
	public GameObject prefabDamageText;

	[System.Serializable]
	public class ProjectileSpreadAttributes {
		// Information regarding the spreads of projectiles
		public Vector2[] spreads;						// The spreads of the weapon/projectile
		public SpreadType spreadType;               // The type of spread this projectile has
		public float spreadDeviation;               // The random rotation deviation given to each projectile in projectile spreads

		public enum SpreadType { Circular, Custom, Spherical }

		public ProjectileSpreadAttributes (Projectile.ProjectileSpreadAttributes copiedProjectileSpreadAttributes) {
			spreads = copiedProjectileSpreadAttributes.spreads;
			spreadType = copiedProjectileSpreadAttributes.spreadType;
			spreadDeviation = copiedProjectileSpreadAttributes.spreadDeviation;
		}
	}

	void Start () {
		StartCoroutine(AutoDestroy());
	}

	IEnumerator AutoDestroy () {
		yield return new WaitForSeconds(projectileAttributes.lifespan);
		if (prefabProjectileShattered == true) {
			GameObject newProjectileShattered = (GameObject)Instantiate(prefabProjectileShattered, transform.position, transform.rotation);
			if (newProjectileShattered.transform.Find("(Pieces)") && newProjectileShattered.transform.Find("(Pieces)").Find("(StationaryPiece)")) {
				Destroy(newProjectileShattered.transform.Find("(Pieces)").Find("(StationaryPiece)").GetComponent<Rigidbody>());
				newProjectileShattered.transform.Find("(Pieces)").Find("(StationaryPiece)").transform.parent = transform.parent;
			}

			Transform[] transformsInProjectileShattered = newProjectileShattered.transform.GetComponentsInChildren<Transform>();
			foreach (Transform tCurrent in transformsInProjectileShattered) {
				tCurrent.gameObject.layer = transform.gameObject.layer;
			}

			Destroy(gameObject);
		}
	}

	void Update () {
		if (isBroken == false) {
			UpdateProjectileMovement();
		}
	}

	public void UpdateProjectileMovement () {
		RaycastHit hit;
		if (Physics.Raycast(transform.position, velocityCurrent.normalized, out hit, velocityCurrent.magnitude * Time.deltaTime, collisionMask)) {

			EntityBone hitEntityBone = hit.transform.GetComponent<EntityBone>();
			Rigidbody hitRigidbody = hit.transform.GetComponent<Rigidbody>();
			Vector3 normalPerpendicular = velocityCurrent.normalized - hit.normal * Vector3.Dot(velocityCurrent.normalized, hit.normal);
			
			if (hitEntityBone == true) {        // If the object we hit has an EntityBone component, damage it's entity
				DamageHitEntity(hit, hitEntityBone);
			}

			if (hitRigidbody == true) {         // If the object we hit has a rigidbody component, apply a force at the hit position
				hitRigidbody.AddForceAtPosition(velocityCurrent.normalized * Mathf.Sqrt(velocityCurrent.magnitude) * Mathf.Sqrt(Vector3.Angle(normalPerpendicular, velocityCurrent.normalized) * 5000f), hit.point);
			}
			
			if (ricochetCount > 0 && Vector3.Angle(normalPerpendicular, velocityCurrent.normalized) <= projectileAttributes.ricochetAngleMax) {  // Does this projectile have ricochets left and is the contact and less than or equal to the ricochetAngleMax?
				// Ricochet
				ricochetCount--;		// Subtract one ricochet from the total amount left
				transform.position = hit.point + (hit.normal * 0.001f);
				transform.rotation = Quaternion.LookRotation(Vector3.Reflect(velocityCurrent.normalized, hit.normal));
				velocityCurrent = Vector3.Reflect(velocityCurrent, hit.normal);
			} else {	// Don't have any ricochets left or the angle bad?
				
				if (projectileAttributes.isSticky == true) {       // Is this projectile sticky?
					
					projectileCollider.enabled = true;      // Enable the projectile's collider
					SetProjectileLayer("Climbable");

					if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {		// If the hit object is an Item
						if (hit.transform.parent && hit.transform.parent.parent != null && hit.transform.parent.parent.GetComponent<Item>()) {
							if (hit.transform.parent.parent.Find(("(Attachments)"))) {
								transform.parent = hit.transform.parent.parent.transform.Find("(Attachments)").transform;
							} else {
								GameObject newCollidersGameObject = new GameObject();
								newCollidersGameObject.transform.name = "(Attachments)";
								newCollidersGameObject.transform.parent = hit.transform.parent.parent;
								newCollidersGameObject.transform.localPosition = Vector3.zero;
								newCollidersGameObject.transform.localEulerAngles = Vector3.zero;

								transform.parent = newCollidersGameObject.transform;
							}
						} else {
							if (hit.transform.Find(("(Attachments)"))) {
								transform.parent = hit.transform.Find("(Attachments)").transform;
							} else {
								GameObject newCollidersGameObject = new GameObject();
								newCollidersGameObject.transform.name = "(Attachments)";
								newCollidersGameObject.transform.parent = hit.transform;
								newCollidersGameObject.transform.localPosition = Vector3.zero;
								newCollidersGameObject.transform.localEulerAngles = Vector3.zero;

								transform.parent = newCollidersGameObject.transform;
							}
						}

						gameObject.AddComponent<Misc>();
						Destroy(this);
					} else if (hit.transform.gameObject.layer == LayerMask.NameToLayer("EnvironmentItem")) {
						transform.position = hit.point;
						SetParentToTransform(hit.transform);

						SetProjectileLayer("ProjectileSticky");

						StartCoroutine(BreakProjectile(hit.point));
					} else {
						transform.position = hit.point;
						SetParentToTransform(hit.transform);
						StartCoroutine(BreakProjectile(hit.point));
					}
				} else {                    // Not sticky? then break this projectile
					StartCoroutine(BreakProjectile(hit.point));
				}
				
			}
		} else {
			// If the projectile did not hit anything
			transform.position += velocityCurrent * Time.deltaTime;
			velocityCurrent += new Vector3(0, projectileAttributes.gravity * Time.deltaTime, 0);
			if (projectileAttributes.decelerationType == ProjectileAttributes.DecelerationType.Normal) {
				velocityCurrent = velocityCurrent.normalized * Mathf.Clamp(velocityCurrent.magnitude - (projectileAttributes.deceleration * Time.deltaTime), 0, Mathf.Infinity);
			} else {
				velocityCurrent = Vector3.Lerp(velocityCurrent, Vector3.zero, projectileAttributes.deceleration * Time.deltaTime);
			}
			if (velocityCurrent.magnitude <= 5 && projectileAttributes.velocityInitial > 50) {
				StartCoroutine(BreakProjectile(transform.position));
			}
		}
	}

	void SetParentToTransform (Transform newParent) {
		if (newParent.transform.Find("[StickyProjectiles]") == true) {      // Does the new parent have a child container called "[StickyProjectiles]"
			transform.parent = newParent.transform.Find("[StickyProjectiles]");
		} else {
			Transform newStickyProjectileContainer = new GameObject("[StickyProjectiles]").transform;
			newStickyProjectileContainer.parent = newParent;
			newStickyProjectileContainer.transform.localPosition = Vector3.zero;
			newStickyProjectileContainer.transform.localEulerAngles = Vector3.zero;
			newStickyProjectileContainer.transform.localScale = new Vector3(1 / newParent.transform.localScale.x, 1 / newParent.transform.localScale.y, 1 / newParent.transform.localScale.z);
			transform.parent = newStickyProjectileContainer;
		}
	}

	void SetProjectileLayer (string layerName) {
		Transform[] transformsInProjectile = transform.GetComponentsInChildren<Transform>();
		foreach (Transform tCurrent in transformsInProjectile) {
			tCurrent.gameObject.layer = LayerMask.NameToLayer(layerName);
		}
	}

	void DamageHitEntity (RaycastHit hit, EntityBone hitEntityBone) {
		int damageCalculated = (int)Mathf.Round((float)projectileAttributes.damage * hitEntityBone.damageMultiplier);
		hitEntityBone.entity.TakeDamage(damageCalculated, (damageCalculated / 25) * hitEntityBone.stunMultiplier, hit.point, hit.normal);
		StartCoroutine(BreakProjectile(transform.position));
	}

	IEnumerator BreakProjectile(Vector3 finalPos) {
		if (isBroken == false) {
			transform.position = finalPos;
			isBroken = true;
			yield return new WaitForSeconds(GetComponent<TrailRenderer>().time * 0.9f);

			// Shoot sub-Projectiles
			if (projectileAttributes.subProjectile != null && projectileAttributes.subProjectileAttributes != null) {
				FireSubProjectiles();
			}
			
			if (projectileAttributes.isSticky == false) {
				Destroy(gameObject);
			}
		} else {
			yield return null;
		}
	}

	void FireSubProjectiles () {
		for (int j = 0; (j < (projectileSpreadAttributes.spreadType == ProjectileSpreadAttributes.SpreadType.Spherical ? 12 : projectileSpreadAttributes.spreads.Length) || (projectileSpreadAttributes.spreads.Length == 0 && j == 0)); j++) {
			
			// Get random spread deviations
			Quaternion projectileSpreadDeviation = Quaternion.Euler(Random.Range(-projectileSpreadAttributes.spreadDeviation, projectileSpreadAttributes.spreadDeviation), Random.Range(-projectileSpreadAttributes.spreadDeviation, projectileSpreadAttributes.spreadDeviation), 0);

			// Step 4: Create new projectile
			GameObject newProjectile = (GameObject)Instantiate(projectileAttributes.prefabProjectile, transform.position + transform.forward * 0.01f, transform.rotation);
			if (projectileSpreadAttributes.spreads.Length > 0) {
				if (projectileSpreadAttributes.spreadType == Projectile.ProjectileSpreadAttributes.SpreadType.Circular) {
					newProjectile.transform.rotation *= projectileSpreadDeviation * Quaternion.Euler(0, 0, projectileSpreadAttributes.spreads[j].x) * Quaternion.Euler(projectileSpreadAttributes.spreads[j].y, 0, 0);
				} else if (projectileSpreadAttributes.spreadType == Projectile.ProjectileSpreadAttributes.SpreadType.Custom) {
					newProjectile.transform.rotation *= Quaternion.Euler(projectileSpreadAttributes.spreads[j].y, projectileSpreadAttributes.spreads[j].x, 0);
				} else if (projectileSpreadAttributes.spreadType == Projectile.ProjectileSpreadAttributes.SpreadType.Spherical) {
					Vector3[] icosphereVectors = IcosphereGenerator.GetIcosphereVectors();
					newProjectile.transform.rotation *= Quaternion.LookRotation(icosphereVectors[j], Vector3.up);
				}
			}
			Projectile newProjectileClass = newProjectile.GetComponent<Projectile>();
			// Apply velocity
			newProjectileClass.velocityCurrent = newProjectile.transform.forward * projectileAttributes.velocityInitial;
			newProjectileClass.projectileAttributes.deceleration = projectileAttributes.deceleration;
			newProjectileClass.projectileAttributes.decelerationType = projectileAttributes.decelerationType;
			newProjectileClass.projectileAttributes.gravity = projectileAttributes.gravity;
			newProjectileClass.ricochetCount = projectileAttributes.ricochetCountInitial;
			newProjectileClass.projectileAttributes.ricochetAngleMax = projectileAttributes.ricochetAngleMax;
			newProjectileClass.projectileAttributes.damage = projectileAttributes.damage;
			newProjectileClass.projectileAttributes.lifespan = projectileAttributes.lifespan;
			newProjectileClass.projectileAttributes.isSticky = projectileAttributes.isSticky;
			//audioManager.PlayClipAtPoint(currentWeapon.soundFireNormal, currentWeapon.barrelPoint.position, 2f);

		}
	}

}
