using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour {

	public Player player;
	Text text;

	void Start () {
		text = GetComponent<Text>();
	}

	void FixedUpdate () {
		//text.text = "IVP : " + player.itemGrabInfoRight.itemVelocityPercentage;
	}
}
