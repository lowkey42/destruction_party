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
	public GameObject[] loot;

	public GameObject[] shards;

	private int health = 0;

	private bool destroyed = false;

	private MeshRenderer mesh;
	private Collider collider;

	private List<GameObject> spawnedShards = new List<GameObject>();

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

		transform.DOShakeScale(0.2f, new Vector3(0.8f,2,0.8f), 8, 18);

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

		mesh.enabled = false;
		collider.isTrigger = true;

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
	public bool repair() {
		if(health < maxHealth) {
			health++;
			if(health >= maxHealth) {
				destroyed = false;
				mesh.enabled = true;

				foreach(var s in spawnedShards) {
					Destroy(s);
				}
				spawnedShards.Clear();
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

	private IEnumerator ReEnableCollider() {
		yield return new WaitForSeconds(1f);

		collider.isTrigger = false;
	}


	public bool IsDestroyed() {
		return destroyed;
	}

}
