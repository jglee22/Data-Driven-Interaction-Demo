using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DataDrivenDemo.UI;

namespace DataDrivenDemo.Core.Flow
{
    [DisallowMultipleComponent]
    public sealed class StartSceneLoader : MonoBehaviour
    {
        [Header("Target Scene")]
        [Tooltip("StartScene에서 자동으로 로드할 플레이 씬 이름(빌드 세팅에 있어야 함).")]
        [SerializeField] private string demoSceneName = "DemoScene";

        [Tooltip("체크하면 Additive로 로드하고 StartScene을 유지합니다.")]
        [SerializeField] private bool loadAdditive = false;

        [Header("Menu")]
        [Tooltip("DemoScene 로드 후 메뉴를 강제로 열어둡니다.")]
        [SerializeField] private bool forceMenuOpenAfterLoad = true;

        private IEnumerator Start()
        {
            if (string.IsNullOrWhiteSpace(demoSceneName))
                yield break;

            // 이미 로드된 경우 중복 로드 방지
            if (SceneManager.GetSceneByName(demoSceneName).IsValid())
            {
                if (forceMenuOpenAfterLoad)
                    ForceMenuOpen();
                yield break;
            }

            var mode = loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var op = SceneManager.LoadSceneAsync(demoSceneName, mode);
            if (op == null)
                yield break;

            while (!op.isDone)
                yield return null;

            // 로드 직후 한 프레임 더 기다려서 오브젝트 초기화(Awake/Start) 시간을 줍니다.
            yield return null;

            if (forceMenuOpenAfterLoad)
                ForceMenuOpen();
        }

        private static void ForceMenuOpen()
        {
            var uiRoot = Object.FindFirstObjectByType<UIRoot>(FindObjectsInactive.Include);
            uiRoot?.ShowMenu();
        }
    }
}

