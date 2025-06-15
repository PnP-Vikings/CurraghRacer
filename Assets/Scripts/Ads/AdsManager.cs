using UnityEngine;

public class AdsManager : MonoBehaviour
{
   public InterstitialAds interstitialAds;
   public BannerAds bannerAds;
   public RewardedAds rewardedAds;
   public InitializeAds initializeAds;
   
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
       
       bannerAds.LoadBannerAd();
       interstitialAds.LoadInterstitialAd();
       rewardedAds.LoadRewardedAd();
   }
}
