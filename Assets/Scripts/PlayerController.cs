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

	public GameObject buttonIndicatorPrefab;

	public GameObject beerMeterPrefab;

	public GameObject modelHome;

	public GameObject modelParty;

	public Sprite[] buttonSprites;

	private Rigidbody rigidbody;

	private GameObject buttonIndicator;

	private Image buttonIndicatorImage;

	private GameObject beerMeter;

	private GameObject modelRef;

	private int nextRepairButton = 0;

	private Vector2 lastMoveDir = new Vector2(0,-1);

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();

		GameObject canvas = GameObject.Find("Canvas");
		buttonIndicator = Instantiate(buttonIndicatorPrefab, Camera.main.WorldToScreenPoint(transform.position), Quaternion.identity);
		buttonIndicatorImage = buttonIndicator.GetComponent<Image>();
		buttonIndicator.transform.parent = canvas.transform;

		beerMeter = Instantiate(beerMeterPrefab, Camera.main.WorldToScreenPoint(transform.position), Quaternion.identity);
		beerMeter.transform.parent = canvas.transform;

		modelRef = Instantiate(modelHome, transform.position, Quaternion.identity);
		modelRef.transform.parent = transform;

		SwitchToDestroyer();
    }

	public void SwitchToRepairer() {
		canDestroy = false;
		canRepair = true;
		needsBeer = false;

		Destroy(modelRef);
		modelRef = Instantiate(modelHome, transform.position, Quaternion.identity);
		modelRef.transform.parent = transform;
	}

	public void SwitchToDestroyer() {
		canDestroy = true;
		canRepair = false;
		needsBeer = true;

		Destroy(modelRef);
		modelRef = Instantiate(modelParty, transform.position, Quaternion.identity);
		modelRef.transform.parent = transform;
	}

    // Update is called once per frame
    void Update()
    {
		Vector2 moveDir = new Vector2(0,0);

		var gamepad = Gamepad.current;
		if (gamepad != null)
			moveDir = gamepad.leftStick.ReadValue();

		var kb = Keyboard.current;
		if(kb != null) {
			if(kb.upArrowKey.isPressed)
				moveDir.y = 1;
			else if(kb.downArrowKey.isPressed)
				moveDir.y = -1;
			if(kb.leftArrowKey.isPressed)
				moveDir.x = -1;
			else if(kb.rightArrowKey.isPressed)
				moveDir.x = 1;
		}

		var len = moveDir.magnitude;
		if(len>0) {
			moveDir /= len;
			lastMoveDir = moveDir;

			rigidbody.AddForce(moveDir.x*10000, 0, moveDir.y*10000);
		}

		transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(new Vector3(lastMoveDir.x, 0, lastMoveDir.y)), Time.deltaTime*10);

		var buttonIndicatorActive = false;

		foreach(var h in getObjectsInRange()) {
			var destr = h.gameObject.GetComponent<Destroyable>();
			if(destr != null) {
				if(canDestroy && destr.canDamage()) {
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

		beerMeter.SetActive(needsBeer);
		if(needsBeer)
			beerMeter.transform.position = screenPos + new Vector3(0,30,0);
    }

	public void OnMove(){}

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
		if(canRepair && actionPossible(true) && nextRepairButton==button) {
			Debug.Log("Repair");
			execAction(true);

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
		if(canDestroy && actionPossible(true)) {
			Debug.Log("Attack");
			execAction(true);
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
