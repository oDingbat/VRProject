using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {

	public ItemType itemType;
	public enum ItemType { Prop, Weapon, Consumable, Ammunition }

	AudioSource audioSourceHit;
	AudioSource audioSourceMove;

	public Weapon weapon;

	void Update () {
		if (weapon != null) {
			if (weapon.triggerHeld == false) {
				if (weapon.chargingEnabled == true) {
					weapon.chargeCurrent = Mathf.Clamp01(weapon.chargeCurrent - (weapon.chargeDecrement * Time.deltaTime));
				}
			}
		}
		audioSourceMove.volume = Mathf.Clamp01(GetComponent<Rigidbody>().velocity.magnitude * 0.1f);
	}

	void OnCollisionEnter (Collision collision) {
		if (audioSourceHit) {
			if (collision.relativeVelocity.magnitude > 2) {
				audioSourceHit.volume = collision.relativeVelocity.magnitude * 0.1f;
				audioSourceHit.Play();
			}
		}
	}

}
