using UnityEngine;
using Mirror;
using VoxelPlay;
using UnityEngine.UI;


/// <summary>
/// Main player script, contains most methods needed for additional GUI info
/// </summary>
public partial class NetworkPlayer : NetworkBehaviour
{
    private const int VISIBLE_LIFEBAR_FRAMES = 12;

    [Header("Indicators")]
    public float indicatorVisibilityDuration = 3f;
    public float indicatorLifebarChangeDuration = 0.5f;

    ILivingEntity indicatorTarget;
    float indicatorShowLifebarTime;
    float indicatorPrevValue = 1f;
    float indicatorCurrentValue = 1f;
    float indicatorChangeStartTime;

    private int hitFrame;

    /// <summary>
    /// Called from WorldState at server to activate the lifebar indicator on the player screen
    /// </summary>
    [ClientRpc]
    public void RpcShowTargetLifebarIndicator(Transform targetIndicator, float multiplier)
    {
        if (targetIndicator == null) return;

        ILivingEntity target = targetIndicator.GetComponent<ILivingEntity>();

        UpdateLifebarIndicatorValues(target, multiplier);
    }

    /// <summary>
    /// Updates lifebar transition start and end value
    /// </summary>
    void UpdateLifebarIndicatorValues(ILivingEntity target, float multiplier)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        // Time when the life bar becomes visible
        indicatorShowLifebarTime = Time.time;
        hitFrame = Time.frameCount;

        // Annotate the target getting damage
        if (target != indicatorTarget)
        {
            indicatorPrevValue = target.GetHealthPercentage();
            indicatorTarget = target;
        }

        indicatorCurrentValue = target.GetHealthPercentage();
        if (indicatorCurrentValue <= 0)
        {
            // if target is dead, make the lifebar disappear earlier
            indicatorShowLifebarTime -= (indicatorVisibilityDuration - 0.1f);
        }

        targetNameText.text = target.GetScreenName();
        headshotTextGO.SetActive(multiplier > 1);

        // Anotate the start of the change transition
        indicatorChangeStartTime = Time.time;
    }

    /// <summary>
    /// Called every frame to reflect indicator on screen
    /// </summary>
    public void ShowTargetDamageIndicator()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        float now = Time.time;

        if (indicatorTarget as Object == null || now - indicatorShowLifebarTime > indicatorVisibilityDuration)
        {
            indicatorTarget = null;
            targetLifebarIndicatorPanel.SetActive(false);
            return;
        }

        if (indicatorCurrentValue != indicatorTarget.GetHealthPercentage())
        {
            UpdateLifebarIndicatorValues(indicatorTarget, .99f);
        }

        bool lifebarVisible = true;
        if (indicatorCurrentValue == 0)
        {
            lifebarVisible = ((Time.frameCount - hitFrame) % (VISIBLE_LIFEBAR_FRAMES * 2)) <= VISIBLE_LIFEBAR_FRAMES;
        }

        targetLifebarIndicatorPanel.SetActive(lifebarVisible);


        float t = Mathf.Clamp01((now - indicatorChangeStartTime) / indicatorLifebarChangeDuration);
        if (t >= 1f)
        {
            indicatorPrevValue = indicatorCurrentValue;
        }

        float perc = Mathf.Lerp(indicatorPrevValue, indicatorCurrentValue, t);
        targetLifebarAmountPanel.transform.localScale =
            new Vector3(perc, targetLifebarAmountPanel.transform.localScale.y, 1f);
    }


}