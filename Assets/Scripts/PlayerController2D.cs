using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public Animator anim;
    public SpriteRenderer spriteRenderer;
    public Transform groundCheck;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    public float groundRayDistance = 0.25f;

    [Header("Normal Move")]
    public float normalMoveSpeed = 4f;

    [Header("Ice Move")]
    public float iceAcceleration = 20f;
    public float iceDeceleration = 2f;
    public float maxIceSpeed = 4f;

    [Header("Air Move")]
    public float airControl = 0.15f;
    public float airDragX = 0.2f;

    [Header("Charge Jump")]
    public float minJumpForce = 5f;
    public float maxJumpForce = 12f;
    public float maxChargeTime = 1.0f;

    [Header("Horizontal Jump Boost")]
    public float minHorizontalJumpBoost = 2f;
    public float maxHorizontalJumpBoost = 6f;

    [Header("Runtime Debug")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isOnIce;
    [SerializeField] private bool isCharging;
    [SerializeField] private bool isJumping;
    [SerializeField] private float currentXVelocity;
    [SerializeField] private float chargeTimer;

    private bool wasGrounded;
    private float moveInput;
    private bool jumpButtonReleased;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0.01f)
            spriteRenderer.flipX = false;
        else if (moveInput < -0.01f)
            spriteRenderer.flipX = true;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            StartCharging();
        }

        if (Input.GetKey(KeyCode.Space) && isCharging)
        {
            ContinueCharging();
        }

        if (Input.GetKeyUp(KeyCode.Space) && isCharging)
        {
            jumpButtonReleased = true;
        }

        if (anim != null)
        {
            anim.SetBool("isGrounded", isGrounded);
            anim.SetBool("isCharging", isCharging);
            anim.SetBool("isJumping", isJumping);
        }
    }

    void FixedUpdate()
    {
        CheckGroundAndSurface();

        if (!wasGrounded && isGrounded && rb.velocity.y <= 0.05f)
        {
            if (anim != null)
                anim.SetTrigger("landTrigger");

            isJumping = false;
        }

        wasGrounded = isGrounded;

        HandleHorizontalMove();

        if (jumpButtonReleased)
        {
            ReleaseJump();
            jumpButtonReleased = false;
        }

        currentXVelocity = rb.velocity.x;
    }

    void StartCharging()
    {
        isCharging = true;
        isJumping = false;
        chargeTimer = 0f;

        // 차징 시작 시 수직 속도만 정리
        rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    void ContinueCharging()
    {
        chargeTimer += Time.deltaTime;

        if (chargeTimer > maxChargeTime)
            chargeTimer = maxChargeTime;
    }

    void ReleaseJump()
    {
        if (!isCharging) return;

        isCharging = false;

        float chargePercent = chargeTimer / maxChargeTime;
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, chargePercent);
        float horizontalBoost = Mathf.Lerp(minHorizontalJumpBoost, maxHorizontalJumpBoost, chargePercent);

        float direction = 0f;

        // 입력 방향 우선, 없으면 바라보는 방향 사용
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            direction = Mathf.Sign(moveInput);
        }
        else
        {
            direction = spriteRenderer.flipX ? -1f : 1f;
        }

        // 수직속도만 초기화하고 점프
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        rb.AddForce(Vector2.right * direction * horizontalBoost, ForceMode2D.Impulse);

        isJumping = true;
        isGrounded = false;
        chargeTimer = 0f;
    }

    void HandleHorizontalMove()
    {
        float x = rb.velocity.x;

        // 차징 중 지상에서는 좌우 고정
        if (isCharging && isGrounded)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        if (isGrounded)
        {
            if (isOnIce)
            {
                // 얼음 위에서는 가속/감속으로 미끄러짐 구현
                if (Mathf.Abs(moveInput) > 0.01f)
                {
                    x += moveInput * iceAcceleration * Time.fixedDeltaTime;
                    x = Mathf.Clamp(x, -maxIceSpeed, maxIceSpeed);
                }
                else
                {
                    x = Mathf.MoveTowards(x, 0f, iceDeceleration * Time.fixedDeltaTime);
                }
            }
            else
            {
                // 일반 바닥은 즉시 반응
                x = moveInput * normalMoveSpeed;
            }
        }
        else
        {
            // 공중에서는 기존 속도 유지 + 약간의 조작
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                x += moveInput * airControl;
            }
            else
            {
                x = Mathf.MoveTowards(x, 0f, airDragX * Time.fixedDeltaTime);
            }
        }

        rb.velocity = new Vector2(x, rb.velocity.y);
    }

    void CheckGroundAndSurface()
    {
        isGrounded = false;
        isOnIce = false;

        Collider2D groundHit = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        if (groundHit != null)
        {
            isGrounded = true;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundRayDistance,
            groundLayer
        );

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Ice"))
            {
                isOnIce = true;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            groundCheck.position,
            groundCheck.position + Vector3.down * groundRayDistance
        );
    }
}