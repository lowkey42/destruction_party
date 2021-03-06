﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
	public float actionRadius = 1f;
	public float actionOffset = 1f;

	public bool canDestroy = false;
	public bool canRepair = false;

	public bool needsBeer = false;

	public float beerMeterStart = 10;
	public float beerUsePerSecond = 1/5f;
	public float beerUsePerAttack = 1;

	public float moveForce = 10000;

	public float destroyerMoveForceFactor = 0.8f;

	public GameObject buttonIndicatorPrefab;

	public GameObject beerMeterPrefab;

	public GameObject modelHome;

	public GameObject modelParty;

	public GameObject modelNeutral;

	public GameObject repairEffect;

	public Sprite[] buttonSprites;

	public Color[] playerColors;

	public List<AudioClip> soundJoin;
	public List<AudioClip> soundTeamChange;
	public List<AudioClip> soundHit;
	public List<AudioClip> soundDrink;
	public List<AudioClip> soundDrinkFull;
	public List<AudioClip> soundBeerEmpty;
	public List<AudioClip> soundRepair;

	private AudioSource audioSource;

	private int usedColor = -1;

	private Rigidbody rigidbody;

	private GameObject buttonIndicator;

	private Image buttonIndicatorImage;

	private GameObject beerMeterIndicator;

	private GameObject beerMeterIndicatorBar;

	private GameObject modelRef;

	private int nextRepairButton = 0;

	private Vector2 moveDir = new Vector2(0,0);
	private Vector2 lastMoveDir = new Vector2(0,1);

	private PlayerTeam team = PlayerTeam.none;

	private float beerMeter = 100;

	private float moveForceFactor = 1;

	private float randomMoveDir = 0;
	private float randomMoveDirFactor = 1;
	private float drunkModelRotation = 0;

	private Tweener drinkTween;

	public PlayerTeam GetTeam(){
		return team;
	}

	private void attackPunch() {
		if(drinkTween==null || !drinkTween.IsPlaying())
			drinkTween = modelRef.transform.DOPunchRotation(new Vector3(70,0,0), 0.2f, 3, 0.2f);
	}

	public bool drinkBeer(float energy) {
		if(!needsBeer) {
			if(drinkTween==null || !drinkTween.IsPlaying())
				drinkTween = modelRef.transform.DOPunchScale(new Vector3(-0.2f,-0.4f,-0.2f), 0.3f, 8, 0.8f);
			return true; // TODO: design / gameplay-test
		}

		if(beerMeter<=0) {
			moveForceFactor = destroyerMoveForceFactor;
		}

		beerMeter += energy;

		if(beerMeter>beerMeterStart) {
			beerMeter = beerMeterStart*0.8f;
			Util.PlayRandomSound(soundDrinkFull, audioSource);

			if(drinkTween!=null && drinkTween.IsPlaying()) {
				drinkTween.Kill();
				modelRef.transform.localRotation = Quaternion.identity;
				modelRef.transform.localScale = new Vector3(1,1,1);
			}
			drinkTween = modelRef.transform.DOPunchRotation(new Vector3(-90,0,0), 0.6f, 1, 0.2f).SetEase(Ease.InOutBounce);

		} else {
			if(drinkTween==null || !drinkTween.IsPlaying())
				drinkTween = modelRef.transform.DOPunchScale(new Vector3(-0.2f,-0.4f,-0.2f), 0.3f, 8, 0.8f);

			Util.PlayRandomSound(soundDrink, audioSource);
		}

		return true;
	}

	public void ResetBeerMeter() {
		beerMeter = beerMeterStart;
	}

    void Start()
    {
		DontDestroyOnLoad(gameObject);

		List<int> colorIdxs = new List<int>();
		for(int i=0; i<playerColors.Length; i++) {
			colorIdxs.Add(i);
		}
		foreach(var p in Object.FindObjectsOfType<PlayerController>()) {
			if(p.usedColor>=0) {
				colorIdxs.Remove(p.usedColor);
			}
		}

		if(colorIdxs.Count==0) {
			Destroy(gameObject);
			return;
		}

		usedColor = colorIdxs[Random.Range(0,colorIdxs.Count)];

		beerMeter = beerMeterStart;

        rigidbody = GetComponent<Rigidbody>();
		audioSource = GetComponent<AudioSource>();

		GameObject canvas = GameObject.Find("Canvas");
		buttonIndicator = Instantiate(buttonIndicatorPrefab, Camera.main.WorldToScreenPoint(transform.position), Quaternion.identity);
		buttonIndicatorImage = buttonIndicator.GetComponent<Image>();
		buttonIndicator.transform.parent = canvas.transform;

		beerMeterIndicator = Instantiate(beerMeterPrefab, Camera.main.WorldToScreenPoint(transform.position), Quaternion.identity);
		beerMeterIndicator.transform.parent = canvas.transform;
		beerMeterIndicatorBar = beerMeterIndicator.transform.GetChild(0).gameObject;

		changeModel(modelNeutral);


		var spawnPoint = GameObject.Find("SpawnPoint");
		if(spawnPoint!=null) {
			var offset = Random.insideUnitSphere * 2;
			offset.y = 0;

			transform.position = offset + spawnPoint.transform.position;
		}

		Util.PlayRandomSound(soundJoin, audioSource);
    }

	private void changeModel(GameObject newPrefab) {
		modelRef = Instantiate(newPrefab, transform.position, Quaternion.identity);
		modelRef.transform.parent = transform;
		modelRef.transform.localPosition = new Vector3(0,0,0);
		modelRef.transform.localRotation = Quaternion.identity;

		var mesh = modelRef.GetComponentInChildren<MeshRenderer>();
		mesh.material.color = playerColors[usedColor];
	}

	private void onSwitch() {
		transform.DOLocalJump(transform.position+new Vector3(0,0,0), 2f, 1, 0.2f, false);
		transform.DOPunchScale(new Vector3(-0.2f,-0.7f,-0.2f), 0.2f, 5, 0.5f);
		transform.DORotate(new Vector3(0,360*2,0), 0.4f, RotateMode.WorldAxisAdd);
	}

	public void SwitchToRepairer() {
		if(team == PlayerTeam.repairer)
			return;

		canDestroy = false;
		canRepair = true;
		needsBeer = false;
		team = PlayerTeam.repairer;
		moveForceFactor = 1f;

		Destroy(modelRef);
		changeModel(modelHome);

		onSwitch();
		Util.PlayRandomSound(soundTeamChange, audioSource);
	}

	public void SwitchToDestroyer() {
		if(team == PlayerTeam.destroyer)
			return;
		
		canDestroy = true;
		canRepair = false;
		needsBeer = true;
		team = PlayerTeam.destroyer;
		nextRepairButton = 0;
		moveForceFactor = destroyerMoveForceFactor;
		beerMeter = beerMeterStart;

		Destroy(modelRef);
		changeModel(modelParty);

		onSwitch();
		Util.PlayRandomSound(soundTeamChange, audioSource);
	}

	public void SwitchToNeutral() {
		if(team == PlayerTeam.none)
			return;
		
		canDestroy = false;
		canRepair = false;
		needsBeer = false;
		team = PlayerTeam.none;
		nextRepairButton = 0;
		moveForceFactor = 1;

		Destroy(modelRef);
		changeModel(modelNeutral);
	}

    void FixedUpdate()
    {
		var p = transform.position;
		p.y = 0;
		transform.position = p;

		var cMoveDir = moveDir;
		var len = cMoveDir.magnitude;

		if(len>0) {
			lastMoveDir = cMoveDir / len;
		}


		if(needsBeer) {
			float targetDir = Mathf.Atan2(lastMoveDir.y, lastMoveDir.x);

			var drunkDir = new Vector2(Mathf.Sin(randomMoveDir), -Mathf.Cos(randomMoveDir));

			randomMoveDir += Time.deltaTime * (len>0.1f ? 5f : 4f) * randomMoveDirFactor;

			if(randomMoveDirFactor<0 && randomMoveDir-targetDir> 45/Mathf.Rad2Deg)
				randomMoveDirFactor *= -1;
			else if(randomMoveDirFactor>0 && randomMoveDir-targetDir < -45/Mathf.Rad2Deg)
				randomMoveDirFactor *= -1;
			
			if(len<0.1f) {
				cMoveDir = lastMoveDir*0.15f + drunkDir*0.2f;
			} else {
				cMoveDir += drunkDir*0.25f;
			}

			len = cMoveDir.magnitude;


			if(drinkTween==null || !drinkTween.IsPlaying()) {
				modelRef.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(Mathf.Sin(drunkModelRotation)*0.2f, 1, 0.2f*-Mathf.Cos(drunkModelRotation)).normalized);
				drunkModelRotation += Time.deltaTime * 4f;
			}

		} else {
			if(drinkTween==null || !drinkTween.IsPlaying()) {
				if(rigidbody.velocity.magnitude > 0.1f) {
					modelRef.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(Mathf.Sin(drunkModelRotation)*0.02f * rigidbody.velocity.magnitude, 1, 0.05f*-Mathf.Cos(drunkModelRotation)).normalized);
					drunkModelRotation += Time.deltaTime * 10f;

				} else {
					modelRef.transform.localRotation = Quaternion.Lerp(modelRef.transform.localRotation, Quaternion.identity, Time.deltaTime*10);
				}
			}
		}
		

		if(len>0) {
			if(len>1) {
				cMoveDir /= len;
			}

			rigidbody.AddForce(cMoveDir.x*moveForce*moveForceFactor, 0, cMoveDir.y*moveForce*moveForceFactor);
		}

		transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(lastMoveDir.x, 0, lastMoveDir.y)), Time.deltaTime*10);

		var buttonIndicatorActive = false;

		foreach(var h in getObjectsInRange()) {
			var destr = h.gameObject.GetComponentInParent<Destroyable>();
			if(destr != null) {
				if(canDestroy && destr.canDamage() && beerMeter>0) {
					buttonIndicatorActive = true;
				}

				if(canRepair && destr.canRepair()) {
					buttonIndicatorActive = true;
				}
			}
		}

		buttonIndicator.SetActive(buttonIndicatorActive);
		if(buttonIndicatorActive) {
			buttonIndicatorImage.overrideSprite = buttonSprites[nextRepairButton];
		}

		var screenPos = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0,2,0));
		buttonIndicator.transform.position = screenPos;

		beerMeterIndicator.SetActive(needsBeer);
		if(needsBeer) {
			beerMeterIndicator.transform.position = screenPos + new Vector3(0,30,0);

			var nonEmpty = beerMeter>0;
			beerMeter -= beerUsePerSecond * Time.deltaTime;
			if(beerMeter<0) {
				beerMeter = 0;
				moveForceFactor = destroyerMoveForceFactor/2;
				if(nonEmpty)
					Util.PlayRandomSound(soundBeerEmpty, audioSource);
			}

			var barScale = beerMeterIndicatorBar.transform.localScale;
			barScale.x = beerMeter/beerMeterStart;
			beerMeterIndicatorBar.transform.localScale = barScale;
			// TODO: change bar color
		}
    }

	public void OnMove(InputValue value){
		moveDir = value.Get<Vector2>();
	}

    public void OnRepairA() {
		tryRepair(0);
	}
    public void OnRepairB() {
		tryRepair(1);
	}
    public void OnRepairX() {
		tryRepair(2);
	}
    public void OnRepairY() {
		tryRepair(3);
	}

	private Coroutine showRepairCorutine;
	private void tryRepair(int button) {
		if(canRepair && nextRepairButton==button) {
			attackPunch();
			Util.PlayRandomSound(soundRepair, audioSource);
			Util.PlayRandomSound(soundRepair, audioSource);

			if(actionPossible(false)) {
				execAction(false);

				if(showRepairCorutine!=null)
					StopCoroutine(showRepairCorutine);
				
				showRepairCorutine = StartCoroutine(ShowRepairEffect());

				var maxPoints = 0;
				foreach(var h in getObjectsInRange()) {
					var destr = h.gameObject.GetComponentInParent<Destroyable>();
					if(destr != null && maxPoints<destr.points) {
						maxPoints = destr.points;
					}
				}
				nextRepairButton = Random.Range(0, Mathf.Min(maxPoints,3)+1);
				nextRepairButton = 0; // TODO: remove this line if we want quick-time-events back
			}
		}
	}

	private IEnumerator ShowRepairEffect() {
		repairEffect.SetActive(true);
		yield return new WaitForSeconds(1f);
		repairEffect.SetActive(false);
	}

	public void OnAttack() {

		if(canDestroy) {
			attackPunch();
			Util.PlayRandomSound(soundHit, audioSource);
			if(actionPossible(true)) {
				execAction(true);
				if(beerMeter>0) {
					beerMeter -= beerUsePerAttack;
					if(beerMeter<=0) {
						beerMeter = 0;
						Util.PlayRandomSound(soundBeerEmpty, audioSource);
					}
				}
			}
		}
	}

	private void execAction(bool damage) {
		foreach(var h in getObjectsInRange()) {
			var destr = h.gameObject.GetComponentInParent<Destroyable>();
			if(destr != null) {
				if(damage)
					destr.damage();
				else
					destr.repair(playerColors[usedColor]);
			}
		}
	}

	private bool actionPossible(bool damage) {
		var ret = false;

		foreach(var h in getObjectsInRange()) {
			var destr = h.gameObject.GetComponentInParent<Destroyable>();
			if(destr != null) {
				if(damage)
					ret = ret || destr.canDamage();
				else
					ret = ret || destr.canRepair();
			}
		}

		return ret;
	}

	private Collider[] getObjectsInRange() {
		return Physics.OverlapSphere(transform.position+transform.rotation*new Vector3(0,actionRadius,actionOffset), actionRadius);
	}

	void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
	Gizmos.DrawSphere(transform.position+transform.rotation*new Vector3(0,actionRadius,actionOffset), actionRadius);
#endif
	}

	void OnPlayerJoined() {
		// TODO
	}

	void OnPlayerLeft() {
		// TODO
	}
}
