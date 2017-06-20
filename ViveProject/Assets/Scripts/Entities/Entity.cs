using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class Entity : MonoBehaviour {

	public Vitals vitals;
	public StatusEffects statusEffects;
	public Attributes attributes;

	public Transform target;

	public NavMeshAgent navMeshAgent;

	// Events
	public event Action eventTakeDamage;
	public event Action eventDie;

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

	public void TakeDamage (int damageAmount) {
		vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent -= damageAmount, 0, vitals.healthMax);
		if (eventTakeDamage != null) {
			eventTakeDamage.Invoke();
		}
		if (vitals.healthCurrent == 0) {
			Die();
		}
	}

	public void Die () {
		vitals.isDead = true;
		if (eventDie != null) {
			eventDie.Invoke();
		}
	}

}
