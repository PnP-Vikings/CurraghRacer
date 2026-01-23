using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Tractor : MonoBehaviour
{
    private Vector3 mousePosition;
    public float moveSpeed = 0.1f;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        mousePosition = Mouse.current.position.ReadValue();
        mousePosition.z = 4;
        transform.position = Vector3.MoveTowards (transform.position, Camera.main.ScreenToWorldPoint (mousePosition), moveSpeed * Time.deltaTime);
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
