using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleController : MonoBehaviour {

	public LayerMask playerMask;

	private WheelCollider[] wheelsColliders;
	public Player player;

	public float maxAngle = 35;
	public float maxTorque = 100;

	public Transform centerOfMass;

	public float inputGas;              // The current amount of input being applied to the gas petal (between 0 & 1)
	public float inputBrake;            // The current amount of input being applied to the brake petal (between 0 & 1)
	public float inputSteering;        // The current amount of inpit being applied to the steering wheel (between -1 & 1)
	public int gearCurrent;            // The current gear the car is in (0 = reverse gear, 1-n = forward gears)
	public int gearMax = 5;            // The maximum amount of gears the car has (typically 5)
	public float[] gearMultiplier = new float[] { -0.75f, 1.00f, 2.00f, 2.75f, 3.25f, 4.125f };
	public float[] gearSpeedRange = new float[] { 12.5f, 25.0f, 35.0f, 47.5f, 55.0f, 62.5f };
	public AnimationCurve[] gearAccelerationCurves;

	public Transform vehicleClimbable;

	public Collider[] movementFields;       // Fields that move players
	public List<PlayerAndOffset> playersInMovementFields = new List<PlayerAndOffset>();

	Vector3 positionLastFrame;
	Quaternion rotationLastFrame;

	[System.Serializable]
	public class PlayerAndOffset {
		public Player player;
		public Vector3 offset;

		public PlayerAndOffset (Player _player, Vector3 _offset) {
			player = _player;
			offset = _offset;
		}
	}

	public void Start() {
		player = GameObject.Find("Player Body").GetComponent<Player>();
		wheelsColliders = GetComponentsInChildren<WheelCollider>();

		for (int i = 0; i < wheelsColliders.Length; ++i) {
			var wheel = wheelsColliders[i];
		}

		vehicleClimbable = transform.Find("VehicleClimbable");
		vehicleClimbable.transform.parent = transform.parent;


		GetComponent<Rigidbody>().centerOfMass = centerOfMass.transform.position - transform.position;

		if (transform.Find("(MovementFields)")) {
			movementFields = transform.Find("(MovementFields)").GetComponentsInChildren<Collider>();
		}
	}

	public void Update() {

		MoveVehicleClimbable();

		//GetStandardInput();
		GetViveInput();
		UpdateTorqueAndAngle();

		MovePlayers();
		FindPlayers();

		positionLastFrame = transform.position;
	}

	void MoveVehicleClimbable() {
		vehicleClimbable.transform.position = transform.position;
		vehicleClimbable.transform.rotation = transform.rotation;
	}

	void GetStandardInput() {
		// Input for gas
		inputGas = Mathf.Lerp(inputGas, (Input.GetKey(KeyCode.W) ? 1 : 0), 10 * Time.deltaTime);

		// Input for brake
		inputBrake = Mathf.Lerp(inputBrake, (Input.GetKey(KeyCode.Space) ? 1 : 0), 10 * Time.deltaTime);

		// Input for steering
		inputSteering = Mathf.Lerp(inputSteering, Input.GetAxis("Horizontal"), 20 * Time.deltaTime);

		// Input for gears
		if (Input.GetKeyDown(KeyCode.Alpha0)) {
			gearCurrent = Mathf.Min(gearMax, 0);
		}

		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			gearCurrent = Mathf.Min(gearMax, 1);
		}

		if (Input.GetKeyDown(KeyCode.Alpha2)) {
			gearCurrent = Mathf.Min(gearMax, 2);
		}

		if (Input.GetKeyDown(KeyCode.Alpha3)) {
			gearCurrent = Mathf.Min(gearMax, 3);
		}

		if (Input.GetKeyDown(KeyCode.Alpha4)) {
			gearCurrent = Mathf.Min(gearMax, 4);
		}

		if (Input.GetKeyDown(KeyCode.Alpha5)) {
			gearCurrent = Mathf.Min(gearMax, 5);
		}
	}

	void GetViveInput() {
		if (player.handInfoLeft.controllerDevice != null) {
			// Input for gas
			inputGas = Mathf.Lerp(inputGas, player.handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y, 10 * Time.deltaTime);

			// Input for brake
			inputBrake = Mathf.Lerp(inputBrake, player.handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x, 10 * Time.deltaTime);

			// Input for steering
			inputSteering = Mathf.Lerp(inputSteering, player.handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x, 10 * Time.deltaTime);
		}
	}

	public WheelFrictionCurve ModifiedFrictionStiffness(WheelFrictionCurve curve, float value) {
		WheelFrictionCurve newCurve = new WheelFrictionCurve();
		newCurve = curve;
		newCurve.stiffness = value;
		return newCurve;
	}

	void UpdateTorqueAndAngle () {
		float angle = maxAngle * inputSteering;
		float torque = maxTorque * inputGas;

		if (torque < 0) {
			torque *= 0.5f;
		}

		foreach (WheelCollider wheelCollider in wheelsColliders) {
			// a simple car where front wheels steer while rear ones drive
			if (wheelCollider.transform.localPosition.z > 0) {
				wheelCollider.steerAngle = Mathf.Lerp(wheelCollider.steerAngle, angle, 5 * Time.deltaTime);
				wheelCollider.motorTorque = Mathf.Lerp(wheelCollider.motorTorque, torque, 7.5f * Time.deltaTime);
			}

			if (wheelCollider.transform.localPosition.z < 0) {
				wheelCollider.motorTorque = Mathf.Lerp(wheelCollider.motorTorque, torque, 7.5f * Time.deltaTime);
			}

			wheelCollider.brakeTorque = (wheelCollider.motorTorque + 1500) * (inputBrake > 0.1f ? inputBrake : 0);
			wheelCollider.sidewaysFriction = ModifiedFrictionStiffness(wheelCollider.sidewaysFriction, 3 - (2.75f * (inputBrake > 0.1f ? inputBrake : 0)));
		}
	}

	void MovePlayers () {
		Vector3 deltaMovementThisFrame = transform.position - positionLastFrame;

		foreach (PlayerAndOffset PO in playersInMovementFields) {
			Vector3 playerOffsetDeltaMovement = (transform.position + (transform.rotation * PO.offset)) - (positionLastFrame + (rotationLastFrame * PO.offset));
			PO.player.MovePlayerWithoutCollision(playerOffsetDeltaMovement);
		}
	}

	void FindPlayers () {
		playersInMovementFields.Clear();

		foreach (Collider field in movementFields) {
			if (field is BoxCollider) {
				BoxCollider fieldBoxCollider = field as BoxCollider;
				Collider[] hitColliders = Physics.OverlapBox(fieldBoxCollider.transform.position, fieldBoxCollider.size / 2, fieldBoxCollider.transform.rotation, playerMask);
				foreach (Collider hitCollider in hitColliders) {
					if (hitCollider.GetComponent<Player>()) {
						PlayerAndOffset newPlayerAndOffset = new PlayerAndOffset(hitCollider.GetComponent<Player>(), Quaternion.Inverse(transform.rotation) * (transform.position - hitCollider.transform.position));
						if (!playersInMovementFields.Contains(newPlayerAndOffset)) {
							playersInMovementFields.Add(newPlayerAndOffset);
						}
					}
				}
			}
		}
	}

}
