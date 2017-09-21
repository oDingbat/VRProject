using UnityEngine;
using System.Collections.Generic;

public class GlowObjectCmd : MonoBehaviour {
	public Color GlowColor;
	public float LerpFactor = 10;
	public bool handNear = false;

	public Renderer[] Renderers {
		get;
		private set;
	}

	public Color CurrentColor {
		get { return _currentColor; }
	}

	private Color _currentColor;
	public Color _targetColor;

	void Start() {
		Renderers = GetComponentsInChildren<Renderer>();
		GlowController.RegisterObject(this);
	}

	/// <summary>
	/// Update color, disable self if we reach our target color.
	/// </summary>
	private void Update() {
		if (handNear == true) {
			_targetColor = GlowColor;
		} else {
			_targetColor = Color.black;
		}

		_currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * LerpFactor);

		handNear = false;
	}
}
