using MiniGames.BalingSilageMinigame;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public class Tractor : MonoBehaviour
{
    private Vector3 mousePosition;
    public float moveSpeed = 1;
    public GameObject collectorPrefab;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        mousePosition = Mouse.current.position.ReadValue();
           mousePosition.z = 4;
           transform.position = Vector3.MoveTowards (transform.position, new Vector3 (Camera.main.ScreenToWorldPoint (mousePosition).x, Camera.main.ScreenToWorldPoint(mousePosition).y, -0.5f), moveSpeed * Time.deltaTime);
           if (FindFirstObjectByType<BalingSilageMinigame>().collecting == true)
           {
               collectorPrefab.SetActive(true);
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
        }
    }
}
