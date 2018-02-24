using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemManager : MonoBehaviour {

	public List<Item> allItems;

	void Start () {
		allItems = ((Item[])FindObjectsOfType(typeof(Item))).ToList();
	}

}
