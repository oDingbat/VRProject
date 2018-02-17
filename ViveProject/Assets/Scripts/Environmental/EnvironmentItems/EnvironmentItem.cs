using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class EnvironmentItem : MonoBehaviour {

	public abstract void OnGrab(Player player, string handSide);

	public abstract void OnRelease();

}
