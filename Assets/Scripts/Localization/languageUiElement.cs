using UnityEngine;
using UnityEngine.Localization.Settings;

public class languageUiElement : MonoBehaviour
{
    public TMPro.TMP_Text languageName;
    public int localeID;
    
    public void SetLanguageName(string languageName)
    {
        this.languageName.text = languageName;
    }
    
    public void SetLocale()
    {
        if(LocalizationManager.Instance == null|| LocalizationSettings.AvailableLocales.Locales.Count == 0|| localeID >= LocalizationSettings.AvailableLocales.Locales.Count)  return;
        
        LocalizationManager.Instance.ChangeLocale(localeID);
    }
}
