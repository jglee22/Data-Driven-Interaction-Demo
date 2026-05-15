using DataDrivenDemo.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataDrivenDemo.Player
{
    /// <summary>
    /// 씬의 플레이어(QuarterViewPlayerController)를 런타임에 찾아 공유합니다.
    /// 인스펙터에 플레이어를 직접 연결하지 않아도 됩니다.
    /// </summary>
    public static class PlayerLocator
    {
        private static QuarterViewPlayerController controller;
        private static ProximityInteractor interactor;
        private static Rigidbody rigidbody;
        private static bool searched;

        static PlayerLocator()
        {
            SceneManager.sceneLoaded += (_, _) => Invalidate();
        }

        public static bool IsReady => Ensure();

        public static Transform Transform => Controller != null ? Controller.transform : null;

        public static QuarterViewPlayerController Controller
        {
            get
            {
                Ensure();
                return controller;
            }
        }

        public static ProximityInteractor Interactor
        {
            get
            {
                Ensure();
                return interactor;
            }
        }

        public static Rigidbody Rigidbody
        {
            get
            {
                Ensure();
                return rigidbody;
            }
        }

        public static void Invalidate()
        {
            searched = false;
            controller = null;
            interactor = null;
            rigidbody = null;
        }

        /// <summary>플레이어를 다시 찾습니다(씬 로드·프리팹 교체 후).</summary>
        public static bool Refresh() => Ensure(force: true);

        private static bool Ensure(bool force = false)
        {
            if (!force && searched)
                return controller != null;

            searched = true;
            controller = null;
            interactor = null;
            rigidbody = null;

            foreach (var pc in Object.FindObjectsByType<QuarterViewPlayerController>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (pc == null || !pc.isActiveAndEnabled)
                    continue;
                controller = pc;
                break;
            }

            if (controller == null)
            {
                foreach (var pc in Object.FindObjectsByType<QuarterViewPlayerController>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (pc == null)
                        continue;
                    controller = pc;
                    break;
                }
            }

            if (controller == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null)
                    controller = tagged.GetComponentInParent<QuarterViewPlayerController>();
            }

            if (controller == null)
                return false;

            interactor = controller.GetComponent<ProximityInteractor>();
            if (interactor == null)
                interactor = controller.GetComponentInChildren<ProximityInteractor>(true);

            rigidbody = controller.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = controller.GetComponentInChildren<Rigidbody>(true);

            return true;
        }
    }
}
