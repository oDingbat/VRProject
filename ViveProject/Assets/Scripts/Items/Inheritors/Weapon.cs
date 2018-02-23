using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class Weapon : Item {

	[Space(10)][Header("Firing Variables")]
	public bool triggerHeld;
	public float timeLastFired;
	public float timeLastTriggered;

	[Space(10)][Header("Ammo Variables")]
	public int ammoCurrent;

	[Space(10)][Header("Accuracy Variables")]
	public float accuracyCurrent;                   // The current accuracy of the weapon

	[Space(10)][Header("Charge Variables")]
	public float chargeCurrent;                     // The current percentage of charge

	[Space(10)][Header("Weapon Attributes")]
	public WeaponAttributes			baseAttributes;
	public WeaponAttributes			combinedAttributes;

	[Space(10)][Header("Position Attributes")]
	public Transform barrelPoint;

	[System.Serializable]
	public class WeaponAttributes {
		[Header("Firing Attributes")]
		public bool automatic;
		public float firerate;
		public int burstCount;
		public float burstDelay;

		[Space(10)]
		[Header("Ammo Attributes")]
		public int ammoMax;
		public int consumption;
		public bool consumePerBurst;

		[Space(10)]
		[Header("Accuracy Attributes")]
		[Range(0, 1)]
		public float accuracyMax;                       // The maximum accuracy the weapon can have
		[Range(0, 1)]
		public float accuracyMin;                       // The minimum accuracy the weapon can have
		public float accuracyIncrement;                 // The amount of accuracy added to accuracyCurrent per second
		public float accuracyDecrement;                 // The amount of accuracy subtracted from accuracyCurrent per fire

		[Space(10)]
		[Header("Recoil Attributes")]
		public float recoilLinear;                      // The amount of velocity added to the weapon when fired
		public float recoilAngular;                     // The amount of angularVelocity added to the weapon when fired

		[Space(10)]
		[Header("Charge Attributes")]
		public bool chargingEnabled;                    // Does the weapon use charging?
		public float chargeIncrement;                   // The amount charge increases per second
		public float chargeDecrement;                   // The amount charge decreases per second
		public float chargeDecrementPerShot;            // The amount of charge lost once the weapon is fired
		public float chargeRequired;                    // The amount of charge required to fire
		public float chargeInfluenceVelocity;           // The percentage of influence the charge amount has on projectile velocity

		[Space(10)]
		[Header("Projectile Attributes")]
		public int projectileBaseDamage;				// The base damage of the projectile
		public Vector2[] projectileSpreads;             // The projectile spreads of the weapon. Each index represents an individual spread the the rotation of that index being applied to it. Used for multi-projectile based weapons (ie: shotguns)
		public SpreadType projectileSpreadType;
		public float projectileSpreadDeviation;         // The random rotation deviation given to each projectile in projectile spreads
		public int projectileRicochetCount;             // The maximum number of times a projectile can ricochet
		public float projectileRicochetAngleMax;        // The maximum angle projectiles can ricochet off of
		public float projectileVelocity;                // The forward velocity applied to all projectiles when fired
		public float projectileGravity;                 // The percentage of gravity the projectile has
		public Projectile.DecelerationType projectileDecelerationType;  // The deceleration type of the projectile (normal or logarithmic)
		public float projectileDeceleration;            // The deceleration applied to the projectiles
		public float projectileLifespan;
		public bool projectileIsSticky;
		public GameObject projectile;                   // The projectile prefab which is instantiated when firing the weapon

		public WeaponAttributes(WeaponAttributes copiedAttributes) {
			automatic = copiedAttributes.automatic;
			firerate = copiedAttributes.firerate;
			burstCount = copiedAttributes.burstCount;
			burstDelay = copiedAttributes.burstDelay;
			ammoMax = copiedAttributes.ammoMax;
			consumption = copiedAttributes.consumption;
			consumePerBurst = copiedAttributes.consumePerBurst;
			accuracyMax = copiedAttributes.accuracyMax;
			accuracyMin = copiedAttributes.accuracyMin;
			accuracyIncrement = copiedAttributes.accuracyIncrement;
			accuracyDecrement = copiedAttributes.accuracyDecrement;
			recoilLinear = copiedAttributes.recoilLinear;
			recoilAngular = copiedAttributes.recoilAngular;
			chargingEnabled = copiedAttributes.chargingEnabled;
			chargeIncrement = copiedAttributes.chargeIncrement;
			chargeDecrement = copiedAttributes.chargeDecrement;
			chargeDecrementPerShot = copiedAttributes.chargeDecrementPerShot;
			chargeRequired = copiedAttributes.chargeRequired;
			chargeInfluenceVelocity = copiedAttributes.chargeInfluenceVelocity;
			projectileBaseDamage = copiedAttributes.projectileBaseDamage;
			projectileSpreads = copiedAttributes.projectileSpreads;
			projectileSpreadType = copiedAttributes.projectileSpreadType;
			projectileSpreadDeviation = copiedAttributes.projectileSpreadDeviation;
			projectileRicochetCount = copiedAttributes.projectileRicochetCount;
			projectileRicochetAngleMax = copiedAttributes.projectileRicochetAngleMax;
			projectileVelocity = copiedAttributes.projectileVelocity;
			projectileGravity = copiedAttributes.projectileGravity;
			projectileDecelerationType = copiedAttributes.projectileDecelerationType;
			projectileDeceleration = copiedAttributes.projectileDeceleration;
			projectileLifespan = copiedAttributes.projectileLifespan;
			projectileIsSticky = copiedAttributes.projectileIsSticky;
			projectile = copiedAttributes.projectile;
		}

		public WeaponAttributes() {
			// All defaul values for attachment WeaponAttributes

			// booleans
			automatic = false;
			consumePerBurst = false;
			chargingEnabled = false;
			projectileSpreadType = SpreadType.Circular;
			projectileDecelerationType = Projectile.DecelerationType.Normal;
			projectileIsSticky = false;

			// percentages
			firerate = 0;
			burstDelay = 0;
			accuracyMax = 0;
			accuracyMin = 0;
			accuracyIncrement = 0;
			accuracyDecrement = 0;
			recoilLinear = 0;
			recoilAngular = 0;
			chargeIncrement = 0;
			chargeDecrement = 0;
			chargeDecrementPerShot = 0;
			chargeRequired = 0;
			chargeInfluenceVelocity = 0;
			projectileBaseDamage = 0;
			projectileSpreadDeviation = 0;
			projectileRicochetAngleMax = 0;
			projectileVelocity = 0;
			projectileGravity = 0;
			projectileDeceleration = 0;
			projectileLifespan = 0;

			// additions
			burstCount = 0;
			ammoMax = 0;
			consumption = 0;
			projectileRicochetCount = 0;

			projectile = null;
			//projectileSpreads = copiedAttributes.projectileSpreads;
		}

		public WeaponAttributes(bool baseValues) {
			// All defaul values for attachment WeaponAttributes

			// booleans
			automatic = false;
			consumePerBurst = false;
			chargingEnabled = false;
			projectileSpreadType = SpreadType.Circular;
			projectileDecelerationType = Projectile.DecelerationType.Normal;
			projectileIsSticky = false;

			// percentages
			
			firerate = 1;
			burstDelay = 1;
			accuracyMax = 1;
			accuracyMin = 1;
			accuracyIncrement = 1;
			accuracyDecrement = 1;
			recoilLinear = 1;
			recoilAngular = 1;
			chargeIncrement = 1;
			chargeDecrement = 1;
			chargeDecrementPerShot = 1;
			chargeRequired = 1;
			chargeInfluenceVelocity = 1;
			projectileBaseDamage = 1;
			projectileSpreadDeviation = 1;
			projectileRicochetAngleMax = 1;
			projectileVelocity = 1;
			projectileGravity = 1;
			projectileDeceleration = 1;
			projectileLifespan = 1;

			// additions
			burstCount = 0;
			ammoMax = 0;
			consumption = 0;
			projectileRicochetCount = 0;

			//projectileSpreads = copiedAttributes.projectileSpreads;
			projectile = null;
		}
	}

	public enum SpreadType { Circular, Custom }

	[Space(10)][Header("Audio Info")]
	public AudioClip		soundFireNormal;                // The normal audio clip player when firing the weapon

	// Events
	public event Action eventAdjustAmmo;

	protected override void OnItemUpdate () {
		if (triggerHeld == false) {
			if (combinedAttributes.chargingEnabled == true) {
				chargeCurrent = Mathf.Clamp01(chargeCurrent - (combinedAttributes.chargeDecrement * Time.deltaTime));
			}
		}
	}

	public void AdjustAmmo (int a) {
		ammoCurrent = (int)Mathf.Clamp(ammoCurrent + a, 0, combinedAttributes.ammoMax);
		if (eventAdjustAmmo != null) {
			eventAdjustAmmo.Invoke();
		}
	}

	public void UpdateCombinedAttributes () {
		// Set combinedAttributes equal to the weapon's base attributes first
		combinedAttributes = new WeaponAttributes(baseAttributes);

		barrelPoint = transform.Find("(BarrelPoint)");

		if (attachments.Count > 0) {
			List<WeaponAttributes> allAttributes = new List<WeaponAttributes>();

			// Add all of the currently used attributes to the allAttributes list
			for (int i = 0; i < attachments.Count; i++) {
				if (attachments[i] is Attachment) {         // Is the current attachment actually an Attachment class Object
					Attachment attachmentObject = attachments[i] as Attachment;

					// Creating allAttributes List
					if (attachmentObject.isGrabbed == true && attachmentObject.attributesAlwaysPassive == false) {       // Is the attachment currently being grabbed and alwaysPassive is false?
						allAttributes.Add(attachmentObject.attachmentAttributesActive);		// If yes, add its active attributes
					} else {
						allAttributes.Add(attachmentObject.attachmentAttributesPassive);		// If no, add its passive attributes
					}

					// Finding Barrel Point
					if (attachments[i].transform.Find("(BarrelPoint)") != null) {
						bool currentNodeWorks = true;
						for (int a = 0; a < attachments[i].attachmentNodes.Count; a++) {
							if (attachments[i].attachmentNodes[a].attachmentType == AttachmentNode.AttachmentType.Barrel && attachments[i].attachmentNodes[a].isAttached == false) {
								currentNodeWorks = false;
							}
						}

						if (currentNodeWorks == true) {
							barrelPoint = attachments[i].transform.Find("(BarrelPoint)");
						}
					}
				}
			}

			// Combined all of the attachments' weapon attributes together in combinedAttributes
			WeaponAttributes combinedAttachmentAttributes = new WeaponAttributes(true);
			for (int j = 0; j < allAttributes.Count; j++) {
				// booleans
				combinedAttachmentAttributes.automatic = allAttributes[j].automatic == true ? allAttributes[j].automatic : combinedAttachmentAttributes.automatic;
				combinedAttachmentAttributes.consumePerBurst = allAttributes[j].consumePerBurst == true ? allAttributes[j].consumePerBurst : combinedAttachmentAttributes.consumePerBurst;
				combinedAttachmentAttributes.chargingEnabled = allAttributes[j].chargingEnabled == true ? allAttributes[j].chargingEnabled : combinedAttachmentAttributes.chargingEnabled;
				combinedAttachmentAttributes.projectileSpreadType = allAttributes[j].projectileSpreadType == SpreadType.Custom ? allAttributes[j].projectileSpreadType : combinedAttachmentAttributes.projectileSpreadType;
				combinedAttachmentAttributes.projectileDecelerationType = allAttributes[j].projectileDecelerationType == Projectile.DecelerationType.Smooth ? allAttributes[j].projectileDecelerationType : combinedAttachmentAttributes.projectileDecelerationType;
				combinedAttachmentAttributes.projectileIsSticky = allAttributes[j].projectileIsSticky == true ? allAttributes[j].projectileIsSticky : combinedAttachmentAttributes.projectileIsSticky;

				// percentages
				combinedAttachmentAttributes.firerate += allAttributes[j].firerate;
				combinedAttachmentAttributes.burstDelay += allAttributes[j].burstDelay;
				combinedAttachmentAttributes.accuracyMax += allAttributes[j].accuracyMax;
				combinedAttachmentAttributes.accuracyMin += allAttributes[j].accuracyMin;
				combinedAttachmentAttributes.accuracyIncrement += allAttributes[j].accuracyIncrement;
				combinedAttachmentAttributes.accuracyDecrement += allAttributes[j].accuracyDecrement;
				combinedAttachmentAttributes.recoilLinear += allAttributes[j].recoilLinear;
				combinedAttachmentAttributes.recoilAngular += allAttributes[j].recoilAngular;
				combinedAttachmentAttributes.chargeIncrement += allAttributes[j].chargeIncrement;
				combinedAttachmentAttributes.chargeDecrement += allAttributes[j].chargeDecrement;
				combinedAttachmentAttributes.chargeDecrementPerShot += allAttributes[j].chargeDecrementPerShot;
				combinedAttachmentAttributes.chargeRequired += allAttributes[j].chargeRequired;
				combinedAttachmentAttributes.chargeInfluenceVelocity += allAttributes[j].chargeInfluenceVelocity;
				combinedAttachmentAttributes.projectileBaseDamage += allAttributes[j].projectileBaseDamage;
				combinedAttachmentAttributes.projectileSpreadDeviation += allAttributes[j].projectileSpreadDeviation;
				combinedAttachmentAttributes.projectileRicochetAngleMax += allAttributes[j].projectileRicochetAngleMax;
				combinedAttachmentAttributes.projectileVelocity += allAttributes[j].projectileVelocity;
				combinedAttachmentAttributes.projectileGravity += allAttributes[j].projectileGravity;
				combinedAttachmentAttributes.projectileDeceleration += allAttributes[j].projectileDeceleration;
				combinedAttachmentAttributes.projectileLifespan += allAttributes[j].projectileLifespan;

				// additions
				combinedAttachmentAttributes.burstCount += allAttributes[j].burstCount;
				combinedAttachmentAttributes.ammoMax += allAttributes[j].ammoMax;
				combinedAttachmentAttributes.consumption += allAttributes[j].consumption;
				combinedAttachmentAttributes.projectileRicochetCount += allAttributes[j].projectileRicochetCount;

				// Prefabs
				combinedAttachmentAttributes.projectile = allAttributes[j].projectile != null ? allAttributes[j].projectile : combinedAttachmentAttributes.projectile;

				//projectileSpreads = copiedAttributes.projectileSpreads;
			}
			
			// Combined the weapon's baseWeaponAttributes with the combined Attachment attributes
			// booleans
			combinedAttributes.automatic = combinedAttachmentAttributes.automatic == true ? combinedAttachmentAttributes.automatic : combinedAttributes.automatic;
			combinedAttributes.consumePerBurst = combinedAttachmentAttributes.consumePerBurst == true ? combinedAttachmentAttributes.consumePerBurst : combinedAttributes.consumePerBurst;
			combinedAttributes.chargingEnabled = combinedAttachmentAttributes.chargingEnabled == true ? combinedAttachmentAttributes.chargingEnabled : combinedAttributes.chargingEnabled;
			combinedAttributes.projectileSpreadType = combinedAttachmentAttributes.projectileSpreadType == SpreadType.Custom ? combinedAttachmentAttributes.projectileSpreadType : combinedAttributes.projectileSpreadType;
			combinedAttributes.projectileDecelerationType = combinedAttachmentAttributes.projectileDecelerationType == Projectile.DecelerationType.Smooth ? combinedAttachmentAttributes.projectileDecelerationType : combinedAttributes.projectileDecelerationType;
			combinedAttributes.projectileIsSticky = combinedAttachmentAttributes.projectileIsSticky == true ? combinedAttachmentAttributes.projectileIsSticky : combinedAttributes.projectileIsSticky;

			// percentages
			combinedAttributes.firerate *= combinedAttachmentAttributes.firerate;
			combinedAttributes.burstDelay *= combinedAttachmentAttributes.burstDelay;
			combinedAttributes.accuracyMax *= combinedAttachmentAttributes.accuracyMax;
			combinedAttributes.accuracyMin *= combinedAttachmentAttributes.accuracyMin;
			combinedAttributes.accuracyIncrement *= combinedAttachmentAttributes.accuracyIncrement;
			combinedAttributes.accuracyDecrement *= combinedAttachmentAttributes.accuracyDecrement;
			combinedAttributes.recoilLinear *= combinedAttachmentAttributes.recoilLinear;
			combinedAttributes.recoilAngular *= combinedAttachmentAttributes.recoilAngular;
			combinedAttributes.chargeIncrement *= combinedAttachmentAttributes.chargeIncrement;
			combinedAttributes.chargeDecrement *= combinedAttachmentAttributes.chargeDecrement;
			combinedAttributes.chargeDecrementPerShot *= combinedAttachmentAttributes.chargeDecrementPerShot;
			combinedAttributes.chargeRequired *= combinedAttachmentAttributes.chargeRequired;
			combinedAttributes.chargeInfluenceVelocity *= combinedAttachmentAttributes.chargeInfluenceVelocity;
			combinedAttributes.projectileBaseDamage *= combinedAttachmentAttributes.projectileBaseDamage;
			combinedAttributes.projectileSpreadDeviation *= combinedAttachmentAttributes.projectileSpreadDeviation;
			combinedAttributes.projectileRicochetAngleMax *= combinedAttachmentAttributes.projectileRicochetAngleMax;
			combinedAttributes.projectileVelocity *= combinedAttachmentAttributes.projectileVelocity;
			combinedAttributes.projectileGravity *= combinedAttachmentAttributes.projectileGravity;
			combinedAttributes.projectileDeceleration *= combinedAttachmentAttributes.projectileDeceleration;
			combinedAttributes.projectileLifespan *= combinedAttachmentAttributes.projectileLifespan;

			// additions
			combinedAttributes.burstCount += combinedAttachmentAttributes.burstCount;
			combinedAttributes.ammoMax += combinedAttachmentAttributes.ammoMax;
			combinedAttributes.consumption += combinedAttachmentAttributes.consumption;
			combinedAttributes.projectileRicochetCount += combinedAttachmentAttributes.projectileRicochetCount;

			//projectileSpreads = copiedAttributes.projectileSpreads;
			combinedAttributes.projectile = combinedAttachmentAttributes.projectile != null ? combinedAttachmentAttributes.projectile : combinedAttributes.projectile;
		}
	}

	public override string GetItemType() {
		return "Weapon";
	}

}
