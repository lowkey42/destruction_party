using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
	public float actionRadius = 1f;
	public float actionOffset = 1f;

	public bool canDestroy = true;
	public bool canRepair = true;

	public bool needsBeer = true;

	public float beerMeterStart = 10;
	public float beerUsePerSecond = 1/5f;
	public float beerUsePerAttack = 1;

	public float moveForce = 10000;

	public float destroyerMoveForceFactor = 0.8f;

	public GameObject buttonIndicatorPrefab;

	public GameObject beerMeterPrefab;

	public GameObject modelHome;

	public GameObject modelParty;

	public Sprite[] buttonSprites;

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

	public PlayerTeam GetTeam(){
		return team;
	}

	public bool drinkBeer(float energy) {
		if(!needsBeer)
			return true; // TODO: design / gameplay-test

		if(beerMeter<=0) {
			moveForceFactor = destroyerMoveForceFactor;
		}

		beerMeter += energy;
		// TODO: sound / animation

		if(beerMeter>beerMeterStart) {
			beerMeter = beerMeterStart - beerUsePerAttack;
			// TODO: sound / animation
		}

		return true;
	}

    void Start()
    {
		beerMeter = beerMeterStart;

        rigidbody = GetComponent<Rigidbody>();

		GameObject canvas = GameObject.Find("Canvas");
		buttonIndicator = Instantiate(buttonIndicatorPrefab, Camera.main.WorldToScreenPoint(transform.position), Quaternion.identity);
		buttonIndicatorImage = buttonIndicator.GetComponent<Image>();
		buttonIndicator.transform.parent = canvas.transform;

		beerMeterIndicator = Instantiate(beerMeterPrefab, Camera.main.WorldToScreenPoint(transform.position), Quaternion.identity);
		beerMeterIndicator.transform.parent = canvas.transform;
		beerMeterIndicatorBar = beerMeterIndicator.transform.GetChild(0).gameObject;

		modelRef = Instantiate(modelHome, transform.position, Quaternion.identity);
		modelRef.transform.parent = transform;

		SwitchToDestroyer();
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
		modelRef = Instantiate(modelHome, transform.position, Quaternion.identity);
		modelRef.transform.parent = transform;
		modelRef.transform.localPosition = new Vector3(0,0,0);
		modelRef.transform.localRotation = Quaternion.identity;
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

		Destroy(modelRef);
		modelRef = Instantiate(modelParty, transform.position, Quaternion.identity);
		modelRef.transform.parent = transform;
		modelRef.transform.localPosition = new Vector3(0,0,0);
		modelRef.transform.localRotation = Quaternion.identity;
	}

    void Update()
    {
		var len = moveDir.magnitude;
		if(len>0) {
			moveDir /= len;
			lastMoveDir = moveDir;

			rigidbody.AddForce(moveDir.x*moveForce*moveForceFactor, 0, moveDir.y*moveForce*moveForceFactor);
		}

		transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(lastMoveDir.x, 0, lastMoveDir.y)), Time.deltaTime*10);

		var buttonIndicatorActive = false;

		foreach(var h in getObjectsInRange()) {
			var destr = h.gameObject.GetComponent<Destroyable>();
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

			beerMeter -= beerUsePerSecond * Time.deltaTime;
			if(beerMeter<0) {
				beerMeter = 0;
				moveForceFactor = destroyerMoveForceFactor/2;
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
	private void tryRepair(int button) {
		if(canRepair && actionPossible(false) && nextRepairButton==button) {
			Debug.Log("Repair");
			execAction(false);

			var maxPoints = 0;
			foreach(var h in getObjectsInRange()) {
				var destr = h.gameObject.GetComponent<Destroyable>();
				if(destr != null && maxPoints<destr.points) {
					maxPoints = destr.points;
				}
			}
			nextRepairButton = Random.Range(0, Mathf.Min(maxPoints,3)+1);
		}
	}
	public void OnAttack() {
		if(canDestroy && actionPossible(true) && beerMeter>0) {
			Debug.Log("Attack");
			execAction(true);
			beerMeter -= beerUsePerAttack;
		}
	}

	private void execAction(bool damage) {
		foreach(var h in getObjectsInRange()) {
			var destr = h.gameObject.GetComponent<Destroyable>();
			if(destr != null) {
				if(damage)
					destr.damage();
				else
					destr.repair();
			}
		}
	}

	private bool actionPossible(bool damage) {
		var ret = false;

		foreach(var h in getObjectsInRange()) {
			var destr = h.gameObject.GetComponent<Destroyable>();
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
		return Physics.OverlapSphere(transform.position+transform.rotation*new Vector3(0,0,-actionOffset), actionRadius);
	}

	void OnPlayerJoined() {
		// TODO
	}

	void OnPlayerLeft() {
		// TODO
	}
}
