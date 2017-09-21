using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Item : MonoBehaviour {

	[Header("Information")]
	public string itemName;

	AudioSource audioSourceHit;
	AudioSource audioSourceMove;

	void OnCollisionEnter (Collision collision) {
		if (audioSourceHit) {
			if (collision.relativeVelocity.magnitude > 2) {
				audioSourceHit.volume = collision.relativeVelocity.magnitude * 0.1f;
				audioSourceHit.Play();
			}
		}
	}

	public abstract string GetItemType();

}
