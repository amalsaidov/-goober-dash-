using System.Collections;
using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    public float jumpForce = 14f;
    public LayerMask groundLayer;
    [HideInInspector] public bool  canMove  = false;
    [HideInInspector] public float speed    = 6f;
    [HideInInspector] public bool  isBoosted = false;

    // ── Difficulty-set intelligence parameters ────────────────────────────
    private float mistakeChance     = 0.08f; // 0 = perfect, 0.4 = falls off a lot
    private float stuckRecoveryTime = 1.5f;  // seconds until stuck-detection kicks in
    private bool  useWallJump       = false; // Ultra: uses Zone-5 wall shaft shortcut
    private float lookahead         = 1.1f;  // obstacle detection distance
    private float dashCooldownMin   = 4f;
    private float dashCooldownMax   = 9f;
    private float catchUpBoost1     = 1.12f;
    private float catchUpBoost2     = 1.00f;
    private float leadBoost         = 1.00f;

    // ── Physics limits ────────────────────────────────────────────────────
    private const float MAX_FALL = -24f;
    private const float MAX_RISE = 20f;
    private float maxVelocity = 18f;

    // ── Internal state ────────────────────────────────────────────────────
    private Rigidbody2D   rb;
    private SpriteRenderer sr;
    private Color  baseColor;
    private bool   isGrounded;
    private float  jumpCooldown;
    private int    currentWaypoint = 0;
    private Vector3[] waypoints;
    private int    jumpsRemaining = 2;
    private float  reactionDelay  = 0.12f;
    private float  dashCooldownTimer;
    private bool   isDashing;
    private bool   jumpQueued;
    private float  jumpQueueTimer;

    // ── Stuck detection ───────────────────────────────────────────────────
    private float   stuckTimer;
    private Vector3 lastStuckPos;

    // ── Wall-jump shaft mode (Ultra only) ─────────────────────────────────
    // Zone-4 climb shaft: left wall x≈-6, right wall x≈6, top y≈47
    private bool  inShaftMode  = false;
    private int   shaftDir     = 1;    // 1=moving right toward right wall, -1=left
    private const float SHAFT_X_MIN   = -6.5f;
    private const float SHAFT_X_MAX   =  6.5f;
    private const float SHAFT_ENTRY_X =  0f;    // enter shaft near x=0
    private const float SHAFT_TOP_Y   = 47f;    // exit once above the walls

    // ── Init ──────────────────────────────────────────────────────────────

    void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        sr        = GetComponent<SpriteRenderer>();
        baseColor = sr.color;

        // Default speed = player speed; overridden by ApplyPreset before race starts
        speed            = 7.0f;
        reactionDelay    = Random.Range(0.09f, 0.18f);
        dashCooldownTimer = Random.Range(3f, 8f);
        stuckTimer       = stuckRecoveryTime;
        lastStuckPos     = transform.position;
    }

    void Start()
    {
        if (WaypointPath.Instance != null)
            waypoints = WaypointPath.Instance.points;
    }

    /// <summary>Set waypoints from the active map's WaypointPath (called by MapManager).</summary>
    public void SetWaypoints(Vector3[] pts) { waypoints = pts; currentWaypoint = 0; }

    // Called by DifficultyManager.Select() before the race starts
    public void ApplyPreset(DifficultyManager.Preset p)
    {
        speed          = Random.Range(p.speedMin, p.speedMax);
        reactionDelay  = Random.Range(p.reactMin, p.reactMax);
        mistakeChance  = p.mistakeChance;
        stuckRecoveryTime = p.stuckRecoveryTime;
        useWallJump    = p.useWallJump;
        lookahead      = p.lookahead;
        dashCooldownMin = p.dashCDMin;
        dashCooldownMax = p.dashCDMax;
        dashCooldownTimer = Random.Range(p.dashCDMin * 0.5f, p.dashCDMax); // staggered
        catchUpBoost1  = 1.00f;   // no rubber-banding — all equal
        catchUpBoost2  = 1.00f;
        leadBoost      = 1.00f;
        jumpForce      = 14f;     // same jump as player
        maxVelocity    = 18f;     // same cap for everyone

        stuckTimer   = stuckRecoveryTime > 0 ? stuckRecoveryTime : float.MaxValue;
        lastStuckPos = transform.position;
    }

    public void ResetWaypoint()
    {
        currentWaypoint = 0;
        isDashing       = false;
        jumpQueued      = false;
        inShaftMode     = false;
        shaftDir        = 1;
    }

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!canMove || waypoints == null || waypoints.Length == 0) return;

        isGrounded = Physics2D.OverlapCircle(
            transform.position + Vector3.down * 0.8f, 0.25f, groundLayer);

        if (isGrounded) jumpsRemaining = 2;

        jumpCooldown      -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;

        // ── Ultra: wall-shaft shortcut logic ────────────────────────────
        if (useWallJump) UpdateShaftBehavior();

        // Skip normal navigation while in shaft mode
        if (inShaftMode) return;

        // ── Normal waypoint navigation ───────────────────────────────────
        Vector3 target = waypoints[Mathf.Min(currentWaypoint, waypoints.Length - 1)];
        float dx = target.x - transform.position.x;
        float dy = target.y - transform.position.y;

        if (Mathf.Abs(dx) < 2f && Mathf.Abs(dy) < 2.5f)
            currentWaypoint = Mathf.Min(currentWaypoint + 1, waypoints.Length - 1);

        // ── Dash: only on safe flat ground ──────────────────────────────
        if (!isDashing && dashCooldownTimer <= 0 && isGrounded && jumpCooldown <= 0)
        {
            bool safeAhead = Physics2D.Raycast(
                transform.position + Vector3.right * 1.8f, Vector2.down, 2f, groundLayer);
            if (safeAhead) StartCoroutine(DoDash());
        }

        // ── Jump decision ────────────────────────────────────────────────
        if (!jumpQueued && jumpCooldown <= 0 && isGrounded && jumpsRemaining > 0)
        {
            bool wallAhead = Physics2D.Raycast(
                transform.position, Vector2.right, lookahead * 1.3f, groundLayer);
            bool gapAhead  = !Physics2D.Raycast(
                transform.position + Vector3.right * lookahead, Vector2.down, 2f, groundLayer);
            bool needHeight = dy > 1.5f;

            if (wallAhead || gapAhead || needHeight)
            {
                // Mistake system: Easy bots randomly botch jumps
                if (mistakeChance > 0f && Random.value < mistakeChance)
                {
                    // Botched! Don't jump — bot will run into wall or fall off edge.
                    // Add a small cooldown so it doesn't retry on the next frame.
                    jumpCooldown = Random.Range(0.1f, 0.3f);
                }
                else
                {
                    jumpQueued     = true;
                    jumpQueueTimer = reactionDelay;
                }
            }
        }

        // ── Double jump in air ───────────────────────────────────────────
        // Mirrors the player: if still airborne with a jump left and a reason to
        // use it (gap ahead or need more height), queue a second jump.
        // Subject to the same mistake system as the ground jump.
        if (!jumpQueued && jumpCooldown <= 0 && !isGrounded
            && jumpsRemaining > 0 && rb.linearVelocity.y < 1.5f)
        {
            bool gapStillAhead = !Physics2D.Raycast(
                transform.position + Vector3.right * lookahead, Vector2.down, 2.5f, groundLayer);
            bool needMoreHeight = dy > 1.0f;

            if (gapStillAhead || needMoreHeight)
            {
                if (mistakeChance <= 0f || Random.value > mistakeChance)
                {
                    jumpQueued     = true;
                    jumpQueueTimer = reactionDelay * 0.5f;
                }
                else
                    jumpCooldown = Random.Range(0.05f, 0.15f);
            }
        }

        // ── Execute queued jump ──────────────────────────────────────────
        if (jumpQueued)
        {
            jumpQueueTimer -= Time.deltaTime;
            if (jumpQueueTimer <= 0)
            {
                jumpQueued = false;
                if (jumpsRemaining > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                        jumpForce * (isGrounded ? 1f : 0.88f));
                    jumpCooldown = 0.35f;
                    jumpsRemaining--;
                }
            }
        }

        // ── Stuck detection ──────────────────────────────────────────────
        if (stuckRecoveryTime > 0)
        {
            stuckTimer -= Time.deltaTime;
            if (stuckTimer <= 0)
            {
                stuckTimer = stuckRecoveryTime;
                float moved = Vector3.Distance(transform.position, lastStuckPos);
                if (moved < 0.4f && canMove) // barely moved — stuck
                {
                    // Recovery: jump if grounded, or try reversing briefly
                    if (isGrounded && jumpsRemaining > 0)
                    {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                        jumpsRemaining--;
                    }
                }
                lastStuckPos = transform.position;
            }
        }
    }

    // ── Wall-shaft logic (Ultra only) ────────────────────────────────────
    // How it works:
    //   1. Bot runs under the shaft walls at ground level (walls start at y=0)
    //   2. At the entry X zone it jumps up into the shaft gap
    //   3. Once above y=0, it alternates left-right to wall-jump up
    //   4. Reaches top platform (y≈9.5), grabs speed pad, rockets to finish

    void UpdateShaftBehavior()
    {
        float px = transform.position.x;
        float py = transform.position.y;

        // Exit shaft mode once we've cleared the top
        if (inShaftMode && py >= SHAFT_TOP_Y)
        {
            inShaftMode = false;
            return;
        }

        // Inside the shaft (above ground level) — wall jump
        if (inShaftMode)
        {
            const float hw = 0.4f;
            bool wallR = Physics2D.Raycast(
                (Vector2)transform.position + Vector2.right * hw, Vector2.right, 0.25f, groundLayer);
            bool wallL = Physics2D.Raycast(
                (Vector2)transform.position - Vector2.right * hw, Vector2.left,  0.25f, groundLayer);

            if (wallR && shaftDir == 1)
            {
                rb.linearVelocity = new Vector2(-9f, jumpForce * 0.95f);
                shaftDir = -1;
            }
            else if (wallL && shaftDir == -1)
            {
                rb.linearVelocity = new Vector2(9f, jumpForce * 0.95f);
                shaftDir = 1;
            }
            return;
        }

        // Approaching shaft entry: jump up to enter it
        bool nearEntry = px > SHAFT_X_MIN && px < SHAFT_ENTRY_X && isGrounded;
        if (nearEntry && jumpCooldown <= 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCooldown = 0.3f;
            inShaftMode  = true;
            shaftDir     = 1;
        }
    }

    IEnumerator DoDash()
    {
        isDashing = true;
        dashCooldownTimer = Random.Range(dashCooldownMin, dashCooldownMax);
        float dashVel = Mathf.Min(speed * 2.2f, maxVelocity);
        rb.linearVelocity = new Vector2(dashVel, rb.linearVelocity.y);

        sr.color = new Color(
            Mathf.Min(baseColor.r + 0.4f, 1f),
            Mathf.Min(baseColor.g + 0.4f, 1f),
            Mathf.Min(baseColor.b + 0.4f, 1f));

        yield return new WaitForSeconds(0.18f);
        sr.color  = baseColor;
        isDashing = false;
    }

    float CatchUpBoost()
    {
        if (waypoints == null) return leadBoost;
        float progress = (float)currentWaypoint / waypoints.Length;
        if (progress < 0.15f) return catchUpBoost1;
        if (progress < 0.35f) return catchUpBoost2;
        return leadBoost; // Easy: 0.94 (eases off), Ultra: 1.10 (pushes harder)
    }

    void FixedUpdate()
    {
        if (!canMove) return;

        if (inShaftMode)
        {
            // Inside shaft: move toward the target wall
            rb.linearVelocity = new Vector2(shaftDir * speed * 0.65f, rb.linearVelocity.y);
        }
        else if (!isDashing)
        {
            rb.linearVelocity = new Vector2(speed * CatchUpBoost(), rb.linearVelocity.y);
        }

        float vx = Mathf.Clamp(rb.linearVelocity.x, -maxVelocity, maxVelocity);
        float vy = Mathf.Clamp(rb.linearVelocity.y, MAX_FALL, MAX_RISE);
        rb.linearVelocity = new Vector2(vx, vy);
    }
}
