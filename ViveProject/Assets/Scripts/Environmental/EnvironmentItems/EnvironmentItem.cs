using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[RequireComponent (typeof(Rigidbody))]
public abstract class EnvironmentItem : MonoBehaviour {

	[Space(10)][Header("Inherited Members")]
	public Rigidbody rigidbody;
	public Player playerGrabbing;
	public string handGrabbingSide;
	public bool isGrabbed;

	public abstract void OnGrab(Player player, string handSide);

	public abstract void OnRelease();

}
