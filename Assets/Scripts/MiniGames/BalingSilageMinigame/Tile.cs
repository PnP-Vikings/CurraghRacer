using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Material tileMaterial;
    public Material dirtMaterial;
    public MeshRenderer tileRenderer;
    public bool highlight;
    private float timeIn;

    void Start()
    {
        timeIn = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (highlight == true)
        {
            if (timeIn < 5)
            {
                timeIn += Time.deltaTime;
            }

            if (timeIn > 5)
            {
                timeIn = 0;
                tileRenderer.material = dirtMaterial;
            }
        }

        if (highlight == false)
        {
            timeIn = 0;
        }
    }

    public void Init(bool isOffset)
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        highlight = true;
    }

    void OnTriggerExit(Collider other)
    {
        highlight = false;
    }
}
