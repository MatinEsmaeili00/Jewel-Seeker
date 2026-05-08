using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public bool isMoving = false;

    
    public InputActionReference move;
    private PlayerCrouch playerCrouch;
    private PlayerDash playerDash;
    private Vector2 moveInput;
    
    public Rigidbody2D rb;
    Animator anim;
    private Vector2 lastMoveDir;

    void Start()
    {
        playerCrouch = GetComponent<PlayerCrouch>();
        playerDash = GetComponent<PlayerDash>();
        anim = GetComponent<Animator>();
    }
    private void OnEnable()
    {
        move.action.Enable();
    }
    private void OnDisable()
    {
        move.action.Disable();
    }
    void Update()
    {
        moveInput = move.action.ReadValue<Vector2>();
        moveInput = moveInput.normalized;

        ProcessInputs();
        Animate();
    } 
    void FixedUpdate()
    {
        if(playerDash.isDashing)
        {
            return;
        }
        if(moveInput == Vector2.zero)
        {
            isMoving = false;
        }
        else isMoving = true;
        if(playerCrouch.crouchInput.action.IsPressed())
        {
            rb.linearVelocity = moveInput * playerCrouch.crouchSpeed;
        }
        else { rb.linearVelocity = moveInput * speed; }
    }

    void ProcessInputs()
    {
        if(moveInput != Vector2.zero)
        {
            lastMoveDir = moveInput;
        }
    }

    void Animate()
    {
        anim.SetFloat("MoveX", moveInput.x);
        anim.SetFloat("MoveY", moveInput.y);
        anim.SetFloat("MoveMagnitude", moveInput.magnitude);
        anim.SetFloat("LastMoveX", lastMoveDir.x);
        anim.SetFloat("LastMoveY", lastMoveDir.y);
    }

}