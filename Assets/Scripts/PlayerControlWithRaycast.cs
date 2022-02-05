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
    private float runSpeedOffset = 0.5f; // Added speed when running

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
    private bool isRunning = false; // Player is sprinting? (Left Shift)
    private bool isPunching = false; // Player is punching? (E key)

    // Client caching
    private Vector3 oldInputPos;
    private Vector3 oldInputRotation;
    private PlayerState oldPlayerState;

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
            if(networkPlayerState.Value == PlayerState.Punching && isPunching)
            {
                CheckPunch(leftHand.transform, Vector3.up);
                CheckPunch(rightHand.transform, Vector3.down);
            }
            
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
                // Deal damage to player being punched
                UpdateHealthServerRpc(1, playerHit.OwnerClientId);
                
                // Notify the client that is currently punching the other player
                float playerHitHealth = playerHit.gameObject.GetComponent<PlayerControlWithRaycast>().networkPlayerHealth.Value;
                Logger.Instance.LogInfo($"Player {playerHit.OwnerClientId} has been punched. {playerHitHealth} HP left.");
            }
        }
        else
        {
            Debug.Log("Did not Hit");
            Debug.DrawRay(hand.position, hand.TransformDirection(aimDirection) * minPunchDistance, Color.red);
        }
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

        // Direction (where the forward unit vector points) multiplied by input vector gives new position
        Vector3 inputPosition = direction * forwardInput; 

        // Change fighting states
        if(isPunching && forwardInput == 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Punching);
            return; // do not move player
        }

        // Change motion states
        if (forwardInput == 0)
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        else if(!isRunning && forwardInput > 0 && forwardInput <= 1)
            UpdatePlayerStateServerRpc(PlayerState.Walk);
        else if(isRunning && forwardInput > 0 && forwardInput <=1)
        {
            inputPosition = direction * runSpeedOffset;
            UpdatePlayerStateServerRpc(PlayerState.Run);
        }
        else if(forwardInput < 0)
            UpdatePlayerStateServerRpc(PlayerState.ReverseWalk);


        if (oldInputPos != inputPosition || oldInputRotation != inputRotation)
        {
            oldInputPos = inputPosition;
            oldInputRotation = inputRotation;

            // Send new pos to server
            UpdateClientPositionAndRotationServerRpc(inputPosition * speed, inputRotation * rotationSpeed);
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
        if(oldPlayerState != networkPlayerState.Value)
        {
            oldPlayerState = networkPlayerState.Value;
            animator.SetTrigger($"{networkPlayerState.Value}");
            if(networkPlayerState.Value == PlayerState.Punching)
            {
                animator.SetFloat("PunchBlend", networkPlayerPunchBlend.Value);
            }
        }
    }

    [ServerRpc]
    public void UpdateHealthServerRpc(int damage, ulong clientId)
    {
        var clientWithDamage = NetworkManager.Singleton.ConnectedClients[clientId]
            .PlayerObject.gameObject.GetComponent<PlayerControlWithRaycast>();

        if(clientWithDamage != null && clientWithDamage.networkPlayerHealth.Value > 0)
        {
            clientWithDamage.networkPlayerHealth.Value -= damage;
        }
            

        // Execute method on client getting punched
        NotifyHealthChangedClientRpc(damage, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] {clientId}
            }
        });
    }

    // Notify client being punched and hurt
    [ClientRpc]
    public void NotifyHealthChangedClientRpc(int damage, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;

        // Client-side logic
        Logger.Instance.LogInfo($"You got punched by Player {OwnerClientId} (-{damage} HP)");
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
        if(networkPlayerState.Value == PlayerState.Punching)
        {
            networkPlayerPunchBlend.Value = Random.Range(0.0f, 1.0f);        
        }
    }
}
