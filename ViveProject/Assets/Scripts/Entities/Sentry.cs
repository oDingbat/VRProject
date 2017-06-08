using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Entity)), RequireComponent(typeof(Entity))]
public class Sentry : MonoBehaviour {

	public Entity entity;

	public LayerMask visionMask;

	public SentryMode sentryMode;
	public enum SentryMode { Sweep, Hold }

	public Transform target;
	public Vector3 targetLastKnownPosition;

	public Transform head;
	public Rigidbody headRigidbody;
	public Transform barrel;
	public Transform laserSight;
	public Transform laser;

	public Weapon	weapon;

	public float	visionCurrent = 0;
	float			visionMax  = 4f;
	float			visionRequired = 1.5f;
	
	void Start () {
		entity = GetComponent<Entity>();

		target = GameObject.Find("Player Head").transform;

		head = transform.Find("Head");
		headRigidbody = head.GetComponent<Rigidbody>();
		barrel = head.Find("Barrel");
		laserSight = head.Find("LaserSight");
		laser = transform.Find("Laser");
	}

	void Update () {
		StabilizeHead();
		SearchForTarget();
	}

	void StabilizeHead () {
		headRigidbody.velocity = Vector3.Lerp(headRigidbody.velocity, (transform.position + new Vector3(0, 1, 0) - head.transform.position) / Time.deltaTime, 15 * Time.deltaTime);
		headRigidbody.transform.rotation = Quaternion.LookRotation((targetLastKnownPosition - head.position).normalized);
	}

	void SearchForTarget () {
		RaycastHit hit;
		if (Physics.Raycast(head.position, target.position - head.position, out hit, Mathf.Infinity, visionMask)) {
			if (hit.transform.gameObject.tag == ("Player")) {
				visionCurrent = Mathf.Clamp(visionCurrent + Time.deltaTime, 0, visionMax);
				targetLastKnownPosition = target.position;
			} else {
				visionCurrent = Mathf.Clamp(visionCurrent - Time.deltaTime, 0, visionMax);
			}
		} else {
			visionCurrent = Mathf.Clamp(visionCurrent - Time.deltaTime, 0, visionMax);
		}

		if (visionCurrent > visionRequired) {
			AttemptWeaponFire();
		}

	}

	void AttemptWeaponFire () {
		if (weapon.timeLastFired + (1 / weapon.firerate) <= Time.timeSinceLevelLoad) {
			weapon.timeLastFired = Time.timeSinceLevelLoad;
			FireWeapon();
		}
	}

	void FireWeapon () {
		GameObject newProjectile = (GameObject)Instantiate(weapon.projectile, barrel.position + barrel.forward * 0.2f, head.rotation * Quaternion.Euler(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)));
		newProjectile.GetComponent<Projectile>().velocity = weapon.projectileVelocity;
	}

}
