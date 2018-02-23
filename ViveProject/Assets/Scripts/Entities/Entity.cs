using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System;

public class Entity : MonoBehaviour {

	[Space(10)][Header("Entity Information")]
	public string entityName;
	public Vitals vitals;
	public StatusEffects statusEffects;
	public Attributes attributes;

	Transform target;
	NavMeshAgent navMeshAgent;

	// Events
	public event Action eventTakeDamage;
	public event Action eventDie;

	[Space(10)][Header("Prefabs")]
	public GameObject prefabDamageText;

	public float timeLastMeleeDamage;

	void Start () {
		if (vitals.healthCurrent == 0) {
			Die();
		}
	}

	void Update () {
		if (target != null && navMeshAgent != null) {
			navMeshAgent.SetDestination(target.position);
			navMeshAgent.transform.position = transform.position;
		}
	}

	public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal) {
		vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent -= damage, 0, vitals.healthMax);
		CreateDamageText(damage, hitPoint, hitNormal);
		if (eventTakeDamage != null) {
			eventTakeDamage.Invoke();
		}
		if (vitals.healthCurrent == 0) {
			Die();
		}
	}

	void CreateDamageText (int damage, Vector3 hitPoint, Vector3 hitNormal) {
		if (prefabDamageText != null) {
			GameObject newDamageText = (GameObject)Instantiate(prefabDamageText, hitPoint, Quaternion.identity);
			newDamageText.transform.position = hitPoint;
			newDamageText.GetComponent<DamageText>().velocity = ((UnityEngine.Random.rotation * Vector3.up) + (hitNormal * 3f) + (Vector3.up * 2)).normalized * 3f;
			newDamageText.transform.Find("Text").GetComponent<Text>().text = damage.ToString();
		}
	}

	public void Die () {
		vitals.isDead = true;
		if (eventDie != null) {
			eventDie.Invoke();
		}
	}

}
