using UnityEngine;

/// <summary>
/// Both players and mobs can receive damage, so they must implement this interface
/// </summary>
public interface ILivingEntity
{

    void ReceiveDamage(ILivingEntity attacker, int damage);
    bool isAlive { get; }
    float GetHealthPercentage();
    Transform GetTransform();
    void IncreaseScore(int amount);
    void Disengage(ILivingEntity target);
    string GetScreenName();
    int GetScoreByHit();
    int GetScoreByKill();
}
