using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : NetworkBehaviour
{
    public enum PlayerState
    {
        Idle,
        Walk,
        ReverseWalk
    }

    [SerializeField]
    private float speed = 2.5f;

    [SerializeField]
    private float rotationSpeed = 1.2f;

    [SerializeField]
    private Vector2 defaultInitialPlanePositionRange = new Vector2(-4, 4);

    [SerializeField]
    private NetworkVariable<Vector3> networkPositionDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<Vector3> networkRotationDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();

    //[SerializeField]
    //private NetworkVariable<float> forwardBackPosition = new NetworkVariable<float>(); // Z-Axis

    //[SerializeField]
    //private NetworkVariable<float> leftRightPosition = new NetworkVariable<float>(); // X-Axis

    private Vector2 inputVec = new Vector2(); // 2D Vector for Input Axis (Horizontal->Left,Right;Vertical->Up,Down)

    // Client caching
    //private float oldForwardBackPos;
    //private float oldLeftRightPos;
    private Vector3 oldInputPos;
    private Vector3 oldInputRotation;

    private CharacterController characterController;
    private Animator animator;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        GetComponent<PlayerInput>().SwitchCurrentControlScheme(controlScheme: "KeyboardMouse",
            devices: new InputDevice[] { Keyboard.current, Mouse.current });
    }

    private void Start()
    {
        if(IsClient && IsOwner)
        {
            transform.position = new Vector3(Random.Range(defaultInitialPlanePositionRange.x, defaultInitialPlanePositionRange.y), 0,
            Random.Range(defaultInitialPlanePositionRange.x, defaultInitialPlanePositionRange.y));
        }
    }

    private void Update()
    {
        /*if(IsServer)
        {
            UpdateServer(); // Apply new position to the player's transform server-side;
        }

        if(IsClient && IsOwner)
        {
            UpdateClient(); // Apply new position to the player's transform client-side;
        }*/
        if(IsClient && IsOwner)
        {
            ClientInput();
        }

        ClientMoveAndRotate();
        ClientVisuals();
    }

    // Event called by the Input System
    public void OnMove(InputValue input)
    {
        inputVec = input.Get<Vector2>();
    }

    // Client-side input
    private void ClientInput()
    {
        Vector3 inputRotation = new Vector3(0, inputVec.x, 0);

        // Forward vector (in front of the character) to world space
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        
        float forwardInput = inputVec.y; // Forward / Backward

        // Direction (where the forward unit vector points) multiplied by input vector gives new position
        Vector3 inputPosition = direction * forwardInput; 

        if(oldInputPos != inputPosition || oldInputRotation != inputRotation)
        {
            oldInputPos = inputPosition;
            oldInputRotation = inputRotation;

            // Send new pos to server
            UpdateClientPositionAndRotationServerRpc(inputPosition * speed, inputRotation * rotationSpeed);
        }

        // Player state changes based on input
        // Send new player state to server

        if (forwardInput > 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Walk);
        }
        else if(forwardInput < 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.ReverseWalk);
        }
        else
        {
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        }
    }

    private void ClientMoveAndRotate()
    {
        if(networkPositionDirection.Value != Vector3.zero)
        {
            characterController.SimpleMove(networkPositionDirection.Value);
        }

        if (networkRotationDirection.Value != Vector3.zero)
        {
            transform.Rotate(networkRotationDirection.Value);
        }
    }

    // Character animation
    private void ClientVisuals()
    {
        if(networkPlayerState.Value == PlayerState.Walk)
        {
            animator.SetFloat("Walk", 1);
        }
        else if(networkPlayerState.Value == PlayerState.ReverseWalk)
        {
            animator.SetFloat("Walk", -1);
        }
        else
        {
            animator.SetFloat("Walk", 0);
        }
    }

    /*private void UpdateServer()
    {
        transform.position = new Vector3(transform.position.x + leftRightPosition.Value, transform.position.y, 
            transform.position.z + forwardBackPosition.Value);
    }*/

    /*private void UpdateClient()
    {
        float forwardBackward = 0;
        float leftRight = 0;

        var keyboard = Keyboard.current;

        if(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            forwardBackward += speed;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            forwardBackward -= speed;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            leftRight -= speed;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            leftRight += speed;
        }

        /*if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            forwardBackward += walkSpeed;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            forwardBackward -= walkSpeed;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            leftRight -= walkSpeed;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            leftRight += walkSpeed;
        }*/

        /*if (oldForwardBackPos != forwardBackward || oldLeftRightPos != leftRight)
        {
            oldForwardBackPos = forwardBackward;
            oldLeftRightPos = leftRight;

            UpdateClientPositionServerRpc(forwardBackward, leftRight);
        }

    }*/

    /*[ServerRpc]
    public void UpdateClientPositionServerRpc(float forwardBackPos, float leftRightPos)
    {
        //forwardBackPosition.Value = forwardBackPos;
        //leftRightPosition.Value = leftRightPos;
        
    }*/

    [ServerRpc]
    public void UpdateClientPositionAndRotationServerRpc(Vector3 newPositionDirection, Vector3 newRotationDirection)
    {
        networkPositionDirection.Value = newPositionDirection;
        networkRotationDirection.Value = newRotationDirection;
    }

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState newPlayerState)
    {
        networkPlayerState.Value = newPlayerState;
    }
}
