using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(ClientNetworkTransform))]
public class PlayerControlAuthorativeV2 : NetworkBehaviour
{
    [SerializeField]
    private float speed = 2.5f;

    [SerializeField]
    private float rotationSpeed = 1.2f;

    [SerializeField]
    private Vector2 defaultInitialPlanePositionRange = new Vector2(-4, 4);

    [SerializeField]
    private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();

    private Vector2 inputVec = new Vector2(); // 2D Vector for Input Axis (Horizontal->Left,Right;Vertical->Up,Down)
    private bool isRunning = false; // Player is sprinting? (Left Shift)
    private bool isPunching = false; // Player is punching? (E key)

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
        if(IsClient && IsOwner) // Local Player
        {
            transform.position = new Vector3(Random.Range(defaultInitialPlanePositionRange.x, defaultInitialPlanePositionRange.y), 0,
            Random.Range(defaultInitialPlanePositionRange.x, defaultInitialPlanePositionRange.y));

            PlayerCameraFollow.Instance.FollowPlayer(transform.Find("PlayerCameraRoot"));
        }
    }

    private void Update()
    {
        if(IsClient && IsOwner)
        {
            ClientInput();
        }

        ClientVisuals();
    }

    // Events called by the Input System
    public void OnMove(InputValue input)
    {
        inputVec = input.Get<Vector2>();
    }

    public void OnSprint(InputValue input)
    {
        isRunning = input.isPressed;
    }

    public void OnPunch(InputValue input)
    {
        isPunching = input.isPressed;
    }

    // Client-side input
    private void ClientInput()
    {
        Vector3 inputRotation = new Vector3(0, inputVec.x, 0);

        // Forward vector (in front of the character) to world space
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        
        float forwardInput = inputVec.y; // Forward / Backward
        if (isRunning && forwardInput > 0) forwardInput = 2; // Sprinting

        // Direction (where the forward unit vector points) multiplied by input vector gives new position
        Vector3 inputPosition = direction * forwardInput;

        // Move and rotate
        // Authorative -> Client is responsible for moving itself instead of server
        characterController.SimpleMove(inputPosition * speed);
        transform.Rotate(inputRotation * rotationSpeed);

        // Player state changes based on input
        // Send new player state to server

        if (isPunching)
        {
            UpdatePlayerStateServerRpc(PlayerState.Punching);
        }
        else if (forwardInput > 1)
        {
            UpdatePlayerStateServerRpc(PlayerState.Run);
        }
        else if (forwardInput > 0)
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

    // Character animation
    private void ClientVisuals()
    {
        switch(networkPlayerState.Value)
        {
            case PlayerState.Walk: 
                animator.SetTrigger("Walk"); break;

            case PlayerState.Run:
                animator.SetTrigger("Run"); break;

            case PlayerState.ReverseWalk:
                animator.SetTrigger("ReverseWalk"); break;

            case PlayerState.Punching:
                animator.SetTrigger("Punching"); break;

            default:
                animator.SetTrigger("Idle"); break;
        }
    }

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState newPlayerState)
    {
        networkPlayerState.Value = newPlayerState;
    }
}
