using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityManager : MonoBehaviour {

	public List<Entity> allEntities;

	void Start() {
		allEntities = ((Entity[])FindObjectsOfType(typeof(Entity))).ToList();
	}
}
