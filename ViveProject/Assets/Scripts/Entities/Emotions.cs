using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Emotions {
	// low typically = bad, high typically = good
	public float sadnessJoy;            // 0 = saddened, 1 = enjoyed
	public float angerLove;             // 0 = hated, 1 = loved
	public float disgustAdmiration;     // 0 = disgusted, 1 = admired
	public float surpriseRelaxed;       // 0 = surprised, 1 = relaxed
	public float fearTrust;             // 0 = feared, 0.5 = unsure of, 1 = trusted
	public float focus;                 // 0 = unimportant, infinity = most important

	public Emotions () {
		sadnessJoy = 0.5f;
		angerLove = 0.5f;
		disgustAdmiration = 0.5f;
		surpriseRelaxed = 0.5f;
		fearTrust = 0.5f;
		focus = 0.5f;
	}

	public Emotions (Emotions copiedEmotions) {
		sadnessJoy = copiedEmotions.sadnessJoy;
		angerLove = copiedEmotions.angerLove;
		disgustAdmiration = copiedEmotions.disgustAdmiration;
		surpriseRelaxed = copiedEmotions.surpriseRelaxed;
		fearTrust = copiedEmotions.fearTrust;
		focus = copiedEmotions.focus;
	}
}
