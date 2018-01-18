using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightSub : InteractionSubscriber {

	[Space(10)][Header("Pocketing Info")]
	public Light lightObject;

	void Start () {
		BaseStart();
		lightObject = GetComponent<Light>();
	}

	protected override void UpdateInteraction (bool interactionState) {
		lightObject.enabled = interactionState;
	}

}
