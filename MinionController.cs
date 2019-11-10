using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MinionController : MonoBehaviour {
    
    #region Parameters
    // The speed of the minion
    public float moveSpeed = 3f;
    // The rotation speed
    public float rotSpeed = 3f;
    // The aggro range of the minion
    public float aggroRange = 3f;
    // The attack range of the minion
    public float attackRangeSqr = 1.2f;
    // The attack speed of the minion, means it will attack at every 1 secound
    public float attackIntervall = 1;
    // The minions damage
    public float damage = 10;
    // The healt of the minion
    [SerializeField]
    private float healt = 100;
    // The value in gold
    [SerializeField]
    private int goldVal = 20;
    #endregion Parameters

    #region Private members
    // The targets of this minion
    private List<Transform> waypoints = new List<Transform>();
    // The current target index of the minion
    private UInt16 waypointIndex;
    // The rigidbody component
    private Rigidbody rb;
    // The animator component
    private Animator anim;
    // The minion state
    private MinionState mState;
    // The target it wants to attack
    private GameObject attackTarget;
    // AttackAnimation coroutine
    IEnumerator DealDamage;
    // The one who killed this minion
    private GameObject destroyer;
    #endregion Private members

    public GameObject AttackTarget {
        get {
            return attackTarget;
        }
        // From the AggroRange script
        set {
            if(value == null) {
                // The target left the aggro range, or died
                attackTarget = null;
                // Change back to onRoute state
                mState = MinionState.Walking;
                // Stop coroutine
                StopCoroutine(DealDamage);
                // Update animation
                anim.SetBool("isAttacking", false);
            }
            // Check if we are not attacking already
            else if (mState != MinionState.Attacking) {
                // Give some information about whom to attack
                attackTarget = value;
                mState = MinionState.Attacking;
            }
        }
    }

    public float Healt { get => healt;}

    #region coroutines

    IEnumerator DealDamageCR() {
        Debug.Log("Started Corotine");

        while (AttackTarget != null) {
            Debug.Log("Attack");
            // Deal damage to enemy target
            AttackTarget.GetComponent<MinionController>().GettingDamage(damage, gameObject);
            // Wait for attackIntervall secound before attacking again
            yield return new WaitForSeconds(attackIntervall);
        }
    }

    #endregion coroutines

    #region interfaces

    public void GettingDamage(float dmg, GameObject attacker) {
        healt -= dmg;

        if(healt < 0) {
            // Minion died
            mState = MinionState.Dead;
            destroyer = attacker;
        }
    }

    // Set the target of this minion, called by spawner.
    public void SetTarget(List<Transform> targets) {
        this.waypoints = targets;
    }

    // From an enemy source, could be tower, here or minion
    public void TakeDamage(int damage) {
        // Reduce the healt by the damage
        healt = healt - damage;
    }

    #endregion interfaces

    // Awake is called when the script instance is being loaded
    private void Awake() {
        // Get rigidbody component
        rb = GetComponent<Rigidbody>();
        // Get animator component
        anim = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start() {
        // Set initial values
        mState = MinionState.Start;
        attackTarget = null;
        DealDamage = DealDamageCR();
    }

    // Update is called once per frame
    void Update() {
        
    }

    // FixedUpdate is called within a fixed given amount of time
    void FixedUpdate() {
        ControllMinion();
    }

    private void ControllMinion() {
        // Minion state machine
        switch (mState) {
            case MinionState.Start:
                StartState();
                break;
            case MinionState.Walking:
                Walking(waypoints[waypointIndex]);
                break;
            case MinionState.Attacking:
                Attacking();
                break;
            case MinionState.AtTarget:
                AtTarget();
                break;
            case MinionState.Dead:
                Dead();
                break;
            case MinionState.Idle:
                Idle();
                break;
            default:
                break;
        }
    }

    #region General private functions

    private bool AttackEnded(GameObject target) {
        // Attack should end when the target dies or exists range.
        
        return true;
    }

    private bool InAttackRange() {
        // Get the distance from the attacking target
        return Vector3.SqrMagnitude(transform.position - attackTarget.transform.position) <= attackRangeSqr;
    }

    private bool HasArrived(Vector3 target, float offset = 5) {
        return Vector3.Distance(transform.position, target) <= transform.lossyScale.x + offset;
    }

    private void MoveToTarget(Transform target) {
        // Handle position
        rb.MovePosition(Vector3.MoveTowards(transform.position,
                                            target.position,
                                            moveSpeed * Time.fixedDeltaTime));
        // Handle rotation
        rb.MoveRotation(Quaternion.RotateTowards(transform.rotation,
                                                 Quaternion.LookRotation(target.position - transform.position),
                                                 rotSpeed * Time.fixedDeltaTime));
    }
    
    #endregion General private functions

    #region The state machine functions
    private void StartState() {
        // Select the first target from the list
        waypointIndex = 0;
        // Go to the next state the on route
        mState = MinionState.Walking;
    }

    private void Walking(Transform target) {
        // Update animation
        anim.SetBool("isWalking", true);
        // Move minion towards the target
        MoveToTarget(target);

        // Check if minion has arrived to the target
        if (HasArrived(target.position)) {
            // Arrived
            mState = MinionState.AtTarget;
            // Update animation
            anim.SetBool("isWalking", false);
        }
    }

    private void Attacking() {
        // Check if the target is in attack range
        if (InAttackRange()) {
            // Start a coroutine which will attack at every attackspeed sec
            StartCoroutine(DealDamage);
            
            // Update animation
            anim.SetBool("isWalking", false);
            anim.SetBool("isAttacking", true);
        }
        else {
            // not in attack range, move towards
            MoveToTarget(attackTarget.transform);
            // stop coroutine
            StopCoroutine(DealDamage);
            // update animation
            anim.SetBool("isWalking", true);
            anim.SetBool("isAttacking", false);
        }

        // When the target lefts the aggro collider either, 
        // becuase it left aggro range or died, 
        // we will be notified via AttackTarget property,
        // So we dont have to take care about that here.
    }

    private void AtTarget() {
        // Check if have targets
        if (waypointIndex < (waypoints.Count - 1)) {
            // There is a next target
            waypointIndex++;
            mState = MinionState.Walking;
        }
        else {
            // No next target, wait here
            mState = MinionState.Idle;
        }
    }

    private void Dead() {
        HeroController hero = destroyer.GetComponent<HeroController>();
        if (hero != null) {
            // killed by hero, give hero gold
        }
        // self destroy
        Destroy(gameObject);
    }

    private void Idle() {
        // Update animation, this is the default so it makes,
        // no sense to set and clear this, but nevermind
        anim.SetBool("isIdle", true);
    }

    #endregion The state machine functions

    // Shows the state of the minion
    enum MinionState {
        Start,
        Idle,
        Walking,
        Attacking,
        Fighting,
        Dead,
        AtTarget
    }
}
