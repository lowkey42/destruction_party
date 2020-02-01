using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beer : MonoBehaviour
{
	public float energy = 1;

	private void OnTriggerEnter(Collider other) {
		var player = other.gameObject.GetComponent<PlayerController>();
		if(player!=null && player.drinkBeer(energy)) {
			energy = 0;
			Destroy(gameObject);
		}
	}

}
