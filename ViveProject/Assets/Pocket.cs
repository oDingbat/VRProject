using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pocket : MonoBehaviour {

	public Item pocketedItem;
	public float timePocketed;

	void Update () {
		if (pocketedItem) {
			float lerpCalculation = Mathf.Clamp01((Time.timeSinceLevelLoad - timePocketed) * 100 * Time.deltaTime);
			if (pocketedItem.pocketGrabNode != null) {
				//pocketedItem.transform.position = transform.position - (pocketedItem.transform.rotation * pocketedItem.pocketGrabNode.transform.localPosition);
				//pocketedItem.transform.rotation = transform.rotation;

				pocketedItem.transform.position = Vector3.Lerp(pocketedItem.transform.position, transform.position - (pocketedItem.transform.rotation * pocketedItem.pocketGrabNode.transform.localPosition), lerpCalculation);
				pocketedItem.transform.rotation = Quaternion.Lerp(pocketedItem.transform.rotation, transform.rotation, lerpCalculation);
			} else {
				pocketedItem.transform.position = Vector3.Lerp(pocketedItem.transform.position, transform.position, lerpCalculation);
				pocketedItem.transform.rotation = Quaternion.Lerp(pocketedItem.transform.rotation, transform.rotation, lerpCalculation);
			}
		}
	}

}
