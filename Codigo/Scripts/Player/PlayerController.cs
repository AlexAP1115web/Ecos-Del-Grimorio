using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento de Lira: caminar, saltar y voltear el sprite.
// Usa el Input System nuevo porque el proyecto de Unity 6 viene configurado así.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 13f;
    [Tooltip("Si suelta el botón antes, el salto se corta (salto variable)")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Detección de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float horizontalInput;
    private bool jumpRequested;
    private bool isGrounded;
    private bool controlsEnabled = true;
    private float jumpMultiplier = 1f;
    private float slowMultiplier = 1f;
    private float slowUntil;
    private Vector2 externalVelocity;
    private MovingPlatform currentPlatform;

    public bool FacingRight { get; private set; } = true;
    public bool IsGrounded => isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        horizontalInput = 0f;
        var kb = Keyboard.current;
        if (!controlsEnabled || kb == null) return;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontalInput -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontalInput += 1f;

        if ((kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) && isGrounded)
            jumpRequested = true;

        // Salto variable: si suelta la tecla mientras sube, se corta
        bool jumpReleased = kb.spaceKey.wasReleasedThisFrame || kb.wKey.wasReleasedThisFrame || kb.upArrowKey.wasReleasedThisFrame;
        if (jumpReleased && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        if (horizontalInput > 0f) FacingRight = true;
        else if (horizontalInput < 0f) FacingRight = false;
        if (spriteRenderer != null) spriteRenderer.flipX = !FacingRight;
    }

    void FixedUpdate()
    {
        CheckGround();

        float speed = moveSpeed * (Time.time < slowUntil ? slowMultiplier : 1f);
        float platformX = currentPlatform != null ? currentPlatform.Velocity.x : 0f;
        rb.linearVelocity = new Vector2(horizontalInput * speed + externalVelocity.x + platformX, rb.linearVelocity.y);

        // El empuje externo (ráfagas de viento) se va apagando
        externalVelocity = Vector2.MoveTowards(externalVelocity, Vector2.zero, 25f * Time.fixedDeltaTime);

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpMultiplier);
            jumpRequested = false;
            isGrounded = false;
        }
    }

    void CheckGround()
    {
        isGrounded = false;
        currentPlatform = null;
        Vector2 point = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        foreach (var col in Physics2D.OverlapCircleAll(point, groundCheckRadius))
        {
            if (col.CompareTag("Ground"))
            {
                isGrounded = true;
                currentPlatform = col.GetComponent<MovingPlatform>();
                return;
            }
        }
    }

    public void SetJumpMultiplier(float multiplier)
    {
        jumpMultiplier = multiplier;
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowUntil = Time.time + duration;
    }

    // Empuja a Lira (ráfagas de Threnody o de Elenora)
    public void Push(Vector2 velocity)
    {
        externalVelocity = new Vector2(velocity.x, 0f);
        if (velocity.y != 0f) rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocity.y);
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled) horizontalInput = 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
