using UnityEngine;

namespace DataDrivenDemo.Player
{
    public enum QuarterViewMovementLockSource
    {
        Menu = 1,
        QuestOffer = 2,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(QuarterViewPlayerAnimation))]
    public sealed class QuarterViewPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 12f; // slerp speed
        [SerializeField] private float gravity = -18f;

        [Header("Jump")]
        [SerializeField] private bool enableJump = true;
        [SerializeField] private float jumpHeight = 1.2f;
        [Tooltip("땅에서 막 떨어진 직후에도 점프 가능(달리며 점프할 때 유리).")]
        [SerializeField] private float coyoteTime = 0.12f;
        [Tooltip("착지 직전에 스페이스를 눌러도 다음 착지 시 점프.")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        [Tooltip("공중에서도 WASD 이동 입력 반영 (1 = 지상과 동일).")]
        [SerializeField, Range(0f, 1f)] private float airMoveControl = 1f;

        [Header("Mobile (simple)")]
        [SerializeField] private bool enableTouchMove = true;
        [SerializeField, Range(0.1f, 0.8f)] private float touchAreaWidthRatio = 0.55f; // left side
        [SerializeField] private float touchDeadZone = 12f; // pixels

        private CharacterController controller;
        private Transform cameraTransform;
        private Vector3 verticalVelocity;
        private Vector3 lastPlanarMove;
        private int movementLockMask;
        private bool jumpTriggered;
        private float coyoteTimeLeft;
        private float jumpBufferLeft;

        // touch state
        private int moveFingerId = -1;
        private Vector2 touchStart;
        private Vector2 touchDelta;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            // Rigidbody + CharacterController 동시에 쓰면 isGrounded 가 거짓으로 떨어져 점프 애니가 반복될 수 있음
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                if (!rb.isKinematic)
                    rb.isKinematic = true;
            }

            PlayerLocator.Invalidate();
            ResolveCameraTransform();
        }

        private void OnEnable() => PlayerLocator.Invalidate();

        private void Start() => ResolveCameraTransform();

        private void ResolveCameraTransform()
        {
            if (cameraTransform != null)
                return;

            var follow = Object.FindFirstObjectByType<QuarterViewFollowCamera>(FindObjectsInactive.Exclude);
            if (follow != null)
                cameraTransform = follow.transform;
            else if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void OnDisable()
        {
            ResetInputState();
            verticalVelocity = Vector3.zero;
        }

        public void ResetInputState()
        {
            moveFingerId = -1;
            touchDelta = Vector2.zero;
            touchStart = Vector2.zero;
        }

        /// <summary>
        /// UI 메뉴·의뢰 패널 등에서 호출합니다. 컴포넌트를 비활성화하지 않고 이동/점프만 막아 중력은 유지합니다.
        /// </summary>
        public void SetMovementLock(QuarterViewMovementLockSource source, bool locked)
        {
            var bit = (int)source;
            if (locked)
            {
                movementLockMask |= bit;
                ResetInputState();
            }
            else
            {
                movementLockMask &= ~bit;
            }
        }

        private bool IsMovementExternallyLocked => movementLockMask != 0;

        /// <summary>애니메이션용: 수평 이동 방향(월드).</summary>
        public Vector3 LastPlanarMove => lastPlanarMove;

        /// <summary>애니메이션·점프 판정용(엄격). CharacterController.isGrounded 만 사용합니다.</summary>
        public bool IsGroundedForAnimation => controller != null && controller.isGrounded;

        public float VerticalSpeed => verticalVelocity.y;

        public bool IsMovementLocked => movementLockMask != 0;

        /// <summary>이 프레임에 점프가 발생했으면 true (한 번만).</summary>
        public bool ConsumeJumpTriggered()
        {
            if (!jumpTriggered)
                return false;
            jumpTriggered = false;
            return true;
        }

        private void Update()
        {
            UpdateJumpTimers();

            var input = IsMovementExternallyLocked ? Vector2.zero : ReadMoveInput();
            var move = ComputeCameraRelativeMove(input);
            lastPlanarMove = move;

            if (move.sqrMagnitude > 0.0001f)
            {
                var targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
            }

            var grounded = controller.isGrounded;
            if (grounded && verticalVelocity.y < 0f)
                verticalVelocity.y = -1f;

            if (TryPerformJump())
            {
                var g = Mathf.Abs(gravity);
                verticalVelocity.y = Mathf.Sqrt(2f * Mathf.Max(0.1f, jumpHeight) * g);
                jumpTriggered = true;
                coyoteTimeLeft = 0f;
                jumpBufferLeft = 0f;
            }

            verticalVelocity.y += gravity * Time.deltaTime;

            var horizontalSpeed = grounded ? 1f : airMoveControl;
            var velocity = (move * (moveSpeed * horizontalSpeed)) + verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void UpdateJumpTimers()
        {
            if (controller.isGrounded)
                coyoteTimeLeft = coyoteTime;
            else
                coyoteTimeLeft = Mathf.Max(0f, coyoteTimeLeft - Time.deltaTime);

            if (ReadJumpPressed())
                jumpBufferLeft = jumpBufferTime;
            else
                jumpBufferLeft = Mathf.Max(0f, jumpBufferLeft - Time.deltaTime);
        }

        private bool TryPerformJump()
        {
            if (!enableJump || IsMovementExternallyLocked)
                return false;
            if (jumpBufferLeft <= 0f)
                return false;
            if (coyoteTimeLeft <= 0f)
                return false;
            if (verticalVelocity.y > 0.15f)
                return false;

            return true;
        }

        private static bool ReadJumpPressed()
        {
            return Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
        }

        private Vector2 ReadMoveInput()
        {
            // PC (Input Manager)
            var x = Input.GetAxisRaw("Horizontal");
            var y = Input.GetAxisRaw("Vertical");
            var pc = new Vector2(x, y);
            if (pc.sqrMagnitude > 0.001f)
                return Vector2.ClampMagnitude(pc, 1f);

            // Mobile (very simple left-side drag joystick)
            if (!enableTouchMove)
                return Vector2.zero;

            HandleTouchMove();
            if (moveFingerId == -1)
                return Vector2.zero;

            if (touchDelta.sqrMagnitude < touchDeadZone * touchDeadZone)
                return Vector2.zero;

            var v = touchDelta / Mathf.Max(1f, (Screen.dpi > 0 ? Screen.dpi * 0.25f : 100f)); // normalize-ish
            return Vector2.ClampMagnitude(v, 1f);
        }

        private void HandleTouchMove()
        {
            // keep current finger if still present
            if (moveFingerId != -1)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.fingerId != moveFingerId) continue;

                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        moveFingerId = -1;
                        touchDelta = Vector2.zero;
                        return;
                    }

                    touchDelta = t.position - touchStart;
                    return;
                }

                // finger disappeared
                moveFingerId = -1;
                touchDelta = Vector2.zero;
                return;
            }

            // acquire new finger from left area
            var maxX = Screen.width * touchAreaWidthRatio;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                if (t.position.x > maxX) continue;

                moveFingerId = t.fingerId;
                touchStart = t.position;
                touchDelta = Vector2.zero;
                return;
            }
        }

        private Vector3 ComputeCameraRelativeMove(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                right = cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            var move = (right * input.x) + (forward * input.y);
            return move.sqrMagnitude > 1f ? move.normalized : move;
        }
    }
}

