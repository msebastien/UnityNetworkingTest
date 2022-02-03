using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class PlayerControlWithRaycast : NetworkBehaviour
{
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

    [SerializeField]
    private NetworkVariable<float> networkPlayerHealth = new NetworkVariable<float>(1000);

    // To blend punch animation and make some subtle variations
    [SerializeField]
    private NetworkVariable<float> networkPlayerPunchBlend = new NetworkVariable<float>();

    [SerializeField]
    private GameObject leftHand;

    [SerializeField]
    private GameObject rightHand;

    // Minimum distance for intersection with Ray Cast and detection of punch
    [SerializeField]
    private float minPunchDistance = 0.25f; 

    private Vector2 inputVec = new Vector2(); // 2D Vector for Input Axis (Horizontal->Left,Right;Vertical->Up,Down)

    // Client caching
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

            PlayerCameraFollow.Instance.FollowPlayer(transform.Find("PlayerCameraRoot"));
        }
    }

    private void Update()
    {
        if(IsClient && IsOwner)
        {
            ClientInput();
        }

        ClientMoveAndRotate();
        ClientVisuals();
    }

    private void FixedUpdate()
    {
        if(IsClient && IsOwner) // Local Player
        {
            CheckPunch(leftHand.transform, Vector3.up);
            CheckPunch(rightHand.transform, Vector3.down);
        }
    }

    private void CheckPunch(Transform hand, Vector3 aimDirection)
    {
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Player");

        if (Physics.Raycast(hand.position, hand.TransformDirection(aimDirection), out hit, minPunchDistance, layerMask))
        {
            Debug.Log("Did Hit");
            Debug.DrawRay(hand.position, hand.TransformDirection(aimDirection) * minPunchDistance, Color.green);

            var playerHit = hit.transform.GetComponent<NetworkObject>();
            if(playerHit != null)
            {
                Logger.Instance.LogInfo($"Player {playerHit.OwnerClientId} has been hit. {networkPlayerHealth.Value} HP left.");
                UpdateHealthServerRpc(1, playerHit.OwnerClientId);
            }
        }
        else
        {
            Debug.Log("Did not Hit");
            Debug.DrawRay(hand.position, hand.TransformDirection(aimDirection) * minPunchDistance, Color.red);
        }
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
            animator.SetTrigger("Walk");
        }
        else if(networkPlayerState.Value == PlayerState.ReverseWalk)
        {
            animator.SetTrigger("ReverseWalk");
        }
        else
        {
            animator.SetTrigger("Idle");
        }
    }

    [ServerRpc]
    public void UpdateHealthServerRpc(float health, ulong clientId)
    {
        NetworkObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        player.gameObject.GetComponent<PlayerControlWithRaycast>().networkPlayerHealth.Value -= 1;
    }

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
