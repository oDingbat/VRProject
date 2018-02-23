using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Projectile : MonoBehaviour {

	public LayerMask	collisionMask;

	public float				initialVelocityMagnitude;
	public Vector3				velocity;
	public float				deceleration;
	public DecelerationType		decelerationType;
	public enum					DecelerationType { Normal, Smooth }
	public float				gravity;
	public bool					sticky;
	public int					baseDamage;
	public float				lifespan = 5;

	public Collider				projectileCollider;

	public int			ricochetCount;
	public float		ricochetAngleMax;
	bool				broken;
	bool				firstFrameLoaded;

	public GameObject prefabProjectileShattered;
	public GameObject prefabDamageText;

	void Start () {
		initialVelocityMagnitude = velocity.magnitude;
		StartCoroutine(AutoDestroy());
	}

	IEnumerator AutoDestroy () {
		yield return new WaitForSeconds(lifespan);
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
		if (firstFrameLoaded == true) {
			if (broken == false) {
				UpdateProjectileMovement();
			}
		} else {
			transform.position -= transform.forward * 0.01f;
			firstFrameLoaded = true;
		}
	}

	public void UpdateProjectileMovement () {
		RaycastHit hit;
		if (Physics.Raycast(transform.position, velocity.normalized, out hit, velocity.magnitude * Time.deltaTime, collisionMask)) {
			if (hit.transform.gameObject.tag == "Player") {
				//GameObject.Find("Player Body").GetComponent<Entity>().TakeDamage(baseDamage);
				StartCoroutine(BreakProjectile(hit.point));
			} else {
				DamageParentingEntity(hit);

				Vector3 normalPerpendicular = velocity.normalized - hit.normal * Vector3.Dot(velocity.normalized, hit.normal);

				if (hit.transform.GetComponent<Rigidbody>()) {
					hit.transform.GetComponent<Rigidbody>().AddForceAtPosition(velocity.normalized * Mathf.Sqrt(velocity.magnitude) * Mathf.Sqrt(Vector3.Angle(normalPerpendicular, velocity.normalized) * 2500f), hit.point);
				}

				if (ricochetCount > 0 && Vector3.Angle(normalPerpendicular, velocity.normalized) <= ricochetAngleMax) {
					ricochetCount--;
					transform.position = hit.point + (hit.normal * 0.001f);
					transform.rotation = Quaternion.LookRotation(Vector3.Reflect(velocity.normalized, hit.normal));
					velocity = Vector3.Reflect(velocity, hit.normal);
				} else {
					StartCoroutine(BreakProjectile(hit.point));
				}

				if (sticky == true && (hit.transform.gameObject.layer == LayerMask.NameToLayer("Environment") || hit.transform.GetComponent<Rigidbody>())) {

					// Enable the projectile's collider																																	// YHAFN
					projectileCollider.enabled = true;

					// Change prefab into a grabNode for the hit item
					if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
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


						Transform[] transformsInProjectile = transform.GetComponentsInChildren<Transform>();
						foreach (Transform tCurrent in transformsInProjectile) {
							tCurrent.gameObject.layer = LayerMask.NameToLayer("Item");
						}

						gameObject.AddComponent<Misc>();
						Destroy(this);
					} else if (hit.transform.gameObject.layer == LayerMask.NameToLayer("EnvironmentItem")) {
						transform.position = hit.point;
						transform.parent = hit.transform;

						Transform[] transformsInProjectile = transform.GetComponentsInChildren<Transform>();
						foreach (Transform tCurrent in transformsInProjectile) {
							tCurrent.gameObject.layer = LayerMask.NameToLayer("EnvironmentItem");
						}
					} else {
						transform.parent = hit.transform;
					}
				}
			}
		} else {
			transform.position += velocity * Time.deltaTime;
			velocity += new Vector3(0, gravity * Time.deltaTime, 0);
			if (decelerationType == DecelerationType.Normal) {
				velocity = velocity.normalized * Mathf.Clamp(velocity.magnitude - (deceleration * Time.deltaTime), 0, Mathf.Infinity);
			} else {
				velocity = Vector3.Lerp(velocity, Vector3.zero, deceleration * Time.deltaTime);
			}
			if (velocity.magnitude <= 5 && initialVelocityMagnitude > 50) {
				StartCoroutine(BreakProjectile(transform.position));
			}
		}
	}

	void DamageParentingEntity (RaycastHit hit) {
		Entity hitEntity = hit.transform.GetComponent<Entity>();
		if (hitEntity != null) {
			hitEntity.TakeDamage(baseDamage, hit.point, hit.normal);
			StartCoroutine(BreakProjectile(transform.position));
		} else {
			EntityBone hitEntityBone = hit.transform.GetComponent<EntityBone>();
			if (hitEntityBone != null) {
				int damageCalculated = (int)Mathf.Round((float)baseDamage * hitEntityBone.damageMultiplier);
				hitEntityBone.entity.TakeDamage(damageCalculated, hit.point, hit.normal);
				StartCoroutine(BreakProjectile(transform.position));
			}
		}
	}

	IEnumerator BreakProjectile(Vector3 finalPos) {
		if (broken == false) {
			transform.position = finalPos;
			broken = true;
			//yield return new WaitForSeconds(GetComponent<TrailRenderer>().time * 0.9f);
			if (sticky == false) {
				Destroy(gameObject);
			}
		} else {
			yield return null;
		}
	}

}
