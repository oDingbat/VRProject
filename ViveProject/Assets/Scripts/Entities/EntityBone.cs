using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Collider))]
public class EntityBone : MonoBehaviour {

	public Entity entity;                   // The entity this entity bone is a child of
	public Rigidbody rigidbody;
	[Range (0, 5)]
	public float damageMultiplier = 1;      // The value that a piece of damage is multiplied by and then dealt the the parent entity (default = 1, critital > 1, armor < 1, etc)
	public float stunMultiplier = 1;

	float meleeDamageCooldown = 0.2f;
	float impactDamageVelocityMinimum = 3f;
	float impactDamageConstant = 4f;

	void Start () {
		FindEntity();
		rigidbody = GetComponent<Rigidbody>();
	}

	void FindEntity () {
		// Find this bone's entity
		if (entity == null) {
			Transform currentLayer = transform;
			while (entity == null && currentLayer != null) {
				Entity currentLayerEntity = currentLayer.GetComponent<Entity>();
				if (currentLayerEntity != null) {
					entity = currentLayerEntity;
				} else {
					if (currentLayer.parent != null) {
						currentLayer = currentLayer.parent;
					} else {
						break;
					}
				}
			}
		}
	}

	void OnCollisionEnter(Collision collision) {
		if (entity.timeLastMeleeDamage + meleeDamageCooldown <= Time.time) {
			EntityBone collisionEntityBone = collision.gameObject.GetComponent<EntityBone>();
			if (collisionEntityBone == null || (collisionEntityBone == true && collisionEntityBone.entity != entity)) {
				if (collision.relativeVelocity.magnitude > impactDamageVelocityMinimum) {       // Was the collision's force greater than the minimum to deal damage?
					entity.timeLastMeleeDamage = Time.time;
					Rigidbody rigidbodyCollision = collision.rigidbody;
					if (rigidbodyCollision != null) {
						float impactDamage = collision.relativeVelocity.magnitude * (rigidbodyCollision.mass / rigidbody.mass) * impactDamageConstant;
						entity.TakeDamage((int)Mathf.Round(impactDamage), (impactDamage / 50) * stunMultiplier, collision.contacts[0].point, -collision.contacts[0].normal);
					} else {
						float impactDamage = collision.relativeVelocity.magnitude * impactDamageConstant;
						entity.TakeDamage((int)Mathf.Round(impactDamage), (impactDamage / 50) * stunMultiplier, collision.contacts[0].point, -collision.contacts[0].normal);
					}
				}
			}
		}
	}

}
