using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(HingeJoint), typeof(Rigidbody))]
public class HingeItem : EnvironmentItem {

	[Space(10)][Header("Objects")]
	public HingeJoint	hingeJoint;
	public Rigidbody	rigidbody;
	
	[Space(10)][Header("Grabbing Information")]
	public Player		playerGrabbing;
	public string		handGrabbingSide;
	public bool			isGrabbed;

	[Space(10)][Header("Movement Settings")]
	public float[]		snapAngles;
	[Range(1f, 180f)]
	public float		snapRange;
	[Range(1f, 180f)]
	public float		springMaxDistance = 180;                    // The max distance required for a spring to be re enabled (high angle = usually always on (levers), low angle = usually not on until close to target angle (doors)
	float				springDefault;
	[Range(0f, 1f)]
	public float		springStrengthGrabbed = 0.1f;
	[Range(0f, 1f)]
	public float		springStrengthOutsideMaxDistance = 0.0f;
	public float		hingeMovementNormalizeDistance = 1f;

	[Space(10)][Header("Haptic Feedback Settings")]
	public float		angleLastHaptic;
	[Range(0f, 1f)]
	public float		hapticFeedbackStrength;

	void Start() {
		rigidbody = GetComponent<Rigidbody>();

		if (GetComponent<HingeJoint>() == true) {
			hingeJoint = GetComponent<HingeJoint>();
			springDefault = hingeJoint.spring.spring;
		}

		if (snapAngles.Length > 0) {
			// Set target angle to first snap angle
			JointSpring newJointSpring = new JointSpring();
			newJointSpring.damper = hingeJoint.spring.damper;
			newJointSpring.spring = hingeJoint.spring.spring;
			newJointSpring.targetPosition = snapAngles[1];
			hingeJoint.spring = newJointSpring;
		}
	}

	void Update () {
		SnapTargetAngle();
		UpdateHapticFeedback();
	}

	void UpdateHapticFeedback () {
		if (Mathf.Abs(angleLastHaptic - hingeJoint.angle) >= 5f) {
			angleLastHaptic = hingeJoint.angle;
			StartCoroutine(playerGrabbing.TriggerHapticFeedback((handGrabbingSide == "Left" ? playerGrabbing.handInfoLeft : playerGrabbing.handInfoRight).controllerDevice, (ushort)(800f * hapticFeedbackStrength), 0f));
		}
	}

	void SnapTargetAngle () {
		if (rigidbody.velocity.magnitude > 0.01f) {
			// Find closest snap angle
			float closestSnapAngle = hingeJoint.spring.targetPosition;
			float closestDistance = Mathf.Abs(hingeJoint.angle - hingeJoint.spring.targetPosition);
			foreach (float snapAngle in snapAngles) {
				float thisDistance = Mathf.Abs(hingeJoint.angle - snapAngle);
				if (thisDistance <= snapRange && thisDistance < closestDistance) {
					closestDistance = thisDistance;
					closestSnapAngle = snapAngle;
				}
			}
			
			if (closestSnapAngle != hingeJoint.spring.targetPosition) {
				if (playerGrabbing == true) {
					StartCoroutine(playerGrabbing.TriggerHapticFeedback((handGrabbingSide == "Left" ? playerGrabbing.handInfoLeft : playerGrabbing.handInfoRight).controllerDevice, (ushort)(6000f * hapticFeedbackStrength), 0.05f));
				}
				// Set target angle to closest snap angle
				SetSpringTargetAngle(closestSnapAngle);
			}
			
			// Check if current angle is close enough to targetAngle to enable spring
			if (Mathf.Abs(hingeJoint.angle - hingeJoint.spring.targetPosition) <= springMaxDistance) {
				if (isGrabbed == true) {
					SetSpringStrength(springDefault * springStrengthGrabbed);
				} else {
					SetSpringStrength(springDefault);
				}
			} else {
				SetSpringStrength(springDefault * springStrengthOutsideMaxDistance);
			}
		}
	}

	public override void OnGrab (Player player, string handSide) {
		isGrabbed = true;
		playerGrabbing = player;
		handGrabbingSide = handSide;
	}

	public override void OnRelease() {
		isGrabbed = false;
		playerGrabbing = null;
		handGrabbingSide = "";
	}

	void SetSpringStrength (float strength) {
		if (hingeJoint.spring.spring != strength) {
			JointSpring newJointSpring = new JointSpring();

			newJointSpring.damper = hingeJoint.spring.damper;
			newJointSpring.spring = strength;
			newJointSpring.targetPosition = hingeJoint.spring.targetPosition;

			hingeJoint.spring = newJointSpring;
		}
	}

	void SetSpringTargetAngle (float targetAngle) {
		if (hingeJoint.spring.targetPosition != targetAngle) {
			JointSpring newJointSpring = new JointSpring();

			newJointSpring.damper = hingeJoint.spring.damper;
			newJointSpring.spring = hingeJoint.spring.spring;
			newJointSpring.targetPosition = targetAngle;

			hingeJoint.spring = newJointSpring;
		}
	}

	void OnDrawGizmosSelected() {
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position + GetComponent<HingeJoint>().anchor + (Quaternion.Euler(0, 0, 90) * transform.rotation * GetComponent<HingeJoint>().axis * hingeMovementNormalizeDistance), 0.125f);
	}

}
