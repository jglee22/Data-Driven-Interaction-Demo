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
    public sealed class QuarterViewPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 12f; // slerp speed
        [SerializeField] private float gravity = -18f;

        [Header("Jump")]
        [SerializeField] private bool enableJump = true;
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("Mobile (simple)")]
        [SerializeField] private bool enableTouchMove = true;
        [SerializeField, Range(0.1f, 0.8f)] private float touchAreaWidthRatio = 0.55f; // left side
        [SerializeField] private float touchDeadZone = 12f; // pixels

        private CharacterController controller;
        private Vector3 verticalVelocity;
        private int movementLockMask;

        // touch state
        private int moveFingerId = -1;
        private Vector2 touchStart;
        private Vector2 touchDelta;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
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

        private void Update()
        {
            var input = IsMovementExternallyLocked ? Vector2.zero : ReadMoveInput();
            var move = ComputeCameraRelativeMove(input);

            if (move.sqrMagnitude > 0.0001f)
            {
                var targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
            }

            // gravity
            if (controller.isGrounded && verticalVelocity.y < 0f)
                verticalVelocity.y = -1f;

            if (enableJump && !IsMovementExternallyLocked && controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                // v = sqrt(2 * h * -g)
                var g = Mathf.Abs(gravity);
                verticalVelocity.y = Mathf.Sqrt(2f * Mathf.Max(0.1f, jumpHeight) * g);
            }

            verticalVelocity.y += gravity * Time.deltaTime;

            var velocity = (move * moveSpeed) + verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
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

