using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewAggroHandler : MonoBehaviour {
    GameObject obj;
    NewMinionCtrl minCtrl;
    SphereCollider aggroCollider;

    #region parameters
    #endregion parameters

    private void Awake() {
        minCtrl = GetComponentInParent<NewMinionCtrl>();
        obj = minCtrl.gameObject;
        aggroCollider = gameObject.GetComponent<SphereCollider>();
    }

    private void OnTriggerEnter(Collider other) {
        // Filter out the triggers, 
        // so only when the other is an enemy the minion controller gets updated.
        if (IsInEnemyTeam(other.gameObject)) {
            // Enemy object, attack it
            minCtrl.AttackingTarget = other.gameObject;
        }
        else {
            // not an enemy, don't care
        }
    }

    /// <summary>
    /// When an enemy leaves the aggo range, check if the enemy was the target,
    /// if it was the target, find a new target.
    /// The new target will be the closest enemy target within the collider.
    /// If there is no such a game object then set the target to null.
    /// </summary>
    /// <param name="other">The object that left the collider</param>
    private void OnTriggerExit(Collider other) {
        // Check if the gameobject that left is in the opposite team, and is the target of the minion
        if (    (IsInEnemyTeam(other.gameObject))
             && (minCtrl.AttackingTarget == other.gameObject)
           ) {
            // The target left the aggro range

            // Get the closes enemy game object in aggro range, 
            // and assign it as new target, 
            // or if there is no such a thing clear target

            Collider[] colliders = Physics.OverlapSphere(transform.position, aggroCollider.radius);

            // Filter out the colliders which are not enemy attackables in a new list
            List<Collider> filteredColliders = new List<Collider>();
            for (int indexColl = 0; indexColl < colliders.Length; indexColl++) {
                if (IsInEnemyTeam(colliders[indexColl].gameObject)) {
                    filteredColliders.Add(colliders[indexColl]);
                }
            }

            // Check if found any collider
            if (filteredColliders.Count != 0) {
                // The closes collider so far
                Collider closestColl = filteredColliders[0];
                // The closes distance so far
                float closestDistSqr = Vector3.SqrMagnitude(transform.position - filteredColliders[0].transform.position);
                // Get the closest collider to the center of the aggro
                for (int indexColl = 1; indexColl < filteredColliders.Count; indexColl++) {
                    // Get the distance of filteredColliders[indexColl] collider
                    float currDistSqr = Vector3.SqrMagnitude(transform.position - filteredColliders[indexColl].transform.position);
                    // Check if this collider is closer 
                    if (currDistSqr < closestDistSqr) {
                        closestDistSqr = currDistSqr;
                        closestColl = filteredColliders[indexColl];
                    }
                }
                // Set the closes as the attacking target
                minCtrl.AttackingTarget = closestColl.gameObject;
            }
            else {
                // No filtered colliders were found, clear target
                minCtrl.AttackingTarget = null;
            }
        }
    }

    // Tells if the other game object is in the enemy team or not
    private bool IsInEnemyTeam(GameObject other) {
        return ((obj.tag == "BlueTeam") && (other.tag == "RedTeam")) || ((obj.tag == "RedTeam") && (other.tag == "BlueTeam"));
    }
}
