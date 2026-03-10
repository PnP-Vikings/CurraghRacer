using MiniGames.BalingSilageMinigame;
using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Material tileMaterial;
    public Material dirtMaterial;
    public Material collectedMaterial;
    public MeshRenderer tileRenderer;
    public bool highlight;
    public bool collected;

    void Start()
    {
        highlight = false;
        collected = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (highlight == true)
        {
            tileRenderer.material = dirtMaterial;
        }

        if (collected == true)
        {
            tileRenderer.material = collectedMaterial;
        }
    }

    public void Init(bool isOffset)
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (FindFirstObjectByType<BalingSilageMinigame>().cutting == true)
        {
            highlight = true;
        }

        if (FindFirstObjectByType<BalingSilageMinigame>().collecting == true && highlight == true)
        {
            collected = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        
    }
}
