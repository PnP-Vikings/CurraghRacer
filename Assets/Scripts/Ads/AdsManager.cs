using UnityEngine;

public class AdsManager : MonoBehaviour
{
   public InterstitialAds interstitialAds;
   public BannerAds bannerAds;
   public RewardedAds rewardedAds;
   public InitializeAds initializeAds;
   [Tooltip("Toggling this will disable ads.")]
   public bool disableAdsForTesting;
   
   public static AdsManager Instance { get; private set; }
   
   private void Awake()
   {
       if (Instance == null)
       {
           Instance = this;
           DontDestroyOnLoad(gameObject);
       }
       else
       {
           Destroy(gameObject);
       }

       if (disableAdsForTesting)
       {
           Debug.Log("Ads are disabled for testing.");
           return;
       }

       if (bannerAds != null && bannerAds.IsAdUnitIDSet())
       {
           bannerAds.LoadBannerAd();
       }
       if (interstitialAds != null && interstitialAds.IsAdUnitIDSet())
       {
           interstitialAds.LoadInterstitialAd();
       }
       if (rewardedAds != null && rewardedAds.IsAdUnitIDSet())
       {
           rewardedAds.LoadRewardedAd();
       }
   }
}
