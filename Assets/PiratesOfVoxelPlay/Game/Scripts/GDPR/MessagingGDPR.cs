#if UNITY_ANDROID
using GoogleMobileAds.Ump.Api;
#endif
using UnityEngine;

internal sealed class MessagingGDPR : MonoBehaviour
{
    [SerializeField, Tooltip("Button to show the privacy options form.")]
    private GameObject _privacyButton;

    private void Start()
    {
#if UNITY_ANDROID
        _privacyButton.SetActive(ConvertPrivacyOptionsRequirementStatus());
#else
        _privacyButton.SetActive(false);
#endif
    }

#if UNITY_ANDROID
    public void ShowPrivacyOptionsForm()
    {
        Debug.Log("Showing privacy options form.");
       
        try
        {
            ConsentForm.ShowPrivacyOptionsForm((FormError showError) =>
            {
                if (showError is not null)
                {
                    Debug.LogError($"Error showing privacy options form with error: {showError.Message}");
                }

                if (_privacyButton is not null)
                {
                    _privacyButton.SetActive(ConvertPrivacyOptionsRequirementStatus());
                }
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private bool ConvertPrivacyOptionsRequirementStatus()
    {
        bool isActiveButton = false;
        try
        {
            isActiveButton = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        }
        catch { }

        return isActiveButton;
    }
#endif
}