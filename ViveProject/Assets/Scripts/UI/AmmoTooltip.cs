using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AmmoTooltip : MonoBehaviour {

	public Text text;
	public Weapon weapon;

	Transform playerHead;

	float sizeNormal = 1f;
	float sizeLarge = 1.75f;

	void Start () {
		text = GetComponent<Text>();
		weapon = transform.parent.parent.GetComponent<Weapon>();

		playerHead = GameObject.Find("Camera (both) (eye)").transform;

		if (weapon) {
			weapon.eventAdjustAmmo += AdjustText;
			text.text = weapon.ammoCurrent.ToString();
		}
	}

	void AdjustText () {
		text.text = weapon.ammoCurrent.ToString();
		transform.localScale = new Vector3(sizeLarge, sizeLarge, sizeLarge);
	}

	void Update () {
		float distanceFactor = Mathf.Clamp(-Vector3.Distance(transform.position, playerHead.position) + 3, 0, 1);
		float angleFactor = Mathf.Clamp(75 - (Vector3.Angle(transform.forward, playerHead.transform.forward) + 10), 0, 75) / 75;
		float factorTimeLastFired = Mathf.Clamp(((weapon.timeLastTriggered * 4) - (Time.timeSinceLevelLoad * 4)) + 4f, 0, 1);
		float factorTimeLastGrabbed = Mathf.Clamp01((weapon.timeLastGrabbed * 4) - (Time.timeSinceLevelLoad * 4) + 5f);

		text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Clamp01(distanceFactor * angleFactor * (factorTimeLastFired + factorTimeLastGrabbed)));
		transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(sizeNormal, sizeNormal, sizeNormal), 10 * Time.deltaTime);
	}



}
