
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Vitals {

	public int healthCurrent;
	public int healthMax;

	public void TakeDamage (int damage) {
		healthCurrent = Mathf.Clamp(healthCurrent - damage, 0, healthMax);
	}

	public void Heal (int healAmount) {
		healthCurrent = Mathf.Clamp(healthCurrent + healAmount, 0, healthMax);
	}

}
