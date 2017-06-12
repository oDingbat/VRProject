using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {

	public ItemType itemType;
	public enum ItemType { Prop, Weapon, Consumable, Ammunition }



	public Weapon weapon;

}
