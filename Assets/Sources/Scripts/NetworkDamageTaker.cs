using UnityEngine;

public class NetworkDamageTaker : MonoBehaviour
{
    /// <summary>
    /// This can be used to augment the damage based on certain parts of the body
    /// </summary>
    public float damageMultiplier = 1f;

    public ILivingEntity entity;

    // Get reference to the owner before any other script starts
    void Awake()
    {
        entity = transform.root.GetComponentInChildren<ILivingEntity>();
    }

    public void ApplyDamage(ILivingEntity attacker, int damage)
    {
        entity.ReceiveDamage(attacker, (int)(damage * damageMultiplier));
    }
}