using UnityEngine;

namespace DataDrivenDemo.Player
{
    [DisallowMultipleComponent]
    public sealed class QuarterViewFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Quarter View")]
        [SerializeField] private float distance = 8f;
        [SerializeField, Range(10f, 80f)] private float pitch = 35f;
        [SerializeField] private float yaw = 45f;

        [Header("Smoothing")]
        [SerializeField] private float followSmoothTime = 0.12f;
        [SerializeField] private float rotateSmoothTime = 0.08f;

        [Header("Optional Zoom/Rotate Input")]
        [SerializeField] private bool allowRotate = true;
        [SerializeField] private bool allowZoom = true;
        [SerializeField] private float rotateSpeed = 140f; // degrees/sec
        [SerializeField] private float zoomSpeed = 6f;     // units/sec
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 14f;

        private Vector3 followVelocity;
        private float yawVelocity;
        private float pitchVelocity;

        private void Reset()
        {
            var cam = GetComponent<Camera>();
            if (cam == null && Camera.main != null)
                transform.position = Camera.main.transform.position;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            HandleInput();

            var desiredRot = Quaternion.Euler(pitch, yaw, 0f);
            var desiredPos = target.position - (desiredRot * Vector3.forward) * distance;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref followVelocity, followSmoothTime);

            var currentEuler = transform.rotation.eulerAngles;
            var smoothYaw = Mathf.SmoothDampAngle(currentEuler.y, yaw, ref yawVelocity, rotateSmoothTime);
            var smoothPitch = Mathf.SmoothDampAngle(currentEuler.x, pitch, ref pitchVelocity, rotateSmoothTime);
            transform.rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
        }

        private void HandleInput()
        {
            // PC: 우클릭 드래그로 회전, 휠로 줌
            if (allowRotate && Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            }

            if (allowZoom)
            {
                var scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.001f)
                    distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
            }

            // Mobile: 1손가락 드래그로 회전(간단), 2손가락 핀치로 줌(간단)
            if (Input.touchCount == 1 && allowRotate)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved)
                    yaw += (t.deltaPosition.x * 0.15f) * rotateSpeed * Time.deltaTime;
            }
            else if (Input.touchCount >= 2 && allowZoom)
            {
                var t0 = Input.GetTouch(0);
                var t1 = Input.GetTouch(1);

                var prev0 = t0.position - t0.deltaPosition;
                var prev1 = t1.position - t1.deltaPosition;

                var prevMag = (prev0 - prev1).magnitude;
                var curMag = (t0.position - t1.position).magnitude;
                var delta = curMag - prevMag;

                distance = Mathf.Clamp(distance - (delta * 0.01f) * zoomSpeed, minDistance, maxDistance);
            }

            pitch = Mathf.Clamp(pitch, 15f, 75f);
        }

        public void SetTarget(Transform newTarget) => target = newTarget;
    }
}

