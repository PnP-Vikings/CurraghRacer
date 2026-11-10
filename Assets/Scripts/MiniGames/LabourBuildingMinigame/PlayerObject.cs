using MiniGames.LabourBuildingMinigame;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UIElements;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public class PlayerObject : MonoBehaviour
{

    Vector3 newPosition;

    [SerializeField]
    private InputAction mouseClick;

    private Camera mainCamera;
    private Coroutine coroutine;
    public float playerSpeed = 10f;
    public float rotationSpeed = 3f;
    private CharacterController characterController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        mouseClick.Enable();
        mouseClick.performed += Move;
    }

    private void OnDisable()
    {
        mouseClick.performed -= Move;
        mouseClick.Disable();
    }

    void Move(InputAction.CallbackContext context)
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray: ray, hitInfo: out RaycastHit hit) && hit.collider)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            
            coroutine = StartCoroutine(PlayerMove(hit.point));
            newPosition = hit.point;
        }
    }

    private IEnumerator PlayerMove (Vector3 target)
    {
        float distanceToFloor = transform.position.y - target.y;
        target.y += distanceToFloor;
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            Vector3 destination = Vector3.MoveTowards(transform.position, target, playerSpeed * Time.deltaTime);
            //transform.position = destination;
            Vector3 direction = target - transform.position;
            Vector3 movement = direction.normalized * playerSpeed * Time.deltaTime;
            characterController.Move(movement);

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), rotationSpeed * Time.deltaTime);

            yield return null;
        }
    }
}
