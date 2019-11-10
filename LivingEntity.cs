using System.Collections.Generic;
using UnityEngine;

public class LivingEntity : MonoBehaviour, IDamagable
{
    [SerializeField]
    public float startingHealth;

    protected float health;
    protected bool isDead;

    #region events
    public event System.Action OnDeath;
    #endregion events

    protected virtual void Start() {
        health = startingHealth;
    }

    public virtual void TakeDamage(float damage, GameObject killer) {
        health -= damage;

        if ((health <= 0) && (!isDead)) {
            // Notify subscribers that this living entity died
            Die();
            // TODO give the killer gold if it was a player
        }
    }

    [ContextMenu("Self Destruct")]
    protected void Die() {
        isDead = true;

        //Check if anybody is subscribed
        if (OnDeath != null) {
            OnDeath();
        }
        Destroy(gameObject);
    }
}
