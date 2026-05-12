using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>대상 위에 떠 있으며 카메라를 향해 Y축만 회전합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class QuestFloatingMarker : MonoBehaviour
    {
        [SerializeField] private Transform anchor;
        [SerializeField] private float heightOffset = 2.2f;

        public void Initialize(Transform anchorTransform, float yOffset)
        {
            anchor = anchorTransform;
            heightOffset = yOffset;
            SnapPosition();
        }

        private void SnapPosition()
        {
            if (anchor == null)
                return;
            transform.position = anchor.position + Vector3.up * heightOffset;
        }

        private void LateUpdate()
        {
            if (anchor == null)
            {
                Destroy(gameObject);
                return;
            }

            var basePos = anchor.position + Vector3.up * heightOffset;
            transform.position = basePos;

            var cam = Camera.main;
            if (cam == null)
            {
                cam = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
                if (cam == null)
                    return;
            }

            // 탑다운(카메라가 거의 수직)일 때 XZ 투영이 0이 되면 스프라이트가 옆으로 안 보입니다.
            var toCam = cam.transform.position - transform.position;
            var flat = new Vector3(toCam.x, 0f, toCam.z);
            if (flat.sqrMagnitude < 0.01f)
            {
                flat = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z);
                if (flat.sqrMagnitude < 0.01f)
                    flat = Vector3.forward;
            }

            transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }
    }
}
