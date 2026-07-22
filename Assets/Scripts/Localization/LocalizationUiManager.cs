using System;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocalizationUiManager : MonoBehaviour
{
   public Transform languageUiParent;
   public languageUiElement languageUiPrefab;
  
  
   private void OnEnable()
   {
      for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
      {
         var locale = LocalizationSettings.AvailableLocales.Locales[i];
         var languageUi = Instantiate(languageUiPrefab, languageUiParent);
         languageUi.SetLanguageName(locale.name);
         languageUi.localeID = i;
      }  
   }
   
   private void OnDisable()
   {
      foreach (Transform child in languageUiParent)
      {
         Destroy(child.gameObject);
      }
   }
}
