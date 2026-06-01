using MiniGames.BalingSilageMinigame;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public class Tractor : MonoBehaviour
{
    private Vector3 mousePosition;
    public float moveSpeed;
    public GameObject collectorPrefab;


    void Start()
    {
        transform.Rotate(0, 90, 0);
        moveSpeed = 0.002f;
    }

    // Update is called once per frame
    void Update()
    {
        /* mousePosition = Mouse.current.position.ReadValue();
            mousePosition.z = 4;
            transform.position = Vector3.MoveTowards (transform.position, new Vector3 (Camera.main.ScreenToWorldPoint (mousePosition).x, Camera.main.ScreenToWorldPoint(mousePosition).y, -0.5f), moveSpeed * Time.deltaTime);
        */
        transform.position += transform.forward * moveSpeed;

        if (FindFirstObjectByType<BalingSilageMinigame>().gameTimer <= 0)
        {
            moveSpeed = 0;
        }
        
        if (Keyboard.current.aKey.isPressed)
        {
            transform.Rotate(transform.rotation.x - 1, 0, 0);
        }

        if (Keyboard.current.dKey.isPressed)
        {
            transform.Rotate(transform.rotation.x + 1, 0, 0);
        }
    }

    public void Init(bool isOffset)
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (FindFirstObjectByType<BalingSilageMinigame>().Collector == other.gameObject && FindFirstObjectByType<BalingSilageMinigame>().cutting == true)
        {
            Destroy(FindFirstObjectByType<BalingSilageMinigame>().Collector);
            FindFirstObjectByType<BalingSilageMinigame>().collecting = true;
            FindFirstObjectByType<BalingSilageMinigame>().cutting = false;
            collectorPrefab.SetActive(true);
        }
    }
}
