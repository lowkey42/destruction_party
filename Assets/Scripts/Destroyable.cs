using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Destroyable : MonoBehaviour
{
	public bool spawnDestroyed = false;
	public int maxHealth = 1;

	public int points = 1;

	public int lootMinCount = 0;
	public int lootMaxCount = 5;

	public bool keepMesh = false;

	public GameObject[] loot;

	public GameObject[] shards;

	public bool throwAfterRepair = true;

	private int health = 0;

	private bool destroyed = false;

	private MeshRenderer mesh;
	private Collider collider;

	private List<GameObject> spawnedShards = new List<GameObject>();

	private Tweener damageTween;

    void Start()
    {
        health = maxHealth;
		mesh = GetComponentInChildren<MeshRenderer>();
		collider = GetComponentInChildren<Collider>();

		if(spawnDestroyed) {
			destoryObj();
		}
    }
	
	public bool canDamage() {
		return health>0;
	}
	public bool damage() {
		if(health<=0)
			return false;

		if(damageTween==null || !damageTween.IsPlaying())
			damageTween = transform.DOShakeScale(0.2f, new Vector3(0.6f,1.0f,0.6f), 8);
		else
			damageTween.Restart();

		health--;
		if(health<=0) {
			destoryObj();
		}

		return true;
	}

	private void destoryObj() {
		health = 0;
		destroyed = true;
		// TODO: sound

		if(!keepMesh) {
			mesh.enabled = false;
			collider.isTrigger = true;
		}

		var damagedSub = transform.Find("damaged");
		if(damagedSub!=null) {
			damagedSub.gameObject.SetActive(true);
		}

		if(damageTween!=null)
			damageTween.Kill();

		foreach(var light in GetComponentsInChildren<Light>()) {
			light.gameObject.AddComponent<Flicker>();
		}

		var shardContainer = new GameObject("shards");
		foreach(var s in shards) {
			var offset = Random.insideUnitSphere;
			offset.y = Mathf.Abs(offset.y);
			var shard = Instantiate(s, transform.position + offset, Random.rotation);
			shard.transform.parent = shardContainer.transform;
			shard.GetComponent<Rigidbody>().AddForce(10*offset.x,10*offset.y,10*offset.z, ForceMode.Impulse);
			spawnedShards.Add(shard);
		}

		if(loot.Length>0) {
			var lootCount = Random.Range(lootMinCount, lootMaxCount+1);
			for(int i=0; i<lootCount; i++) {
				var offset = Random.insideUnitSphere;
				offset.y = Mathf.Abs(offset.y);
				var l = Instantiate(loot[Random.Range(0, loot.Length)], transform.position + offset*0.5f, Quaternion.identity);
				l.GetComponent<Rigidbody>().AddForce(offset.x,10*offset.y,offset.z, ForceMode.Impulse);
			}
		}
	}

	public bool canRepair() {
		return health<maxHealth;
	}
	public bool repair(Color color) {
		if(health < maxHealth) {
			health++;
			if(health >= maxHealth) {
				destroyed = false;
				if(!keepMesh)
					mesh.enabled = true;
				
				var damagedSub = transform.Find("damaged");
				if(damagedSub!=null) {
					damagedSub.gameObject.SetActive(false);
				}
				StartCoroutine(ReEnableCollider(collider));

				foreach(var light in GetComponentsInChildren<Light>()) {
					var flicker = light.gameObject.GetComponent<Flicker>();
					if(flicker!=null)
						Destroy(flicker);
				}

				foreach(var s in spawnedShards) {
					Destroy(s);
				}
				spawnedShards.Clear();

				if(mesh!=null) {
					mesh.material.color = Color.Lerp(color, new Color(1,1,1,1), 0.75f);
				}

				if(throwAfterRepair) {
					throwStuff(gameObject);
					if(Random.value<0.33f) {
						var o = Random.onUnitSphere;
						o.y = Mathf.Abs(o.y);
						var ngo = Instantiate(gameObject, transform.position+o, Quaternion.identity);
						throwStuff(ngo);
						StartCoroutine(ReEnableCollider(ngo.GetComponentInChildren<Collider>()));
					}
				}
			}

			// TODO: sound effect

			foreach(var s in spawnedShards) {
				var diff = transform.position - s.transform.position;
				var len = diff.magnitude;
				if(len>0.3) {
					diff /= len;
					var body = s.GetComponent<Rigidbody>();
					body.useGravity = false;
					body.AddForce(diff*100, ForceMode.Impulse);
				}
			}

			return true;
		}

		return false;
	}

	private void throwStuff(GameObject go) {
		var p = go.transform.position;
		p.y += 2f;
		go.transform.position = p;

		var body = go.GetComponent<Rigidbody>();
		if(body==null)
			body = go.AddComponent<Rigidbody>();

		var offset = Random.insideUnitSphere;
		offset.y = Mathf.Abs(offset.y)+1;
		body.mass = 200;
		body.angularDrag = 2;
		body.AddForce(500*offset.x, 800*offset.y, 500*offset.z, ForceMode.Impulse);

		var torque = Random.insideUnitSphere*5;
		body.AddTorque(torque.x, torque.y, torque.z, ForceMode.Impulse);
	}

	private IEnumerator ReEnableCollider(Collider collider) {
		yield return new WaitForSeconds(1f);

		collider.isTrigger = false;
	}


	public bool IsDestroyed() {
		return destroyed;
	}

}
