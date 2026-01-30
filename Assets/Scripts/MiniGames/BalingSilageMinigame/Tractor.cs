using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Tractor : MonoBehaviour
{
    private Vector3 mousePosition;
    public float moveSpeed = 1;
    private float timeIn;
    public TextMeshPro timerAmount;


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        mousePosition = Mouse.current.position.ReadValue();
        mousePosition.z = 4;
        transform.position = Vector3.MoveTowards (transform.position, new Vector3 (Camera.main.ScreenToWorldPoint (mousePosition).x, Camera.main.ScreenToWorldPoint(mousePosition).y, -0.5f), moveSpeed * Time.deltaTime);
    }

    public void Init(bool isOffset)
    {
        
    }

    void OnMouseEnter()
    {
        
    }

    void OnMouseExit()
    {
        
    }
}
