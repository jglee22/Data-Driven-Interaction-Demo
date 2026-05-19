using System;
using UnityEngine;
using DataDrivenDemo.Core.Save;

namespace DataDrivenDemo.Firebase
{
    [DisallowMultipleComponent]
    public sealed class FirebaseSaveInstaller : MonoBehaviour
    {
        /// <summary>
        /// <see cref="SaveServices.QuestSave"/>가 Firestore 구현으로 교체된 뒤 한 번(메인 스레드).
        /// <see cref="DataDrivenDemo.Quest.QuestSystem"/> 등이 폴링 대신 이 이벤트로 대기할 수 있습니다.
        /// </summary>
        public static event Action<ISaveService> QuestSaveInstalled;

        [SerializeField] private FirebaseBootstrap bootstrap;
        private FirestoreQuestSaveService service;
        private System.Action<string> signedInHandler;

        private void Awake()
        {
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<FirebaseBootstrap>(FindObjectsInactive.Include);

            if (bootstrap == null)
            {
                Debug.LogWarning("[FirebaseSaveInstaller] FirebaseBootstrap not found.");
                return;
            }

            if (bootstrap.IsReady)
                Install();
            else
            {
                signedInHandler = _ => Install();
                bootstrap.SignedIn += signedInHandler;
            }
        }

        private void OnDestroy()
        {
            if (bootstrap != null && signedInHandler != null)
                bootstrap.SignedIn -= signedInHandler;
        }

        private void Install()
        {
            if (service != null)
                return;

            service = new FirestoreQuestSaveService(bootstrap);
            SaveServices.QuestSave = service;
            QuestSaveInstalled?.Invoke(service);
            Debug.Log("[FirebaseSaveInstaller] SaveServices.QuestSave set to Firestore.");
        }

        public FirestoreQuestSaveService GetService() => service;
    }
}

