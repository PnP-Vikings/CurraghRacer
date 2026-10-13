using MiniGames.BalingSilageMinigame;
using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Material tileMaterial;
    public Material dirtMaterial;
    public Material collectedMaterial;
    public Material inactiveMaterial;
    public MeshRenderer tileRenderer;
    public bool highlight;
    public bool collected;
    public bool inactive;

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

        if (inactive == true)
        {
            tileRenderer.material = inactiveMaterial;
        }

    }

    public void Init(bool isOffset)
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (FindAnyObjectByType<BalingSilageMinigame>().collecting == false && FindAnyObjectByType<BalingSilageMinigame>().cutting == true && highlight == true)
        {
            highlight = false;
            inactive = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (FindAnyObjectByType<BalingSilageMinigame>().cutting == true && highlight == false)
        {
            highlight = true;
        }

        if (FindAnyObjectByType<BalingSilageMinigame>().collecting == true && highlight == true)
        {
            collected = true;
            highlight = false;
            FindAnyObjectByType<BalingSilageMinigame>().grassCounter++;
        }
    }
}
