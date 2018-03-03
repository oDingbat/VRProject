using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class Weapon : Item {

	[Space(10)][Header("Weapon Information")]
	public WeaponType weaponType = WeaponType.Melee;
	public enum WeaponType { Melee, Gun };

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
	public Transform muzzleFlash;

	[System.Serializable]
	public class WeaponAttributes {
		[Header("Firing Attributes")]
		public bool automatic;
		public float firerate;
		public int burstCount;
		public float burstDelay;

		[Space(10)][Header("Ammo Attributes")]
		public int ammoMax;
		public int consumption;
		public bool consumePerBurst;

		[Space(10)][Header("Accuracy Attributes")]
		[Range(0, 1)]
		public float accuracyMax;							// The maximum accuracy the weapon can have
		[Range(0, 1)]
		public float accuracyMin;							// The minimum accuracy the weapon can have
		public float accuracyIncrement;						// The amount of accuracy added to accuracyCurrent per second
		public float accuracyDecrement;						// The amount of accuracy subtracted from accuracyCurrent per fire

		[Space(10)]
		[Header("Recoil Attributes")]
		public float recoilLinear;							// The amount of velocity added to the weapon when fired
		public float recoilAngular;							// The amount of angularVelocity added to the weapon when fired

		[Space(10)][Header("Charge Attributes")]
		public bool chargingEnabled;						// Does the weapon use charging?
		public float chargeIncrement;						// The amount charge increases per second
		public float chargeDecrement;						// The amount charge decreases per second
		public float chargeDecrementPerShot;				// The amount of charge lost once the weapon is fired
		public float chargeRequired;						// The amount of charge required to fire
		public float chargeInfluenceVelocity;				// The percentage of influence the charge amount has on projectile velocity

		[Space(10)][Header("Projectile Attributes & Spread Attributes")]
		public ProjectileAttributes projectileAttributes = new ProjectileAttributes();
		public Projectile.ProjectileSpreadAttributes projectileSpreadAttributes;

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
			projectileAttributes = new ProjectileAttributes(copiedAttributes.projectileAttributes);
			projectileSpreadAttributes = new Projectile.ProjectileSpreadAttributes(copiedAttributes.projectileSpreadAttributes);
			projectileAttributes.prefabProjectile = copiedAttributes.projectileAttributes.prefabProjectile;
		}

		public WeaponAttributes() {
			// All defaul values for attachment WeaponAttributes

			// booleans
			automatic = false;
			consumePerBurst = false;
			chargingEnabled = false;
			projectileSpreadAttributes.spreadType = Projectile.ProjectileSpreadAttributes.SpreadType.Circular;
			projectileAttributes.decelerationType = ProjectileAttributes.DecelerationType.Normal;
			projectileAttributes.isSticky = false;

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
			projectileAttributes.damage = 0;
			projectileSpreadAttributes.spreadDeviation = 0;
			projectileAttributes.ricochetAngleMax = 0;
			projectileAttributes.velocityInitial = 0;
			projectileAttributes.gravity = 0;
			projectileAttributes.deceleration = 0;
			projectileAttributes.lifespan = 0;

			// additions
			burstCount = 0;
			ammoMax = 0;
			consumption = 0;
			projectileAttributes.ricochetCountInitial = 0;

			projectileAttributes.prefabProjectile = null;
			//projectileSpreads = copiedAttributes.projectileSpreads;
		}

		public WeaponAttributes(bool baseValues) {
			// All defaul values for attachment WeaponAttributes

			// booleans
			automatic = false;
			consumePerBurst = false;
			chargingEnabled = false;
			projectileSpreadAttributes.spreadType = Projectile.ProjectileSpreadAttributes.SpreadType.Circular;
			projectileAttributes.decelerationType = ProjectileAttributes.DecelerationType.Normal;
			projectileAttributes.isSticky = false;

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
			projectileAttributes.damage = 1;
			projectileSpreadAttributes.spreadDeviation = 1;
			projectileAttributes.ricochetAngleMax = 1;
			projectileAttributes.velocityInitial = 1;
			projectileAttributes.gravity = 1;
			projectileAttributes.deceleration = 1;
			projectileAttributes.lifespan = 1;

			// additions
			burstCount = 0;
			ammoMax = 0;
			consumption = 0;
			projectileAttributes.ricochetCountInitial = 0;

			//projectileSpreads = copiedAttributes.projectileSpreads;
			projectileAttributes.prefabProjectile = null;
		}
	}
	
	[Space(10)][Header("Audio Info")]
	public AudioClip		soundFireNormal;                // The normal audio clip player when firing the weapon

	// Events
	public event Action eventAdjustAmmo;

	void Start () {
		UpdateCombinedAttributes();
	}

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
		
		if (transform.Find("(BarrelPoint)")) {
			
			barrelPoint = transform.Find("(BarrelPoint)");
			if (barrelPoint.Find("(MuzzleFlash)")) {
				muzzleFlash = barrelPoint.Find("(MuzzleFlash)");
			}
		}

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
				combinedAttachmentAttributes.projectileSpreadAttributes.spreadType = allAttributes[j].projectileSpreadAttributes.spreadType == Projectile.ProjectileSpreadAttributes.SpreadType.Custom ? allAttributes[j].projectileSpreadAttributes.spreadType : combinedAttachmentAttributes.projectileSpreadAttributes.spreadType;
				combinedAttachmentAttributes.projectileAttributes.decelerationType = allAttributes[j].projectileAttributes.decelerationType == ProjectileAttributes.DecelerationType.Smooth ? allAttributes[j].projectileAttributes.decelerationType : combinedAttachmentAttributes.projectileAttributes.decelerationType;
				combinedAttachmentAttributes.projectileAttributes.isSticky = allAttributes[j].projectileAttributes.isSticky == true ? allAttributes[j].projectileAttributes.isSticky : combinedAttachmentAttributes.projectileAttributes.isSticky;

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
				combinedAttachmentAttributes.projectileAttributes.damage += allAttributes[j].projectileAttributes.damage;
				combinedAttachmentAttributes.projectileSpreadAttributes.spreadDeviation += allAttributes[j].projectileSpreadAttributes.spreadDeviation;
				combinedAttachmentAttributes.projectileAttributes.ricochetAngleMax += allAttributes[j].projectileAttributes.ricochetAngleMax;
				combinedAttachmentAttributes.projectileAttributes.velocityInitial += allAttributes[j].projectileAttributes.velocityInitial;
				combinedAttachmentAttributes.projectileAttributes.gravity += allAttributes[j].projectileAttributes.gravity;
				combinedAttachmentAttributes.projectileAttributes.deceleration += allAttributes[j].projectileAttributes.deceleration;
				combinedAttachmentAttributes.projectileAttributes.lifespan += allAttributes[j].projectileAttributes.lifespan;

				// additions
				combinedAttachmentAttributes.burstCount += allAttributes[j].burstCount;
				combinedAttachmentAttributes.ammoMax += allAttributes[j].ammoMax;
				combinedAttachmentAttributes.consumption += allAttributes[j].consumption;
				combinedAttachmentAttributes.projectileAttributes.ricochetCountInitial += allAttributes[j].projectileAttributes.ricochetCountInitial;

				// Prefabs
				combinedAttachmentAttributes.projectileAttributes.prefabProjectile = allAttributes[j].projectileAttributes.prefabProjectile != null ? allAttributes[j].projectileAttributes.prefabProjectile : combinedAttachmentAttributes.projectileAttributes.prefabProjectile;

				//projectileSpreads = copiedAttributes.projectileSpreads;
			}
			
			// Combined the weapon's baseWeaponAttributes with the combined Attachment attributes
			// booleans
			combinedAttributes.automatic = combinedAttachmentAttributes.automatic == true ? combinedAttachmentAttributes.automatic : combinedAttributes.automatic;
			combinedAttributes.consumePerBurst = combinedAttachmentAttributes.consumePerBurst == true ? combinedAttachmentAttributes.consumePerBurst : combinedAttributes.consumePerBurst;
			combinedAttributes.chargingEnabled = combinedAttachmentAttributes.chargingEnabled == true ? combinedAttachmentAttributes.chargingEnabled : combinedAttributes.chargingEnabled;
			combinedAttributes.projectileSpreadAttributes.spreadType = combinedAttachmentAttributes.projectileSpreadAttributes.spreadType == Projectile.ProjectileSpreadAttributes.SpreadType.Custom ? combinedAttachmentAttributes.projectileSpreadAttributes.spreadType : combinedAttributes.projectileSpreadAttributes.spreadType;
			combinedAttributes.projectileAttributes.decelerationType = combinedAttachmentAttributes.projectileAttributes.decelerationType == ProjectileAttributes.DecelerationType.Smooth ? combinedAttachmentAttributes.projectileAttributes.decelerationType : combinedAttributes.projectileAttributes.decelerationType;
			combinedAttributes.projectileAttributes.isSticky = combinedAttachmentAttributes.projectileAttributes.isSticky == true ? combinedAttachmentAttributes.projectileAttributes.isSticky : combinedAttributes.projectileAttributes.isSticky;

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
			combinedAttributes.projectileAttributes.damage *= combinedAttachmentAttributes.projectileAttributes.damage;
			combinedAttributes.projectileSpreadAttributes.spreadDeviation *= combinedAttachmentAttributes.projectileSpreadAttributes.spreadDeviation;
			combinedAttributes.projectileAttributes.ricochetAngleMax *= combinedAttachmentAttributes.projectileAttributes.ricochetAngleMax;
			combinedAttributes.projectileAttributes.velocityInitial *= combinedAttachmentAttributes.projectileAttributes.velocityInitial;
			combinedAttributes.projectileAttributes.gravity *= combinedAttachmentAttributes.projectileAttributes.gravity;
			combinedAttributes.projectileAttributes.deceleration *= combinedAttachmentAttributes.projectileAttributes.deceleration;
			combinedAttributes.projectileAttributes.lifespan *= combinedAttachmentAttributes.projectileAttributes.lifespan;

			// additions
			combinedAttributes.burstCount += combinedAttachmentAttributes.burstCount;
			combinedAttributes.ammoMax += combinedAttachmentAttributes.ammoMax;
			combinedAttributes.consumption += combinedAttachmentAttributes.consumption;
			combinedAttributes.projectileAttributes.ricochetCountInitial += combinedAttachmentAttributes.projectileAttributes.ricochetCountInitial;

			//projectileSpreads = copiedAttributes.projectileSpreads;
			combinedAttributes.projectileAttributes.prefabProjectile = combinedAttachmentAttributes.projectileAttributes.prefabProjectile != null ? combinedAttachmentAttributes.projectileAttributes.prefabProjectile : combinedAttributes.projectileAttributes.prefabProjectile;
		}
	}

	public override string GetItemType() {
		return "Weapon";
	}

}
