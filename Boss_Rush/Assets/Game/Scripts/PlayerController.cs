using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 8f;
    private float moveInput;

    [Header("Pulo")]
    public float jumpForce = 12f;
    public int maxJumps = 2;
    private int jumpsRemaining;

    [Header("Coyote Time")]
    public float coyoteTime = 0.15f;
    private float coyoteCounter;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    [Header("Dash")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private float lastDashTime;
    private bool isDashing;

    [Header("Wall")]
    public float wallSlideSpeed = 2f;
    public float wallJumpForce = 12f;
    public Vector2 wallJumpDirection = new Vector2(1, 1.5f);
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool wallJumping;

    [Header("Ataque")]
    public float attackCooldown = 0.3f;
    private float lastAttackTime;

    [Header("Ataque Pesado")]
    public float heavyChargeTime = 1f;
    private bool isCharging;
    private float chargeStart;

    private Rigidbody2D rb;
    private bool isGrounded;

    private PlayerInputActions input;

    void Awake()
    {
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Enable();

        input.Player.Jump.performed += ctx => OnJumpPressed();
        input.Player.Dash.performed += ctx => TryDash();
        input.Player.LightAtk.performed += ctx => LightAttack();

        input.Player.HeavyAtk.started += ctx => StartCharge();
        input.Player.HeavyAtk.canceled += ctx => ReleaseCharge();
    }

    void OnDisable()
    {
        input.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        moveInput = input.Player.Move.ReadValue<Vector2>().x;

        // Flip personagem
        if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);

        // COYOTE TIME
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        // JUMP BUFFER
        if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;

        // WALL SLIDE
if (isTouchingWall && !isGrounded && !wallJumping)
{
    // Só desliza se estiver indo em direção à parede
    if ((moveInput > 0 && transform.localScale.x > 0) ||
        (moveInput < 0 && transform.localScale.x < 0))
    {
        isWallSliding = true;
    }
    else
    {
        isWallSliding = false;
    }
}
else
{
    isWallSliding = false;
}

        // Executar pulo com buffer
        if (jumpBufferCounter > 0)
        {
            if (coyoteCounter > 0 || jumpsRemaining > 0)
            {
                Jump();
                jumpBufferCounter = 0;
            }
        }
    }

    void FixedUpdate()
    {
        if (!isDashing && !wallJumping)
        {
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
        }

        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    void OnJumpPressed()
    {
        jumpBufferCounter = jumpBufferTime;
    }

    void Jump()
    {
        // WALL JUMP
        if (isWallSliding)
        {
            wallJumping = true;
            isWallSliding = false;

            float direction = -transform.localScale.x;

            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(direction * wallJumpDirection.x, wallJumpDirection.y) * wallJumpForce, ForceMode2D.Impulse);

            Invoke(nameof(StopWallJump), 0.2f);
            return;
        }

        // PULO NORMAL / COYOTE
        if (coyoteCounter > 0 || jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            if (!isGrounded)
                jumpsRemaining--;

            coyoteCounter = 0;
        }
    }

    void StopWallJump()
    {
        wallJumping = false;
    }

    void TryDash()
    {
        if (Time.time < lastDashTime + dashCooldown) return;

        StartCoroutine(Dash());
    }

    IEnumerator Dash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        float direction = transform.localScale.x;
        rb.linearVelocity = new Vector2(direction * dashForce, 0f);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }

    void LightAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;
        Debug.Log("Ataque leve");
    }

    void StartCharge()
    {
        isCharging = true;
        chargeStart = Time.time;
    }

    void ReleaseCharge()
    {
        if (!isCharging) return;

        float chargeTime = Time.time - chargeStart;

        if (chargeTime >= heavyChargeTime)
            Debug.Log("Ataque pesado carregado");
        else
            Debug.Log("Ataque pesado fraco");

        isCharging = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            jumpsRemaining = maxJumps;
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = false;
        }
    }
}