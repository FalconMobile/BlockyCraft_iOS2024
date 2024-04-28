using System;
using System.Collections;
using System.Collections.Generic;
using TaskSystem;
using UnityEngine;
using AppodealAds.Unity.Common;
using AppodealAds.Unity.Api;
using JetBrains.Annotations;
using Telepathy;

namespace Scripts.Advertising
{
    public enum AdsCallPlaces
    {
        Undefined = 0,
        LoadingAd = 1,
        WillBeReborn = 2,
        NewGameAd = 3,
    }

    public sealed class AdvertisingViewer: IAppodealInitializationListener, IInterstitialAdListener, IRewardedVideoAdListener
    {
        
#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IPHONE
        public static string appKey = "ace33b3fdab9ed2446bb4b87aa916d98b95205e520b0d782";
#elif UNITY_ANDROID
        public static string appKey = "ace33b3fdab9ed2446bb4b87aa916d98b95205e520b0d782";
#elif UNITY_IPHONE
        public static string appKey = "ace33b3fdab9ed2446bb4b87aa916d98b95205e520b0d782";
#else
	    public static string appKey = "ace33b3fdab9ed2446bb4b87aa916d98b95205e520b0d782";
#endif
        
        public static AdvertisingViewer Instance => _instance ??= new AdvertisingViewer();
        private static AdvertisingViewer _instance;
        private Coroutine _skippedWait;

        // [CanBeNull] private Action _interstitialAction;
        // [CanBeNull] private Action _interstitialFailAction;
        // [CanBeNull] private Action _rewardedAction;
        // [CanBeNull] private Action _rewardedFailAction;

        public void ShowRewarded(Action successCallback, Action failCallback = null, AdsCallPlaces adsCallPlaces = AdsCallPlaces.Undefined)
        {
            if (Appodeal.show(Appodeal.REWARDED_VIDEO))
            {
                successCallback.Invoke();
            }
            else
            {
                failCallback?.Invoke();
            }
        }

        public void ShowSkipping(Action successCallback, AdsCallPlaces adsCallPlace = AdsCallPlaces.Undefined)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                successCallback?.Invoke();
                return;
            }

            if (Appodeal.show(Appodeal.INTERSTITIAL))
            {
                successCallback?.Invoke();
            }
            else
            {
                successCallback?.Invoke();
            }
        }
        
        public void Init()
        {
            if (Appodeal.isInitialized(Appodeal.INTERSTITIAL | Appodeal.REWARDED_VIDEO))
                return;
            
            Appodeal.disableLocationPermissionCheck();
            Appodeal.setChildDirectedTreatment(false);
            Appodeal.setInterstitialCallbacks(this);
            Appodeal.setRewardedVideoCallbacks(this);
            Appodeal.initialize(appKey, Appodeal.INTERSTITIAL | Appodeal.REWARDED_VIDEO, this);
        }

        public void onInitializationFinished(List<string> errors) { }
        public void onInterstitialLoaded(bool isPrecache) { }
        public void onInterstitialFailedToLoad() { }
        public void onInterstitialShowFailed()
        {
            // _interstitialAction?.Invoke();
        }
        public void onInterstitialShown() { }
        public void onInterstitialClosed()
        {
            // _interstitialAction?.Invoke();
        }
        public void onInterstitialClicked() { }
        public void onInterstitialExpired() { }
        
        public void onRewardedVideoLoaded(bool precache) { }
        public void onRewardedVideoFailedToLoad() { }
        public void onRewardedVideoShowFailed()
        {
            // _rewardedFailAction?.Invoke();
        }
        public void onRewardedVideoShown()  { }
        public void onRewardedVideoFinished(double amount, string name) { }
        public void onRewardedVideoClosed(bool finished)
        {
            // _rewardedAction?.Invoke();
        }
        public void onRewardedVideoExpired() { }
        public void onRewardedVideoClicked() { }
    }
}