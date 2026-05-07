using UnityEngine;
using DataDrivenDemo.Core.Save;

namespace DataDrivenDemo.Firebase
{
    [DisallowMultipleComponent]
    public sealed class FirebaseSaveInstaller : MonoBehaviour
    {
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
            Debug.Log("[FirebaseSaveInstaller] SaveServices.QuestSave set to Firestore.");
        }

        public FirestoreQuestSaveService GetService() => service;
    }
}

