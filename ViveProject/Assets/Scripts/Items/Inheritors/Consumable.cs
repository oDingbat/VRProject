using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Consumable : Item {

	public override string GetItemType() {
		return "Consumable";
	}

}
