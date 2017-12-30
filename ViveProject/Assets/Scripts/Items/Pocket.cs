using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pocket : MonoBehaviour {

	public Item pocketedItem;
	public float timePocketed;
	public PocketSize pocketSize = PocketSize.Small;
	public enum PocketSize { None, Small, Medium, Large }

	public Pocket linkedPocket;
	public float angleRange;

	void Update() {
		UpdateItemMovement();
	}

	void UpdateItemMovement() {
		if (pocketedItem) {
			if (pocketedItem.GetComponent<ItemConstraints>() && pocketedItem.GetComponent<ItemConstraints>().colliderContainer) {
				ReleaseItem();
			}
			float lerpCalculation = Mathf.Clamp01((Time.timeSinceLevelLoad - timePocketed) * 100 * Time.deltaTime);
			if (pocketedItem.pocketGrabNode != null) {
				pocketedItem.transform.position = Vector3.Lerp(pocketedItem.transform.position, transform.position - (pocketedItem.transform.rotation * pocketedItem.pocketGrabNode.transform.localPosition), lerpCalculation);
				pocketedItem.transform.rotation = Quaternion.Lerp(pocketedItem.transform.rotation, transform.rotation, lerpCalculation);
			} else {
				pocketedItem.transform.position = Vector3.Lerp(pocketedItem.transform.position, transform.position, lerpCalculation);
				pocketedItem.transform.rotation = Quaternion.Lerp(pocketedItem.transform.rotation, transform.rotation, lerpCalculation);
			}
		}
	}

	public void ReleaseItem() {
		pocketedItem.pocketCurrent = null;
		pocketedItem.GetComponent<Rigidbody>().useGravity = true;
		pocketedItem = null;
	}

	public void PocketItem(Item newItem) {
		if (linkedPocket == null || (linkedPocket != null && linkedPocket.pocketedItem == null)) {
			timePocketed = Time.timeSinceLevelLoad;
			pocketedItem = newItem;
			newItem.GetComponent<Rigidbody>().useGravity = false;
			newItem.pocketCurrent = this;
		}
	}

	public bool GetAvailability () {
		if (pocketedItem) {
			return false;
		} else {
			if (linkedPocket) {
				if (linkedPocket.pocketedItem) {
					return false;
				} else {
					return true;
				}
			} else {
				return true;
			}
		}
	}


	void OnDrawGizmosSelected() {
		if (linkedPocket) {
			if (linkedPocket.linkedPocket != this) {
				linkedPocket.linkedPocket = this;
			}
			Debug.DrawLine(transform.position, linkedPocket.transform.position, Color.magenta);
		}
	}

}
