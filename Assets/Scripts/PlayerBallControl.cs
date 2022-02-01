using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(ClientNetworkTransform))]
public class PlayerBallControl : NetworkBehaviour
{
    [SerializeField]
    private float speed = 2.5f;

    [SerializeField]
    private float flySpeed = 4.0f;

    [SerializeField]
    private Vector2 defaultInitialPlanePositionRange = new Vector2(-4, 4);

    private Rigidbody ballRigidbody;

    private Vector2 inputVec = new Vector2(); // 2D Vector for Input Axis (Horizontal->Left,Right;Vertical->Up,Down)
    private bool isFlying = false;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
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
        if(IsClient && IsOwner)
        {
            ClientInput();
        }
    }

    // Events called by the Input System
    public void OnMove(InputValue input)
    {
        inputVec = input.Get<Vector2>();
    }

    public void OnJump(InputValue input)
    {
        isFlying = input.isPressed;
    }

    // Client-side input
    // Apply a force to the ball (rigidbody) in different directions (world axis)
    private void ClientInput()
    {
        // Direction
        float horizontal = inputVec.x;
        float vertical = inputVec.y;

        if (vertical != 0)
            ballRigidbody.AddForce(vertical > 0 ? Vector3.forward * speed : Vector3.back * speed);
        if (horizontal != 0)
            ballRigidbody.AddForce(horizontal > 0 ? Vector3.right * speed : Vector3.left * speed);
        
        if (isFlying)
        {
            Logger.Instance.LogInfo("Fly!");
            ballRigidbody.AddForce(Vector3.up * flySpeed, ForceMode.Impulse);
            isFlying = false;
        }
    }
}
