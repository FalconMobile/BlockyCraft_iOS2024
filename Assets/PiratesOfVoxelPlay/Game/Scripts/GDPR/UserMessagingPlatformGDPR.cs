#if UNITY_ANDROID
using GoogleMobileAds.Ump.Api;
#endif
using UnityEngine;

public class UserMessagingPlatformGDPR : MonoBehaviour
{
#if UNITY_ANDROID
    void Start()
    {
        var debugSettings = new ConsentDebugSettings
        {
            DebugGeography = DebugGeography.EEA,
        };

        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            //ConsentDebugSettings = debugSettings,
        };
        
        try
        {
            ConsentInformation.Update(request, OnConsentInfoUpdated);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    void OnConsentInfoUpdated(FormError consentError)
    {
        if (consentError is not null)
        {
            Debug.LogError(consentError);
            return;
        }
        
        try
        {
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError is not null)
                {
                    Debug.LogError(consentError.Message);
                    return;
                }
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
    }
#endif
}