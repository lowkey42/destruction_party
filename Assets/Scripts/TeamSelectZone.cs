using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerTeam {
	destroyer, repairer, none
}

public class TeamSelectZone : MonoBehaviour
{
	public float range = 4;

	public PlayerTeam team;

    // Update is called once per frame
    void Update()
    {
        var inRange = Physics.OverlapSphere(transform.position, range);

		foreach(var entity in inRange) {
			var player = entity.GetComponent<PlayerController>();
			if(player!=null) {
				if(team==PlayerTeam.destroyer)
					player.SwitchToDestroyer();
				else
					player.SwitchToRepairer();
			}
		}
    }

	void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
	Gizmos.DrawSphere(transform.position, range);
#endif
	}
}
