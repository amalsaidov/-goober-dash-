using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 15f;

    [Header("Dash")]
    public float dashForce = 22f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 1.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Wall")]
    public float wallSlideSpeed = 2.5f;   // max fall speed while sliding on wall
    public float wallJumpX     = 10f;     // horizontal kick on wall jump
    public float wallCheckDist = 0.55f;   // raycast length for wall detection

    [Header("Feel")]
    public float coyoteTime    = 0.15f;
    public float jumpBufferTime = 0.12f;

    [HideInInspector] public bool canControl = false;
    [HideInInspector] public bool isBoosted  = false;

    // Hard physics limits — nothing may exceed these
    private const float MAX_H_SPEED = 24f;
    private const float MAX_FALL    = -26f;
    private const float MAX_RISE    = 22f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color _assignedColor = Color.white;  // color set by SceneSetup, theme-aware base
    private bool isGrounded, wasGrounded;
    public  bool IsGrounded => isGrounded;
    private float moveInput;
    private float coyoteTimer, jumpBufferTimer;
    private Vector3 originalScale;
    private float dashTimer, dashCooldownTimer;
    private bool isDashing;
    private Vector3 spawnPoint;
    private int jumpsRemaining = 2;

    // Wall
    private bool isWallLeft, isWallRight;
    private bool isWallSliding;
    private int  wallDir;               // -1 = left wall, +1 = right wall
    private float wallJumpLockTimer;    // briefly blocks horizontal input after wall jump

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        spawnPoint = transform.position;
        _assignedColor = sr.color;
    }

    // Returns the base color adjusted for the current world theme (B&W vs Standard)
    Color BaseColor()
    {
        if (WorldThemeManager.Instance != null)
            return WorldThemeManager.Instance.GetThemedColor(_assignedColor);
        return _assignedColor;
    }

    void Update()
    {
        if (!canControl) return;

        var keyboard = Keyboard.current; // null on iPad — touch fills in below

        // ── Dash ────────────────────────────────────────────────────────────
        dashCooldownTimer -= Time.deltaTime;
        bool dashPressed = (keyboard != null && keyboard.leftShiftKey.wasPressedThisFrame)
                        || TouchInput.dashDown;
        if (dashPressed && dashCooldownTimer <= 0 && !isDashing)
            StartDash();

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0) isDashing = false;
            return;
        }

        // ── Move ─────────────────────────────────────────────────────────────
        moveInput = 0f;
        if ((keyboard != null && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed))
            || TouchInput.moveLeft)  moveInput = -1f;
        if ((keyboard != null && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed))
            || TouchInput.moveRight) moveInput =  1f;

        // Lock horizontal control briefly after wall jump (so the player launches away)
        wallJumpLockTimer -= Time.deltaTime;
        if (wallJumpLockTimer > 0) moveInput = 0f;

        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;

        // ── Ground ───────────────────────────────────────────────────────────
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        // Reset jumps ONLY on landing (wasGrounded false→true).
        // Resetting every frame while isGrounded caused 3+ jumps: the
        // OverlapCircle still touches ground for 1-2 frames after launch,
        // so a fast second press re-granted a jump before the player left.
        if (!wasGrounded && isGrounded)
            jumpsRemaining = 2;

        // ── Wall detection ───────────────────────────────────────────────────
        // Cast from the SIDE EDGES of the player (not center) so the ray never
        // starts inside the wall. Two rays per side (upper/lower body) for
        // reliable detection on uneven surfaces.
        if (!isGrounded)
        {
            const float hw   = 0.42f;  // half-width of player collider
            const float cd   = 0.18f;  // short cast — we just need to confirm contact
            Vector2 top      = (Vector2)transform.position + Vector2.up * 0.25f;
            Vector2 bot      = (Vector2)transform.position - Vector2.up * 0.25f;
            isWallRight = Physics2D.Raycast(top + Vector2.right * hw, Vector2.right, cd, groundLayer)
                       || Physics2D.Raycast(bot + Vector2.right * hw, Vector2.right, cd, groundLayer);
            isWallLeft  = Physics2D.Raycast(top - Vector2.right * hw, Vector2.left,  cd, groundLayer)
                       || Physics2D.Raycast(bot - Vector2.right * hw, Vector2.left,  cd, groundLayer);
        }
        else
        {
            isWallRight = false;
            isWallLeft  = false;
        }

        // Sliding: touching wall + pressing into it + falling (any speed)
        isWallSliding = false;
        if (isWallRight && moveInput > 0 && rb.linearVelocity.y < 0.5f) { isWallSliding = true; wallDir =  1; }
        if (isWallLeft  && moveInput < 0 && rb.linearVelocity.y < 0.5f) { isWallSliding = true; wallDir = -1; }

        // ── Jump input (keyboard or touch) ───────────────────────────────────
        bool jumpPressed  = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) || TouchInput.jumpDown;
        bool jumpReleased = (keyboard != null && keyboard.spaceKey.wasReleasedThisFrame) || TouchInput.jumpUp;

        // ── Jump buffer ──────────────────────────────────────────────────────
        if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        // ── Wall jump ────────────────────────────────────────────────────────
        if (jumpPressed && isWallSliding)
        {
            // Kick away from wall + upward
            rb.linearVelocity  = new Vector2(-wallDir * wallJumpX, jumpForce);
            wallJumpLockTimer  = 0.18f;   // briefly override horizontal input
            jumpsRemaining     = 2;       // restore double jump after wall jump
            jumpBufferTimer    = 0f;
            isWallSliding      = false;
            StartCoroutine(WallJumpFlash());
        }
        // ── Normal / coyote jump ─────────────────────────────────────────────
        else if (jumpBufferTimer > 0 && coyoteTimer > 0 && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer   = 0;
            coyoteTimer       = 0;
            jumpsRemaining--;
        }
        // ── Double jump (in air) ─────────────────────────────────────────────
        else if (jumpPressed && jumpsRemaining > 0 && !isGrounded && !isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.9f);
            jumpsRemaining--;
            StartCoroutine(DoubleJumpFlash());
        }

        // Short hop on release
        if (jumpReleased && rb.linearVelocity.y > 0 && !isWallSliding)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        UpdateVisuals();

        if (transform.position.y < -12f) Respawn();
    }

    void FixedUpdate()
    {
        if (!isDashing && canControl)
        {
            if (isWallSliding)
                // Don't push into the wall — let gravity pull the player down freely.
                // Setting vx=0 removes horizontal fighting against the wall surface.
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        // Wall slide: gravity still acts, but we cap the fall to a slow glide
        if (isWallSliding)
        {
            float vy = Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);
        }

        // Hard velocity limits every tick
        float vx      = Mathf.Clamp(rb.linearVelocity.x, -MAX_H_SPEED, MAX_H_SPEED);
        float vyClamped = Mathf.Clamp(rb.linearVelocity.y, MAX_FALL, MAX_RISE);
        rb.linearVelocity = new Vector2(vx, vyClamped);
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        float dir = sr.flipX ? -1f : 1f;
        rb.linearVelocity = new Vector2(dir * dashForce, rb.linearVelocity.y * 0.5f);
        StartCoroutine(DashFlash());
    }

    // ── Visuals ──────────────────────────────────────────────────────────────

    void UpdateVisuals()
    {
        float s = Time.deltaTime * 15f;

        if (isWallSliding)
        {
            // Squish flat against wall, stretch tall — classic wall-cling look
            transform.localScale = Vector3.Lerp(transform.localScale,
                new Vector3(originalScale.x * 0.55f, originalScale.y * 1.35f, 1f), Time.deltaTime * 20f);
            if (!isBoosted) sr.color = Color.Lerp(BaseColor(), Color.green, 0.45f); // lime tint
        }
        else if (!isGrounded && rb.linearVelocity.y > 0.5f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale,
                new Vector3(originalScale.x * 0.75f, originalScale.y * 1.3f, 1f), s);
            if (!isBoosted) sr.color = BaseColor();
        }
        else if (!isGrounded && rb.linearVelocity.y < -0.5f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale,
                new Vector3(originalScale.x * 1.15f, originalScale.y * 0.85f, 1f), s);
            if (!isBoosted) sr.color = BaseColor();
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 20f);
            if (!isBoosted) sr.color = BaseColor();
        }

        if (!wasGrounded && isGrounded)
        {
            transform.localScale = new Vector3(originalScale.x * 1.5f, originalScale.y * 0.55f, 1f);
            float fallSpeed = Mathf.Abs(rb.linearVelocity.y);
            if (fallSpeed > 5f) CameraFollow.Instance?.Shake(fallSpeed * 0.02f, 0.15f);
        }
    }

    System.Collections.IEnumerator WallJumpFlash()
    {
        sr.color = new Color(0.4f, 1f, 0.5f);
        transform.localScale = new Vector3(originalScale.x * 1.4f, originalScale.y * 0.65f, 1f);
        yield return new WaitForSeconds(0.1f);
        sr.color = BaseColor();
    }

    System.Collections.IEnumerator DoubleJumpFlash()
    {
        sr.color = new Color(1f, 1f, 0.3f);
        transform.localScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.7f, 1f);
        yield return new WaitForSeconds(0.12f);
        sr.color = BaseColor();
    }

    System.Collections.IEnumerator DashFlash()
    {
        sr.color = new Color(0.6f, 0.9f, 1f);
        transform.localScale = new Vector3(originalScale.x * 1.6f, originalScale.y * 0.5f, 1f);
        yield return new WaitForSeconds(dashDuration);
        sr.color = BaseColor();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void TakeDamage() { }
    public void BounceUp() { rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f); }
    public void UpdateSpawnPoint(Vector3 pos) => spawnPoint = pos;

    void Respawn()
    {
        transform.position = spawnPoint;
        rb.linearVelocity  = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        // Wall check rays
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, Vector2.right * wallCheckDist);
        Gizmos.DrawRay(transform.position, Vector2.left  * wallCheckDist);
    }
}
