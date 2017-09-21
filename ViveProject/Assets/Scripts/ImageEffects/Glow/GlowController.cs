using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>
/// Creates and maintains a command buffer to set up the textures used in the glowing object image effect.
/// </summary>
public class GlowController : MonoBehaviour {
	private static GlowController _instance;

	private CommandBuffer _commandBuffer;

	private List<GlowObjectCmd> _glowableObjects = new List<GlowObjectCmd>();
	private Material _glowMat;
	private Material _blurMaterial;
	private Vector2 _blurTexelSize;

	private int _prePassRenderTexID;
	private int _blurPassRenderTexID;
	private int _tempRenderTexID;
	private int _blurSizeID;
	private int _glowColorID;

	/// <summary>
	/// On Awake, we cache various values and setup our command buffer to be called Before Image Effects.
	/// </summary>
	private void Awake() {
		_instance = this;

		_glowMat = new Material(Shader.Find("Hidden/GlowCmdShader"));
		_blurMaterial = new Material(Shader.Find("Hidden/Blur"));

		_prePassRenderTexID = Shader.PropertyToID("_GlowPrePassTex");
		_blurPassRenderTexID = Shader.PropertyToID("_GlowBlurredTex");
		_tempRenderTexID = Shader.PropertyToID("_TempTex0");
		_blurSizeID = Shader.PropertyToID("_BlurSize");
		_glowColorID = Shader.PropertyToID("_GlowColor");

		_commandBuffer = new CommandBuffer();
		_commandBuffer.name = "Glowing Objects Buffer"; // This name is visible in the Frame Debugger, so make it a descriptive!
		GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
	}

	/// <summary>
	/// TODO: Add a degister method.
	/// </summary>
	public static void RegisterObject(GlowObjectCmd glowObj) {
		if (_instance != null) {
			_instance._glowableObjects.Add(glowObj);
		}
	}

	/// <summary>
	/// Adds all the commands, in order, we want our command buffer to execute.
	/// Similar to calling sequential rendering methods insde of OnRenderImage().
	/// </summary>
	private void RebuildCommandBuffer() {
		_commandBuffer.Clear();

		_commandBuffer.GetTemporaryRT(_prePassRenderTexID, GetComponent<Camera>().pixelWidth, GetComponent<Camera>().pixelHeight, 0, FilterMode.Trilinear, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, QualitySettings.antiAliasing);
		_commandBuffer.SetRenderTarget(_prePassRenderTexID);
		_commandBuffer.ClearRenderTarget(true, true, Color.clear);

		for (int i = 0; i < _glowableObjects.Count; i++) {
			if (_glowableObjects[i]._targetColor != Color.black) {		// Exclude objects that are full black
				_commandBuffer.SetGlobalColor(_glowColorID, _glowableObjects[i].CurrentColor);

				for (int j = 0; j < _glowableObjects[i].Renderers.Length; j++) {
					_commandBuffer.DrawRenderer(_glowableObjects[i].Renderers[j], _glowMat);
				}
			}
		}

		_commandBuffer.GetTemporaryRT(_blurPassRenderTexID, Screen.width >> 1, Screen.height >> 1, 0, FilterMode.Trilinear);
		_commandBuffer.GetTemporaryRT(_tempRenderTexID, Screen.width >> 1, Screen.height >> 1, 0, FilterMode.Trilinear);
		_commandBuffer.Blit(_prePassRenderTexID, _blurPassRenderTexID);

		_blurTexelSize = new Vector2(0.25f / (Screen.width >> 1), 0.25f / (Screen.height >> 1));
		_commandBuffer.SetGlobalVector(_blurSizeID, _blurTexelSize);

		for (int i = 0; i < 4; i++) {
			_commandBuffer.Blit(_blurPassRenderTexID, _tempRenderTexID, _blurMaterial, 0);
			_commandBuffer.Blit(_tempRenderTexID, _blurPassRenderTexID, _blurMaterial, 1);
		}
	}

	private void Update() {
		RebuildCommandBuffer();
	}
}
