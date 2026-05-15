using UnityEngine;

namespace DataDrivenDemo.Player
{
    [DisallowMultipleComponent]
    public sealed class QuarterViewPlayerAnimation : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private QuarterViewPlayerController movement;
        [SerializeField] private Animator animator;

        [Header("Animator state names")]
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string forwardState = "Forward";
        [SerializeField] private string backwardState = "Backward";
        [SerializeField] private string jumpState = "Jump";
        [SerializeField] private string landState = "Land";

        [Header("Tuning")]
        [SerializeField] private float crossFadeDuration = 0.1f;
        [SerializeField] private float backwardDotThreshold = -0.2f;
        [SerializeField] private float landLockDuration = 0.35f;
        [Tooltip("이 속도 이상이면 공중으로 봅니다.")]
        [SerializeField] private float airborneVerticalSpeed = 0.15f;
        [Tooltip("착지로 볼 수직 속도(이하).")]
        [SerializeField] private float landedVerticalSpeed = 0.08f;
        [SerializeField] private float minJumpAirTime = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool logMissingSetup;

        private int idleHash;
        private int forwardHash;
        private int backwardHash;
        private int jumpHash;
        private int landHash;
        private int currentHash;

        private bool jumpActive;
        private bool wasInAir;
        private float jumpStartTime;
        private float landLockUntil;

        private bool setupOk;

        private void Awake()
        {
            if (movement == null)
                movement = GetComponent<QuarterViewPlayerController>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            CacheStateHashes();
        }

        private void Start()
        {
            setupOk = ValidateSetup();
            jumpActive = false;
            wasInAir = false;
            if (setupOk)
                Play(idleHash, force: true);
        }

        private void LateUpdate()
        {
            if (!setupOk || animator == null || movement == null)
                return;

            if (movement.ConsumeJumpTriggered())
            {
                jumpActive = true;
                wasInAir = false;
                jumpStartTime = Time.time;
                landLockUntil = 0f;
                Play(jumpHash, force: true);
                return;
            }

            if (jumpActive)
            {
                UpdateJumpSequence();
                return;
            }

            if (Time.time < landLockUntil)
            {
                TryFinishLandEarly();
                if (Time.time < landLockUntil)
                    return;
            }

            UpdateGroundLocomotion();
        }

        private void UpdateJumpSequence()
        {
            var grounded = movement.IsGroundedForAnimation;
            var vy = movement.VerticalSpeed;

            if (!wasInAir)
            {
                if (!grounded || vy > airborneVerticalSpeed)
                    wasInAir = true;
            }

            var canLand = wasInAir && grounded && vy <= landedVerticalSpeed &&
                          Time.time - jumpStartTime >= minJumpAirTime;

            if (canLand)
            {
                jumpActive = false;
                wasInAir = false;
                Play(landHash, force: true);
                landLockUntil = Time.time + Mathf.Max(0.05f, landLockDuration);
                return;
            }

            if (currentHash != jumpHash)
                Play(jumpHash, force: true);
        }

        private void TryFinishLandEarly()
        {
            if (animator == null)
                return;

            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash != landHash)
                return;
            if (info.normalizedTime < 0.92f)
                return;

            landLockUntil = 0f;
        }

        private void UpdateGroundLocomotion()
        {
            if (movement.IsMovementLocked || movement.LastPlanarMove.sqrMagnitude < 0.0004f)
            {
                Play(idleHash);
                return;
            }

            var local = transform.InverseTransformDirection(movement.LastPlanarMove);
            if (local.z < backwardDotThreshold)
                Play(backwardHash);
            else
                Play(forwardHash);
        }

        private bool ValidateSetup()
        {
            if (animator == null)
            {
                if (logMissingSetup)
                    Debug.LogError("[QuarterViewPlayerAnimation] Animator 없음", this);
                return false;
            }

            if (animator.runtimeAnimatorController == null)
            {
                if (logMissingSetup)
                    Debug.LogError("[QuarterViewPlayerAnimation] Controller 비어 있음", this);
                return false;
            }

            if (!animator.HasState(0, idleHash) && logMissingSetup)
                Debug.LogError($"[QuarterViewPlayerAnimation] '{idleState}' 상태 없음", this);

            return animator.HasState(0, idleHash);
        }

        private void CacheStateHashes()
        {
            idleHash = Animator.StringToHash(idleState);
            forwardHash = Animator.StringToHash(forwardState);
            backwardHash = Animator.StringToHash(backwardState);
            jumpHash = Animator.StringToHash(jumpState);
            landHash = Animator.StringToHash(landState);
        }

        private void OnValidate() => CacheStateHashes();

        private void Play(int stateHash, bool force = false)
        {
            if (stateHash == 0)
                return;
            if (!force && currentHash == stateHash)
                return;
            if (!animator.HasState(0, stateHash))
                return;

            if (crossFadeDuration <= 0f)
                animator.Play(stateHash, 0, 0f);
            else
                animator.CrossFadeInFixedTime(stateHash, crossFadeDuration, 0, 0f);

            currentHash = stateHash;
        }
    }
}
