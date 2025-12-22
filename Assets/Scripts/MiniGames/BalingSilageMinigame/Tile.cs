using UnityEngine;

public class Tile : MonoBehaviour
{
    public Material tileMaterial;
    public Material dirtMaterial;
    public MeshRenderer tileRenderer;
    public bool highlight;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (highlight == true)
        {
            tileRenderer.material = dirtMaterial;
        }

        if (highlight == false)
        {
            tileRenderer.material = tileMaterial;
        }
    }

    public void Init(bool isOffset)
    {
        
    }

    void OnMouseEnter()
    {
        highlight = true;
    }

    void OnMouseExit()
    {
        highlight = false;
    }
}
