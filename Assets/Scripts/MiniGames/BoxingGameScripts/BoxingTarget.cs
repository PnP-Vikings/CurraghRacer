using UnityEngine;
using UnityEngine.UI;

public class BoxingTarget : MonoBehaviour
{
    private Button button;
    [HideInInspector] public int spawnPointIndex = -1; // Track which spawn point this target is using
    [HideInInspector] public Coroutine fadeCoroutine; // Track the fade coroutine so we can stop it when hit

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("BoxingTarget needs a Button component!");
            return;
        }
        
        button.onClick.AddListener(OnTargetClicked);
    }

    private void OnTargetClicked()
    {
        // Notify the game manager that this target was hit
        if (BoxingMinigameManager.Instance != null)
        {
            BoxingMinigameManager.Instance.TargetHit(this);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnTargetClicked);
        }
    }
}
