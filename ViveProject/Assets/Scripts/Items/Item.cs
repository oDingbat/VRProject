using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[RequireComponent(typeof(Rigidbody))]
public abstract class Item : MonoBehaviour {

	public Rigidbody			itemRigidbody;

	[Header("Information")]
	public string				itemName;
	public float				timeLastGrabbed;

	[Header("Pocketing Info")]
	public GrabNode				pocketGrabNode;
	public Pocket				pocketCurrent;
	public Pocket.PocketSize	pocketSize = Pocket.PocketSize.Small;
	public Transform			pocketingModel;
	public Pocket				pocketCandidate;
	public float				pocketCandidateTime;
	public Material				pocketingModelMaterialPrivate;
	public Quaternion			pocketingModelRotationOffset;

	AudioSource audioSourceHit;
	AudioSource audioSourceMove;

	public GrabNode grabNodes;

	void Start () {
		itemRigidbody = GetComponent<Rigidbody>();
		if (transform.Find("(PocketingModel)")) {
			pocketingModel = transform.Find("(PocketingModel)");

			if (pocketingModel) {
				foreach (Transform model in pocketingModel) {
					if (pocketingModelMaterialPrivate == null) {
						pocketingModelMaterialPrivate = new Material(model.GetComponent<Renderer>().material);
					}
					model.GetComponent<Renderer>().material = pocketingModelMaterialPrivate;
				}
			}
		} else {
			Debug.LogWarning("No pocketingModel found!");
		}
	}

	void OnCollisionEnter (Collision collision) {
		if (audioSourceHit) {
			if (collision.relativeVelocity.magnitude > 2) {
				audioSourceHit.volume = collision.relativeVelocity.magnitude * 0.1f;
				audioSourceHit.Play();
			}
		}
	}

	void FixedUpdate () {
		UpdatePocketingModel();
	}

	void Update () {
		OnItemUpdate();
	}

	void UpdatePocketingModel () {
		if (pocketingModel) {
			if (pocketCandidate) {
				// Set Pocketing Model Color
				if (Time.timeSinceLevelLoad - pocketCandidateTime < 0.1f) {
					pocketingModelMaterialPrivate.color = Color.Lerp(pocketingModelMaterialPrivate.color, new Color(pocketingModelMaterialPrivate.color.r, pocketingModelMaterialPrivate.color.g, pocketingModelMaterialPrivate.color.b, 0.2f), Mathf.Clamp01(Time.deltaTime * 25));
				} else {
					pocketingModelMaterialPrivate.color = Color.Lerp(pocketingModelMaterialPrivate.color, new Color(pocketingModelMaterialPrivate.color.r, pocketingModelMaterialPrivate.color.g, pocketingModelMaterialPrivate.color.b, 0.0f), Mathf.Clamp01(Time.deltaTime * 25));
				}

				// Set Pocketing Model Position & Rotation
				if (pocketingModelMaterialPrivate.color.a > 0) {
					if (pocketGrabNode) {
						pocketingModel.transform.position = pocketCandidate.transform.position - (pocketingModel.transform.rotation * pocketGrabNode.transform.localPosition);
						pocketingModel.transform.rotation = pocketCandidate.transform.rotation;
					} else {
						pocketingModel.transform.position = pocketCandidate.transform.position;
						pocketingModel.transform.rotation = pocketCandidate.transform.rotation;
					}
				}
			} else {
				pocketingModelMaterialPrivate.color = Color.Lerp(pocketingModelMaterialPrivate.color, new Color(pocketingModelMaterialPrivate.color.r, pocketingModelMaterialPrivate.color.g, pocketingModelMaterialPrivate.color.b, 0.0f), Mathf.Clamp01(Time.deltaTime * 25));
			}
		}
	}

	protected virtual void OnItemUpdate () { }

	public abstract string GetItemType();

}
