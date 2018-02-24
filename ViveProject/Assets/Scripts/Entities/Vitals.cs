
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Vitals {

	[Space(10)] [Header("Health")]
	public int healthCurrent;
	public int healthMax;

	[Space(10)] [Header("Stunning")]
	public float stunCurrent;
	public float stunMax;
	public float stunThreshold;

	[Space(10)] [Header("Booleans")]
	public bool isDead = false;
	public bool isConscious = false;
	
}
