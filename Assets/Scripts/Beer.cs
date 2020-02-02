using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Beer : MonoBehaviour
{
	public float energy = 1;

	private void OnEnable() {
		StartCoroutine(jump());
	}

	private IEnumerator jump() {
		yield return new WaitForSeconds(Random.Range(0, 2f));
		transform.GetChild(0).DOMoveY(0.5f, 1f).SetLoops(-1, LoopType.Yoyo);
	}

	private void OnTriggerEnter(Collider other) {
		var player = other.gameObject.GetComponent<PlayerController>();
		if(player!=null && player.drinkBeer(energy)) {
			energy = 0;
			Destroy(gameObject);
		}
	}

}
