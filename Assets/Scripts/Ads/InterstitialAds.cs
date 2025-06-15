using UnityEngine;
using UnityEngine.Advertisements;

public class InterstitialAds : MonoBehaviour,IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private string androidAdUnitId;
    [SerializeField] private string iosAdUnitId;
    
    private string adUnitId;
    
    private void Awake()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            adUnitId = iosAdUnitId;
        }
        else
        {
            adUnitId = androidAdUnitId;
        }
    }
    public void LoadInterstitialAd()
    {
       Advertisement.Load(adUnitId,this);
    }
    
    public void ShowInterstitialAd()
    {
        Advertisement.Show(adUnitId,this);
        LoadInterstitialAd();
    }
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"Interstitial ad loaded: {placementId}");
    }
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        PlayerStatsView.Instance.DisplayInfo("Ad failed to Load", 2f);
        RaceManager.Instance.waitingForAd = false; // Reset the flag after ad is shown
    }
    
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        PlayerStatsView.Instance.DisplayInfo("Ad failed to Show", 2f);
        RaceManager.Instance.waitingForAd = false; // Reset the flag after ad is shown
    }
    public void OnUnityAdsShowStart(string placementId)
    {
        PlayerStatsView.Instance.DisplayInfo("Ad is showing, please wait...", 2f);
    }
    public void OnUnityAdsShowClick(string placementId)
    {
        throw new System.NotImplementedException();
    }
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"Interstitial ad shown: {placementId}");
        RaceManager.Instance.waitingForAd = false; // Reset the flag after ad is shown
    }
}
