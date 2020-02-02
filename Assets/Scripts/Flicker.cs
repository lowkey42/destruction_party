using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Flicker : MonoBehaviour
{
    
	private Light light;

	private Tweener tweener;

	private void OnEnable() {
		light = GetComponentInChildren<Light>();
		StartCoroutine(flicker());
	}

	private void OnDisable() {
		if(tweener!=null)
			tweener.Kill();

		light.intensity = 1;
	}

	private IEnumerator flicker() {
		yield return new WaitForSeconds(Random.Range(0, 2f));

		tweener = light.DOIntensity(0.2f, 0.1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash).SetDelay(0.5f);
	}

}
