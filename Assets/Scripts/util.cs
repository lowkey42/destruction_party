using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class Util {
	public static void PlayRandomSound(List<AudioClip> sounds, AudioSource src) {
		if(src!=null && sounds.Count>0) {
			src.PlayOneShot(sounds[Random.Range(0, sounds.Count)]);
		}
	}
}
