using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

	public LayerMask	collisionMask;

	public float				velocity;
	public float				deceleration;
	public DecelerationType		decelerationType;
	public enum					DecelerationType { Normal, Smooth }

	float				lifespan = 5;

	public int			ricochetCount;
	public float		ricochetAngleMax;
	bool				broken;
	bool				firstFrameLoaded;

	void Start () {
		StartCoroutine(AutoDestroy());
	}

	IEnumerator AutoDestroy () {
		yield return new WaitForSeconds(lifespan);
		Destroy(gameObject);
	}

	void Update () {
		if (firstFrameLoaded == true) {
			if (broken == false) {
				AttemptMove();
			}
		} else {
			transform.position -= transform.forward * 0.01f;
			firstFrameLoaded = true;
		}
	}

	void AttemptMove () {
		RaycastHit hit;
		if (Physics.Raycast(transform.position, transform.forward, out hit, velocity * Time.deltaTime, collisionMask)) {
			if (hit.transform) {
				if (hit.transform.gameObject.tag == "Player") {
					GameObject.Find("Player Body").GetComponent<Player>().TakeDamage(25);
					StartCoroutine(BreakProjectile(hit.point));
				} else {
					Vector3 normalPerpendicular = transform.forward - hit.normal * Vector3.Dot(transform.forward, hit.normal);
					if (ricochetCount > 0 && Vector3.Angle(normalPerpendicular, transform.forward) <= ricochetAngleMax) {
						ricochetCount--;
						if (hit.transform.GetComponent<Rigidbody>()) {
							hit.transform.GetComponent<Rigidbody>().AddForce(transform.forward * 100 * velocity);
						}
						transform.position = hit.point + (hit.normal * 0.001f);
						transform.rotation = Quaternion.LookRotation(Vector3.Reflect(transform.forward, hit.normal));
					} else {
						StartCoroutine(BreakProjectile(hit.point));
					}
				}
			}
		} else {
			transform.position += transform.forward * velocity * Time.deltaTime;
			if (decelerationType == DecelerationType.Normal) {
				velocity = Mathf.Clamp(velocity - (deceleration * Time.deltaTime), 0, Mathf.Infinity);
			} else {
				velocity = Mathf.Clamp(Mathf.Lerp(velocity, 0, deceleration * Time.deltaTime), 0, Mathf.Infinity);
			}
			if (velocity <= 5f) {
				StartCoroutine(BreakProjectile(transform.position));
			}
		}
	}

	IEnumerator BreakProjectile(Vector3 finalPos) {
		if (broken == false) {
			transform.position = finalPos;
			broken = true;
			yield return new WaitForSeconds(GetComponent<TrailRenderer>().time * 0.9f);
			Destroy(gameObject);
		} else {
			yield return null;
		}
	}

}
