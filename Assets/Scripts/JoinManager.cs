using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class JoinManager : MonoBehaviour
{

	public int startDelay = 5;

	private float statDelayLeft = 0;

	private PlayerInputManager inputManager;

	void Start() {
		statDelayLeft = startDelay;

		inputManager = GetComponent<PlayerInputManager>();
	}

    // Update is called once per frame
    void Update()
    {
		var players =  Object.FindObjectsOfType<PlayerController>();

		int numDestroyers = 0;
		int numRepairers = 0;
		foreach(var p in players) {
			if(p.GetTeam()!=PlayerTeam.destroyer) {
				numDestroyers++;
			} else if(p.GetTeam()!=PlayerTeam.repairer) {
				numRepairers++;
			}
		}

		var ready = numDestroyers>0 && numRepairers>0 && numRepairers+numDestroyers == players.Length;
        
		if(ready) {
			statDelayLeft -= Time.deltaTime;
			if(statDelayLeft<=0) {
				// TODO: load level
				inputManager.DisableJoining();
			}

		} else {
			statDelayLeft = startDelay;
		}

		// TODO: update ui (countdown)
    }

}
