using UnityEngine;
using UnityEngine.UI;

public class PlayerController2D : MonoBehaviour
{
    [Header("Ground Move")]
    public float moveSpeed = 0.01f;

    [Header("Charge Jump")]
    public float minJumpForce = 0.1f;
    public float maxJumpForce = 0.7f;
    public float minHorizontalForce = 0.15f;
    public float maxHorizontalForce = 0.3f;
    public float chargeTime = 1.8f;

    [Header("Jump Lock")]
    public float jumpLockDuration = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Ice Settings")]
    public LayerMask iceLayer;
    public float iceSlideAcceleration = 3f;
    public float iceMaxSlideSpeed = 2.2f;
    public float icePassiveSlide = 0.8f;

    [Header("UI")]
    public Image chargeBarFill;

    [Header("Effects")]
    public ParticleSystem landingEffect;

    private Rigidbody2D rb;
    private float xInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isOnIce;

    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private float jumpLockTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 좌우 입력
        xInput = Input.GetAxisRaw("Horizontal");

        // 땅 체크
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // 빙판 체크
        isOnIce = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            iceLayer
        );

        // 착지 감지
        if (!wasGrounded && isGrounded)
        {
            if (landingEffect != null)
            {
                landingEffect.Play();
            }
        }

        wasGrounded = isGrounded;

        // 점프 직후 잠금 타이머 감소
        if (jumpLockTimer > 0f)
        {
            jumpLockTimer -= Time.deltaTime;
        }

        // 차징 시작
        if (Input.GetButtonDown("Jump") && isGrounded && !isCharging)
        {
            isCharging = true;
            currentChargeTime = 0f;
            rb.velocity = Vector2.zero;
        }

        // 차징 중
        if (isCharging)
        {
            currentChargeTime += Time.deltaTime;
            currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, chargeTime);
        }

        // 차징바 업데이트
        if (chargeBarFill != null)
        {
            if (isCharging)
            {
                chargeBarFill.fillAmount = currentChargeTime / chargeTime;
            }
            else
            {
                chargeBarFill.fillAmount = 0f;
            }
        }

        // 차징 후 점프 발사
        if (Input.GetButtonUp("Jump") && isCharging)
        {
            float chargePercent = currentChargeTime / chargeTime;

            // 높이는 완만하게 증가
            float jumpCurve = Mathf.Sqrt(chargePercent);

            // 거리는 후반 차징일수록 확실히 증가
            float horizontalCurve = chargePercent * chargePercent * chargePercent;
            horizontalCurve = Mathf.Clamp01(horizontalCurve);

            float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, jumpCurve);

            float horizontalForce = 0f;
            if (Mathf.Abs(xInput) > 0.01f)
            {
                horizontalForce = Mathf.Sign(xInput) *
                                  Mathf.Lerp(minHorizontalForce, maxHorizontalForce, horizontalCurve);
            }

            rb.velocity = new Vector2(horizontalForce, jumpForce);

            isCharging = false;
            currentChargeTime = 0f;
            jumpLockTimer = jumpLockDuration;
        }
    }

    void FixedUpdate()
    {
        if (!isCharging && isGrounded && jumpLockTimer <= 0f)
        {
            // 빙판 위
            if (isOnIce)
            {
                float targetX = rb.velocity.x;

                // 방향 입력이 있으면 가속
                if (Mathf.Abs(xInput) > 0.01f)
                {
                    targetX += xInput * iceSlideAcceleration * Time.fixedDeltaTime;
                }
                else
                {
                    // 입력이 없어도 기존 방향으로 조금 더 미끄러짐
                    if (Mathf.Abs(rb.velocity.x) > 0.01f)
                    {
                        targetX += Mathf.Sign(rb.velocity.x) * icePassiveSlide * Time.fixedDeltaTime;
                    }
                }

                targetX = Mathf.Clamp(targetX, -iceMaxSlideSpeed, iceMaxSlideSpeed);
                rb.velocity = new Vector2(targetX, rb.velocity.y);
            }
            // 일반 바닥
            else
            {
                rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}