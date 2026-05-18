using UnityEngine;

namespace DataDrivenDemo.Interaction
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName = "오브젝트";

        public string Id => string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

        public virtual bool CanInteract(GameObject interactor) => true;

        public abstract void Interact(GameObject interactor);

        /// <summary>Unity는 추상 베이스의 Awake를 호출하지 않으므로, 파생 클래스에서 반드시 base.Awake()를 호출합니다.</summary>
        protected virtual void Awake() => InteractableRegistry.Register(this);

        /// <summary>에디터·Edit Mode 테스트에서 컴포넌트 추가 시 등록합니다.</summary>
        private void Reset() => InteractableRegistry.Register(this);

        protected virtual void OnDestroy() => InteractableRegistry.Unregister(this);

        protected virtual void OnEnable() => InteractableRegistry.Register(this);

        protected virtual void OnDisable() => InteractableRegistry.Unregister(this);
    }
}

