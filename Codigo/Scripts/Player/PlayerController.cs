using System;
using UnityEngine;

// Movimiento de Lira: correr con aceleración, salto con "coyote time" y búfer,
// caída más rápida, esquive con invulnerabilidad, agacharse (para pasar por
// pasadizos bajos y atacar a enemigos pequeños) y animación procedural
// (estirarse al saltar, aplastarse al caer, inclinarse al correr).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float groundAcceleration = 70f;
    [SerializeField] private float airAcceleration = 45f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 13.5f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBuffer = 0.12f;
    [SerializeField] private float fallGravityMultiplier = 1.6f;
    [SerializeField] private float maxFallSpeed = 18f;

    [Header("Esquive")]
    [SerializeField] private float dashSpeed = 17f;
    [SerializeField] private float dashTime = 0.18f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("Agacharse")]
    [SerializeField] private float crouchSpeed = 3.2f;
    [Tooltip("Altura del collider agachada (fracción de la altura de pie)")]
    [SerializeField] private float crouchHeight = 0.6f;

    [Header("Detección de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    public event Action Jumped;
    public event Action Landed;
    public event Action Dashed;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform visual;
    private Vector3 visualBaseScale;
    private Health health;

    private float horizontalInput;
    private bool isGrounded;
    private bool controlsEnabled = true;
    private float jumpMultiplier = 1f;
    private float slowMultiplier = 1f;
    private float slowUntil;
    private Vector2 externalVelocity;
    private MovingPlatform currentPlatform;
    private float baseGravity;

    private float lastGroundedTime = -10f;
    private float lastJumpPressedTime = -10f;
    private bool jumpCutPending;
    private float dashUntil;
    private float nextDashTime;
    private float dashDirection;
    private float nextGhostTime;
    private Vector2 squash = Vector2.one;
    private CapsuleCollider2D capsule;
    private Vector2 standSize, standOffset;
    private Vector3 visualBaseLocalPos;
    private float visualBottom;
    private float crouchAmount;
    private bool landedSoundReady;

    public bool FacingRight { get; private set; } = true;
    public bool IsGrounded => isGrounded;
    public bool IsDashing => Time.time < dashUntil;
    public bool IsCrouching { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        visual = spriteRenderer != null && spriteRenderer.transform != transform ? spriteRenderer.transform : null;
        if (visual != null)
        {
            visualBaseScale = visual.localScale;
            visualBaseLocalPos = visual.localPosition;
            if (spriteRenderer.sprite != null) visualBottom = spriteRenderer.sprite.bounds.min.y * visualBaseScale.y;
        }
        baseGravity = rb.gravityScale;
        capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            standSize = capsule.size;
            standOffset = capsule.offset;
        }
    }

    void Update()
    {
        horizontalInput = 0f;
        if (controlsEnabled && Time.timeScale > 0f)
        {
            horizontalInput = Controles.Horizontal;

            if (Controles.SaltarPresionado) lastJumpPressedTime = Time.time;
            if (!Controles.SaltarSostenido && rb.linearVelocity.y > 0f && !isGrounded) jumpCutPending = true;

            if (Controles.EsquivePresionado && Time.time >= nextDashTime) StartDash();
        }

        UpdateCrouch(controlsEnabled && Time.timeScale > 0f && Controles.Abajo && isGrounded);

        if (horizontalInput > 0.1f) FacingRight = true;
        else if (horizontalInput < -0.1f) FacingRight = false;
        if (spriteRenderer != null) spriteRenderer.flipX = !FacingRight;

        AnimateVisual();
    }

    void FixedUpdate()
    {
        bool wasGrounded = isGrounded;
        CheckGround();
        if (isGrounded) lastGroundedTime = Time.time;
        if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0.1f) OnLand();

        float platformX = currentPlatform != null ? currentPlatform.Velocity.x : 0f;

        if (IsDashing)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed + platformX, 0f);
            return;
        }

        // Correr con aceleración (se siente más suave que cambiar la velocidad de golpe)
        float speed = (IsCrouching ? crouchSpeed : moveSpeed) * (Time.time < slowUntil ? slowMultiplier : 1f);
        float target = horizontalInput * speed;
        float accel = isGrounded ? groundAcceleration : airAcceleration;
        float currentX = rb.linearVelocity.x - externalVelocity.x - platformX;
        float newX = Mathf.MoveTowards(currentX, target, accel * Time.fixedDeltaTime);
        float vy = rb.linearVelocity.y;

        // Salto con coyote time (unos milisegundos después de dejar el suelo) y búfer
        bool canJump = Time.time - lastGroundedTime <= coyoteTime && !(IsCrouching && CeilingBlocked());
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBuffer;
        if (canJump && wantsJump)
        {
            vy = jumpForce * jumpMultiplier;
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            isGrounded = false;
            jumpCutPending = false;
            if (IsCrouching) SetCrouch(false);
            squash = new Vector2(0.8f, 1.25f);
            AudioManager.Play(Sfx.Salto, 0.7f);
            Particula.Polvo((Vector2)transform.position + Vector2.down * 0.8f, new Color(0.9f, 0.85f, 0.75f, 0.6f));
            Jumped?.Invoke();
        }
        else if (jumpCutPending)
        {
            if (vy > 0f) vy *= jumpCutMultiplier;
            jumpCutPending = false;
        }

        rb.gravityScale = vy < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
        vy = Mathf.Max(vy, -maxFallSpeed);

        rb.linearVelocity = new Vector2(newX + externalVelocity.x + platformX, vy);
        externalVelocity = Vector2.MoveTowards(externalVelocity, Vector2.zero, 25f * Time.fixedDeltaTime);
    }

    void StartDash()
    {
        dashDirection = Mathf.Abs(horizontalInput) > 0.1f ? Mathf.Sign(horizontalInput) : (FacingRight ? 1f : -1f);
        dashUntil = Time.time + dashTime;
        nextDashTime = Time.time + dashCooldown;
        if (health != null) health.SetInvulnerable(dashTime + 0.05f);
        squash = new Vector2(1.3f, 0.8f);
        AudioManager.Play(Sfx.Esquive, 0.8f);
        Dashed?.Invoke();
    }

    // ---------- Agacharse ----------

    void UpdateCrouch(bool wantsCrouch)
    {
        if (capsule == null) return;
        if (wantsCrouch && !IsCrouching) SetCrouch(true);
        else if (!wantsCrouch && IsCrouching && !CeilingBlocked()) SetCrouch(false);
    }

    void SetCrouch(bool crouch)
    {
        IsCrouching = crouch;
        if (crouch)
        {
            float h = standSize.y * crouchHeight;
            capsule.size = new Vector2(standSize.x, h);
            capsule.offset = new Vector2(standOffset.x, standOffset.y - (standSize.y - h) * 0.5f);
        }
        else
        {
            capsule.size = standSize;
            capsule.offset = standOffset;
        }
    }

    // ¿Hay techo encima? (en un pasadizo bajo Lira no puede pararse)
    bool CeilingBlocked()
    {
        if (capsule == null) return false;
        Vector2 center = (Vector2)transform.position + standOffset + Vector2.up * standSize.y * 0.2f;
        Vector2 size = new Vector2(standSize.x * 0.8f, standSize.y * 0.55f);
        foreach (var col in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            if (col.isTrigger || col.attachedRigidbody == rb) continue;
            if (col.CompareTag("Enemy") || col.CompareTag("Player")) continue;
            return true;
        }
        return false;
    }

    void OnLand()
    {
        squash = new Vector2(1.25f, 0.75f);
        if (landedSoundReady) AudioManager.Play(Sfx.Aterrizaje, 0.5f);
        landedSoundReady = true;
        Particula.Polvo((Vector2)transform.position + Vector2.down * 0.8f, new Color(0.9f, 0.85f, 0.75f, 0.6f));
        Landed?.Invoke();
    }

    // Animación hecha por código porque el arte es una sola imagen por personaje
    void AnimateVisual()
    {
        if (visual == null) return;

        squash = Vector2.Lerp(squash, Vector2.one, 12f * Time.deltaTime);

        float run = isGrounded ? Mathf.Abs(rb.linearVelocity.x) / moveSpeed : 0f;
        float bob = run > 0.1f ? Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.06f * run : Mathf.Sin(Time.time * 2.5f) * 0.015f;
        float stretchY = !isGrounded ? Mathf.Clamp(rb.linearVelocity.y * 0.012f, -0.08f, 0.1f) : 0f;

        crouchAmount = Mathf.MoveTowards(crouchAmount, IsCrouching ? 1f : 0f, 10f * Time.deltaTime);
        float crouchY = Mathf.Lerp(1f, crouchHeight + 0.05f, crouchAmount);
        float crouchX = Mathf.Lerp(1f, 1.08f, crouchAmount);
        if (IsCrouching) bob *= 0.4f;

        float scaleY = squash.y * (1f + bob + stretchY) * crouchY;
        visual.localScale = new Vector3(
            visualBaseScale.x * squash.x * (1f - stretchY * 0.5f) * crouchX,
            visualBaseScale.y * scaleY,
            visualBaseScale.z);
        // Mantiene los pies en el suelo aunque el sprite se aplaste
        visual.localPosition = visualBaseLocalPos + Vector3.up * (visualBottom * (1f - scaleY));

        float tilt = IsDashing ? -12f * dashDirection : -6f * horizontalInput * (isGrounded ? 1f : 0.5f) * (IsCrouching ? 0.3f : 1f);
        visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(0f, 0f, tilt), 15f * Time.deltaTime);

        if (IsDashing && Time.time >= nextGhostTime)
        {
            nextGhostTime = Time.time + 0.03f;
            Particula.Fantasma(spriteRenderer, new Color(0.7f, 0.5f, 1f, 0.5f), 0.25f);
        }
    }

    void CheckGround()
    {
        isGrounded = false;
        currentPlatform = null;
        Vector2 point = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        foreach (var col in Physics2D.OverlapCircleAll(point, groundCheckRadius))
        {
            if (col.CompareTag("Ground") && !col.isTrigger)
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

    // Empuja a Lira (golpes de enemigos y ráfagas de Threnody o Elenora)
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
