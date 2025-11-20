using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    InputSystem_Actions inputActions;
    [SerializeField] Camera mainCamera;
    [SerializeField] float speed;
    [SerializeField] Vector2 moveDirection;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        inputActions.Player.Zoom.performed += Zoom;
        inputActions.Player.Movement.performed += moving =>
        {
            moveDirection = moving.ReadValue<Vector2>();
        };
        inputActions.Player.Movement.canceled += x => moveDirection = Vector3.zero;

        mainCamera = GetComponent<Camera>();
    }

    #region Enable/Disable - inputSystem
    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
    #endregion

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Vector3 move = moveDirection.normalized * speed * Time.fixedDeltaTime;
        transform.position += move;
    }

    void Zoom(InputAction.CallbackContext c)
    {
        mainCamera.orthographicSize -= c.ReadValue<float>();
        if (mainCamera.orthographicSize <= 1)
        {
            mainCamera.orthographicSize = 1f;
        }
    }
}
