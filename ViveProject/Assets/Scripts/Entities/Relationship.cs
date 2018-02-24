using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Relationship {

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
	
	[System.Serializable]
	public struct Emotions {
		// low typically = bad, high typically = good
		public float sadnessJoy;            // 0 = saddened, 1 = enjoyed
		public float angerLove;             // 0 = hated, 1 = loved
		public float disgustAdmiration;     // 0 = disgusted, 1 = admired
		public float surpriseRelaxed;       // 0 = surprised, 1 = relaxed
		public float fearTrust;             // 0 = feared, 0.5 = unsure of, 1 = trusted
		public float focus;					// 0 = unimportant, infinity = most important

		public Emotions(Emotions copiedEmotions) {
			sadnessJoy = copiedEmotions.sadnessJoy;
			angerLove = copiedEmotions.angerLove;
			disgustAdmiration = copiedEmotions.disgustAdmiration;
			surpriseRelaxed = copiedEmotions.surpriseRelaxed;
			fearTrust = copiedEmotions.fearTrust;
			focus = copiedEmotions.focus;
		}
	}

}
