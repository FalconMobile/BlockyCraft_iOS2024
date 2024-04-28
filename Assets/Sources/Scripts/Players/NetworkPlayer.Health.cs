using UnityEngine;
using Mirror;
using VoxelPlay;
using UnityEngine.UI;


/// <summary>
/// Main player script, contains most methods needed for player health management
/// </summary>
public partial class NetworkPlayer : NetworkBehaviour
{

    // Health
    [Header("Health")]
    public int maxHealth = 200;
    [SyncVar(hook = nameof(OnHealthChanged))] int health = 200;
    public Color screenFXDamageColor = Color.red;
    public Color screenFXHealthColor = Color.green;

    public bool isAlive => health > 0;

    public float GetHealthPercentage() { return Mathf.Clamp01((float)health / maxHealth); }

    Text healthDisplay;

    // Hit fx
    Image damageIndicator;
    bool showingDamageEffect;
    float damageTimer;
    Color tempDamageColor = Color.white;
    Material damageMaterial;
    
    void ResetHealth()
    {
        health = maxHealth;
    }

    void OnHealthChanged(int previousHealth, int newHealth)
    {
        if (healthDisplay == null) return;

        if (newHealth < previousHealth && newHealth < maxHealth)
        {
            GotHit();
        }
        else
        {
            GotHealth();
        }
        healthDisplay.text = health.ToString();
    }


    // Client effect, triggered when health is reduced
    public void GotHit()
    {
        if (health > 0)
        {
            damageIndicator.enabled = true;
            damageTimer = 1;
            tempDamageColor = screenFXDamageColor;
            damageMaterial.SetColor("_Color", tempDamageColor);
            showingDamageEffect = true;
        }
    }

    // Client effect, triggered when health is restored
    public void GotHealth()
    {
        if (health > 0)
        {
            damageIndicator.enabled = true;
            damageTimer = 1;
            tempDamageColor = screenFXHealthColor;
            damageMaterial.SetColor("_Color", tempDamageColor);
            showingDamageEffect = true;
        }
    }

    // This player gets damage
    [Server]
    public void ReceiveDamage(ILivingEntity attacker, int damage)
    {
        health -= damage;
    }

    // Respawns the character on the beach
    public void RespawnCharacter()
    {
        // notify the server so health and score, which are syncvars, are reset
        CmdRespawn();

        // in order to move the character transform, we need to disable the character controller first
        CharacterController cc = GetComponent<CharacterController>();
        cc.enabled = false;
        WorldState.Instance.PlacePlayerOnStartPosition(transform);
        cc.enabled = true;
        VPFPP.enabled = true;
    }

    /// <summary>
    /// Called when Respawn Timer reaches zero on Client
    /// </summary>
    void CmdRespawn()
    {
        ResetHealth();
        ResetScore();
    }

    [Command]
    void UsePotion(string itemPotionName)
    {
        ItemDefinition item = VoxelPlayEnvironment.GetItemDefinition(itemPotionName);
        if (item != null)
        {
            int newHealth = health + item.GetPropertyValue<int>("healthPoints");
            if (newHealth > maxHealth)
            {
                newHealth = maxHealth;
            }
            health = newHealth;
        }
    }

}
