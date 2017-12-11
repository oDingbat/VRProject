using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[RequireComponent (typeof(GlowObjectCmd)), RequireComponent(typeof(Rigidbody))]
public abstract class Item : MonoBehaviour {

	public Rigidbody itemRigidbody;

	[Header("Information")]
	public string itemName;
	public float timeLastGrabbed;

	[Header("Pocketing Info")]
	public GrabNode pocketGrabNode;
	public Pocket pocketCurrent;

	AudioSource audioSourceHit;
	AudioSource audioSourceMove;

	public GrabNode grabNodes;

	void Start () {
		itemRigidbody = GetComponent<Rigidbody>();
	}

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
