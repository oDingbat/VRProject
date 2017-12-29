using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour {

	Text text;

	void Start () {
		text = GetComponent<Text>();
	}

	void FixedUpdate () {
		text.text = ((int)Mathf.Round(1.0f / Time.smoothDeltaTime)).ToString();
	}
}
