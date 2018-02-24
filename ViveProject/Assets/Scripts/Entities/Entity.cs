using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System;
using System.Linq;

public class Entity : MonoBehaviour {

	[Space(10)][Header("Entity Information")]
	public string entityName;
	public Vitals vitals;
	public StatusEffects statusEffects;
	public Attributes attributes;

	public bool isPlayer;

	[Space(10)] [Header("Settings")]
	public Transform head;

	[Space(10)][Header("Relationships")]
	public List<Relationship> relationships = new List<Relationship>();
	public Relationship.Emotions defaultEmotionsEntities;
	public Relationship.Emotions defaultEmotionsItems;

	public MonoBehaviour focusedMonobehaviour;

	[Space(10)][Header("VisionMask")]
	public LayerMask visionMask;

	// Events
	public event Action eventTakeDamage;
	public event Action eventDie;

	[Space(10)][Header("Prefabs")]
	public GameObject prefabDamageText;

	Transform target;
	NavMeshAgent navMeshAgent;

	public Rigidbody rigidbody;

	public EntityManager entityManager;
	public ItemManager itemManager;

	public float timeLastMeleeDamage;
	public float stunThreshold;			// The minimum stun amount to be stunned;

	void Start () {
		if (vitals.healthCurrent == 0) {
			Die();
		}
	}

	void Update () {
		if (isPlayer == false) {
			Observe();
			UpdateNavigation();
		}

		UpdateVitals();
	}

	void UpdateVitals () {
		vitals.stunCurrent = Mathf.Clamp(vitals.stunCurrent - Time.deltaTime, 0, vitals.stunMax);
	}

	void Observe () {
		// This method handles the entity's observing of it's surroundings
		
		foreach (Entity observedEntity in entityManager.allEntities) {
			ObserveMonobehaviour(observedEntity);
		}

		foreach (Item observedItem in itemManager.allItems) {
			ObserveMonobehaviour(observedItem);
		}

		// Get most focused relationship & get lineOfSight
		if (relationships.Count > 0) {
			float focusHighest = -1;
			for (int i = 0; i < relationships.Count; i++) {
				if (relationships[i].emotions.focus > focusHighest) {
					focusHighest = relationships[i].emotions.focus;
					focusedMonobehaviour = relationships[i].monobehaviour;
				}

				RaycastHit visionHit;
				if (relationships[i].monobehaviour is Entity) {
					Entity relationshipEntity = relationships[i].monobehaviour as Entity;
					if (Physics.Raycast(head.transform.position, (relationshipEntity.head.position - head.transform.position), out visionHit, attributes.visionDistance, visionMask)) {
						if (visionHit.transform == relationships[i].monobehaviour.transform) {
							relationships[i].lineOfSight = true;
						} else {
							relationships[i].lineOfSight = false;
						}
					} else {
						relationships[i].lineOfSight = false;
					}
				} else if (relationships[i].monobehaviour is Item) {
					Item relationshipItem = relationships[i].monobehaviour as Item;
					if (Physics.Raycast(head.transform.position, (relationshipItem.transform.position - head.transform.position), out visionHit, attributes.visionDistance, visionMask)) {
						if (visionHit.transform == relationships[i].monobehaviour.transform) {
							relationships[i].lineOfSight = true;
						} else {
							relationships[i].lineOfSight = false;
						}
					} else {
						relationships[i].lineOfSight = false;
					}
				}
			}
		}
		
		// Adjust relationship focus
		foreach (Relationship relationship in relationships) {
			relationship.emotions.focus = Mathf.Lerp(relationship.emotions.focus, 0, 0.1f * Time.deltaTime);
			
			if (relationship.monobehaviour is Entity) {
				Entity relationshipEntity = relationship.monobehaviour as Entity;
				if (relationshipEntity.isPlayer == true) {
					relationship.emotions.focus = Mathf.Clamp(relationship.emotions.focus, 10, 100);
				} else {
					if (relationshipEntity.rigidbody != null) {
						relationship.emotions.focus = Mathf.Clamp(relationship.emotions.focus + Mathf.Sqrt(relationshipEntity.rigidbody.velocity.magnitude / 5), 0, 100);
					}
				}
			} else if (relationship.monobehaviour is Item) {
				Item relationshipItem = relationship.monobehaviour as Item;
				if (relationshipItem.itemRigidbody != null) {
					relationship.emotions.focus = Mathf.Clamp(relationship.emotions.focus + Mathf.Sqrt(relationshipItem.itemRigidbody.velocity.magnitude / 5), 0, 100);
				}
			}
		}

	}

	void ObserveMonobehaviour (MonoBehaviour observed) {
		if (observed is Entity) {
			Entity observedEntity = observed as Entity;
			if (observedEntity != this) {       // Make sure we're not trying to observe ourselves
				float dist = Vector3.Distance(observedEntity.head.transform.position, head.transform.position);
				if (dist < attributes.visionDistance) {        // Is the observed 
					Vector3 directionOfObserved = observedEntity.head.transform.position - head.transform.position;
					if (Vector3.Angle(directionOfObserved, head.transform.forward) <= attributes.visionFOV) {
						RaycastHit visionHit;
						if (Physics.Raycast(head.transform.position, directionOfObserved, out visionHit, attributes.visionDistance, visionMask)) {
							if (relationships.Any(p => p.monobehaviour == observedEntity) == false) {
								Relationship newRelationship = new Relationship(observedEntity.transform.name, observedEntity, observedEntity.head.transform.position, Time.time, defaultEmotionsEntities);
								newRelationship.emotions.focus = (attributes.visionDistance / dist);
								relationships.Add(newRelationship);
							}
						}
					}
				}
			}
		} else if (observed is Item) {
			Item observedItem = observed as Item;
			if (observedItem != this) {       // Make sure we're not trying to observe ourselves
				float dist = Vector3.Distance(observedItem.transform.position, head.transform.position);
				if (dist < attributes.visionDistance) {        // Is the observed 
					Vector3 directionOfObserved = observedItem.transform.position - head.transform.position;
					if (Vector3.Angle(directionOfObserved, head.transform.forward) <= attributes.visionFOV) {
						RaycastHit visionHit;
						if (Physics.Raycast(head.transform.position, directionOfObserved, out visionHit, attributes.visionDistance, visionMask)) {
							if (relationships.Any(p => p.monobehaviour == observedItem) == false) {
								Relationship newRelationship = new Relationship(observedItem.transform.name, observedItem, observedItem.transform.position, Time.time, defaultEmotionsEntities);
								newRelationship.emotions.focus = (attributes.visionDistance / dist);
								relationships.Add(newRelationship);
							}
						}
					}
				}
			}
		}
	}

	void UpdateNavigation () {
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

	public void TakeDamage(int damage, float stun, Vector3 hitPoint, Vector3 hitNormal) {
		vitals.healthCurrent = Mathf.Clamp(vitals.healthCurrent -= damage, 0, vitals.healthMax);
		vitals.stunCurrent = Mathf.Clamp(vitals.stunCurrent + stun, 0, vitals.stunMax);
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
