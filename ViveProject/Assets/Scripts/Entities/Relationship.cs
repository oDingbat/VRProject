using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Relationship {
	// Keeps track of relationships between an entity and any type of monobehaviour (ie: another Entity, an Item, an EnvironmentItem, etc)

	public string objectName;
	public MonoBehaviour monobehaviour;
	public Vector3 lastKnownPosition;
	public float timeLastSeen;
	public bool lineOfSight;

	public Emotions emotions;

	public Relationship(string _objectName, MonoBehaviour _monobehaviour, Vector3 _lastKnownPosition, float _timeLastSeen, Emotions _emotions) {
		objectName = _objectName;
		monobehaviour = _monobehaviour;
		lastKnownPosition = _lastKnownPosition;
		timeLastSeen = _timeLastSeen;
		emotions = new Emotions(_emotions);
	}
	
	

}
