using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyable : MonoBehaviour
{
	public int maxHealth = 1;

	public int points = 1;

	public int lootMinCount = 0;
	public int lootMaxCount = 5;
	public GameObject[] loot;

	public GameObject[] shards;

	private int health = 0;

	private MeshRenderer mesh;
	private Collider collider;

	private List<GameObject> spawnedShards = new List<GameObject>();

    void Start()
    {
        health = maxHealth;
		mesh = GetComponent<MeshRenderer>();
		collider = GetComponent<Collider>();
    }
	
	public bool canDamage() {
		return health>0;
	}
	public bool damage() {
		if(health<=0)
			return false;

		health--;
		if(health<=0) {
			Debug.Log("Damaged "+health);
			// TODO: animation
			// TODO: sound

			mesh.enabled = false;
			collider.isTrigger = true;

			var shardContainer = new GameObject("shards");
			foreach(var s in shards) {
				var offset = Random.insideUnitSphere;
				var shard = Instantiate(s, transform.position + offset*0.5f, Quaternion.identity);
				shard.transform.parent = shardContainer.transform;
				shard.GetComponent<Rigidbody>().AddForce(offset.x,offset.y,offset.z, ForceMode.Impulse);
				spawnedShards.Add(shard);
			}

			if(loot.Length>0) {
				var lootCount = Random.Range(lootMinCount, lootMaxCount+1);
				for(int i=0; i<lootCount; i++) {
					var offset = Random.insideUnitSphere;
					var l = Instantiate(loot[Random.Range(0, loot.Length)], transform.position + offset*0.5f, Quaternion.identity);
					l.GetComponent<Rigidbody>().AddForce(offset.x,2*offset.y,offset.z, ForceMode.Impulse);
				}
			}
		}

		return true;
	}

	public bool canRepair() {
		return health<maxHealth;
	}
	public bool repair() {
		if(health < maxHealth) {
			health++;
			if(health >= maxHealth) {
				mesh.enabled = true;
				collider.isTrigger = false;

				foreach(var s in spawnedShards) {
					Destroy(s);
				}
				spawnedShards.Clear();
			}

			// TODO: animate and move shard back to object center

			return true;
		}

		return false;
	}

}
