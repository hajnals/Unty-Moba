using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMinionCtrl : LivingEntity {
    Rigidbody rb;
    Animator anim;
    StateManager sm;

    GameObject attackingTarget;
    List<Transform> waypoints;
    int waypointIndex;

    // Possible states
    IdleState idleState;
    Walking walkingState;
    Attacking attackingState;

    #region interfaces
    // Get/Set a target to attack by the aggro handler
    public GameObject AttackingTarget {
        get {
            return attackingTarget;
        }
        set {
            // Check target
            if (value == null) {
                Debug.Log($"{gameObject.name} Lost previous target!");
                // lost previous target
                // change state
                sm.ChangeToState(idleState);
                // clear target
                attackingTarget = value;
            }
            else if (attackingTarget == null) {
                Debug.Log($"{gameObject.name} New target given!");
                // we are not attacking anything at the moment
                // set target
                attackingTarget = value;
                // Set the destination to the targets location
                attackingState.AttackTarget = value;
                // change state to walking
                sm.ChangeToState(attackingState);
            }
        }
    }

    // Notify controller that it has arrived to waypoint by the walking class
    public void ArrivedAtDestination() {
        Debug.Log($"{gameObject.name} ArrivedAtDestination");
        // Check if it was attacking somebody
        if (AttackingTarget != null) {
            Debug.Log($"{gameObject.name}  At attacking target");
            // Arrived to attacking target's position
            attackingState.AttackTarget = attackingTarget;
            sm.ChangeToState(attackingState);
        }
        else {
            Debug.Log($"{gameObject.name}  At waypoint");
            // Check if have targets
            if (waypointIndex < (waypoints.Count - 1)) {
                // There is a next target
                waypointIndex++;
                // Set new target to walk towards
                walkingState.Target = waypoints[waypointIndex];
            }
            else {
                // No next target, wait here
                sm.ChangeToState(idleState);
            }
        }
    }

    // have nothing to do, look for task, called by idle state
    public void CheckForTask() {
        // Simply return to walking state for the latest waypoint.

        // Check if minion has places to be
        if (waypointIndex < waypoints.Count) {
            // go to waypoint
            walkingState.Target = waypoints[waypointIndex];
            sm.ChangeToState(walkingState);
        }

        // The minions should be always go to the last target
    }

    // Called when attacking target went out of range
    public void TargetIsOutOfRange() {
        // Move to target
        walkingState.Target = attackingTarget.transform;
        sm.ChangeToState(walkingState);
    }

    // Called when target dies
    public void OnTargetDeath() {
        // clear target
        AttackingTarget = null;
    }

    // Setting the destionation targets called by spawner
    public void SetWaypoints(List<Transform> targets) {
        // Set targets
        waypoints = targets;
    }

    #endregion interfaces

    void Awake() {
        // Get references
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        // Instantiate states
        idleState = new IdleState(gameObject);
        walkingState = new Walking(gameObject);
        attackingState = new Attacking(gameObject);
    }

    protected override void Start() {
        base.Start();
        attackingTarget = null;
        waypointIndex = 0;
        // start state manager, in idle state
        sm = new StateManager(idleState);
    }

    void Update() {
        // call the current states Update function
        sm.Update();
    }

    void FixedUpdate() {

        // call the current states FixedUpdate function
        sm.FixedUpdate();
    }
}

#region minion states

// knowing what to do in attacking state
public class Attacking : IState {
    Animator anim;  // reference to animator component
    GameObject obj; // reference to the game object of this script
    NewMinionCtrl ctrl; // reference to the minion controller
    GameObject attackTarget;    // target to attack

    #region parameters
    float damage = 10;  // attack damage
    float attackIntervall = 1;  // attack speed
    float attackRangeSqr = 2f;    // attack range squared value
    #endregion parameters

    float lastAttackTime = 0f;  // last attack's time

    #region interfaces
    public GameObject AttackTarget {
        set {
            if (value != null)
                attackTarget = value;
        }
    }
    #endregion interfaces


    public Attacking(GameObject obj) {
        this.obj = obj;
        ctrl = this.obj.GetComponent<NewMinionCtrl>();
        anim = this.obj.GetComponent<Animator>();
        // by default we dont have a target
        attackTarget = null;
    }

    public void Enter() {
        Debug.Log($"{obj.name} Entered attacking state.");
        // subscribe to targets OnDeath event
        attackTarget.GetComponent<LivingEntity>().OnDeath += ctrl.OnTargetDeath;
    }

    public void Exit() {
        Debug.Log($"{obj.name} Exited attacking state.");
        // stop attack animation
        anim.SetBool("isAttacking", false);
        // unsubscribe from targets OnDeath event
        attackTarget.GetComponent<LivingEntity>().OnDeath -= ctrl.OnTargetDeath;
        // clear target
        attackTarget = null;
    }

    public void FixedUpdate() {
        // check if target not null
        if (attackTarget != null) {
            // Check if target is in range, it could run out of range
            if (!TargetIsInRange()) {
                // Target is out of range
                Debug.Log($"{obj.name} Target out of range!");
                // Move to target
                ctrl.TargetIsOutOfRange();
            }
            else {
                // Target in range
                Debug.Log($"{obj.name} Target in range!");
                anim.SetBool("isAttacking", true);
                // check if we can attack
                if (Time.time > lastAttackTime + attackIntervall) {
                    // we can attack
                    lastAttackTime = Time.time;
                    // Get enemys IDamagable interface
                    IDamagable enemy = attackTarget.GetComponent<IDamagable>();
                    // Deal damage to enemy
                    enemy.TakeDamage(damage, obj);
                }
                else {
                    // attack is on cool down, wait
                }
            }
        }
        else {
            // dont have a proper target
            // TODO go back to idle
            Debug.LogWarning($"{obj.name} Lost target unexcpectedly!");
        }

    }

    public void Update() {
        // do nothing
    }

    #region private functions
    bool TargetIsInRange() {
        return Vector3.SqrMagnitude(attackTarget.transform.position - obj.transform.position) 
               <= (attackRangeSqr + obj.transform.lossyScale.x);
    }
    #endregion private functions
}

// knowing what to do in walking state
public class Walking : IState {
    GameObject obj; // The gameobject to which the script is attached to
    NewMinionCtrl ctrl; // reference to the minion controller
    Rigidbody rb;
    Animator anim;  // reference to animator component
    Transform target;   // The target it should walk towards to

    #region parameters
    readonly float moveSpeed = 6f;
    readonly float rotSpeed = 360f;
    readonly float arrivalOffsetSqr = 2;    // offset allowed to target
    #endregion parameters

    #region interface
    public Transform Target {
        get {
            return target;
        }
        set {
            if (value != null)
                target = value;
        }
    }
    #endregion interface

    public Walking(GameObject obj) {
        this.obj = obj;
        // set references
        ctrl = this.obj.GetComponent<NewMinionCtrl>();
        rb   = this.obj.GetComponent<Rigidbody>();
        anim = this.obj.GetComponent<Animator>();
        // Set the initial target to null
        target = null;
    }

    public void Enter() {
        Debug.Log($"{obj.name} Entered walking state.");
        // Update animation
        anim.SetBool("isWalking", true);
    }

    public void Exit() {
        Debug.Log($"{obj.name} Exited walking state.");
        // Update animation
        anim.SetBool("isWalking", false);
    }

    public void FixedUpdate() {
        if (target != null) {
            // check if arrived
            if (HasArrived(target.position)) {
                // arrived, notify minion controller
                ctrl.ArrivedAtDestination();
            }
            else {
                // not arrived yet, move minion towards the target
                MoveToTarget(target);
            }
        }
        else {
            Debug.LogWarning($"{obj.name} Walking towards null target!");
        }
    }

    public void Update() {
        // Nothing to do here
    }

    #region private functions
    bool HasArrived(Vector3 target) {
        return Vector3.SqrMagnitude(obj.transform.position - target) 
            <= (obj.transform.lossyScale.x + arrivalOffsetSqr);
    }

    void MoveToTarget(Transform target) {
        // Handle position
        rb.MovePosition(Vector3.MoveTowards(obj.transform.position,
                                            target.position,
                                            moveSpeed * Time.fixedDeltaTime));
        // Handle rotation
        rb.MoveRotation(Quaternion.RotateTowards(obj.transform.rotation,
                                                 Quaternion.LookRotation(target.position - obj.transform.position),
                                                 rotSpeed * Time.fixedDeltaTime));
    }
    #endregion private functions
}

// knowing what to do in idle state
public class IdleState : IState {
    Animator anim;  // reference to animator component
    GameObject obj; // reference to the game object of this script
    NewMinionCtrl ctrl; // reference to the minion controller

    public IdleState(GameObject obj) {
        this.obj = obj;
        ctrl = this.obj.GetComponent<NewMinionCtrl>();
        anim = this.obj.GetComponent<Animator>();
    }

    public void Enter() {
        Debug.Log($"{obj.name} Entered idle state.");
        // Update animation, this is the default animation
        anim.SetBool("isIdle", true);
    }

    public void Exit() {
        Debug.Log($"{obj.name} Existed idle state.");
        // Update animation, this is the default animation
        anim.SetBool("isIdle", true);
    }

    public void FixedUpdate() {
        ctrl.CheckForTask();
    }

    public void Update() {
        // dont do anything here in the idle animation
    }
}

#endregion minion states
