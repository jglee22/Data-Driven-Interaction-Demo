using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataDrivenDemo.Interaction;
using DataDrivenDemo.Player;
using DataDrivenDemo.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataDrivenDemo.UI
{
    [DisallowMultipleComponent]
    public sealed class QuestOfferView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform scrollContent;
        [SerializeField] private QuestOfferRowView rowPrefab;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private QuestSystem questSystem;
        [SerializeField] private QuestCatalog catalog;
        [SerializeField] private QuestHudView hud;

        [SerializeField] private bool closeOnEscape = true;

        private readonly List<QuestOfferRowView> rows = new();
        private string[] offeredIds = System.Array.Empty<string>();
        private string selectedQuestId;
        private GameObject lastInteractorRoot;
        private Coroutine openRevealRoutine;

        private GameObject RootGo => panelRoot != null ? panelRoot : gameObject;

        public bool IsOpen => RootGo.activeSelf;

        private void Awake()
        {
            ResolveRefs();

            // 패널 루트가 이 컴포넌트와 같은 오브젝트일 때 RootGo.SetActive(false) 하면,
            // OpenWithIds() 첫 줄 SetActive(true) 직후 Awake 가 다시 끄면서 코루틴 미실행·입력만 꼬이는 상태가 된다.
            // 가시성은 CanvasGroup 로만 숨김하고, 활성 상태는 호출(Open/Close)이 제어하게 둔다.
            var cgInit = RootGo.GetComponent<CanvasGroup>();
            if (cgInit == null)
                cgInit = RootGo.AddComponent<CanvasGroup>();
            cgInit.alpha = 0f;
            cgInit.blocksRaycasts = false;
            cgInit.interactable = false;

            if (!ReferenceEquals(panelRoot, gameObject) && panelRoot != null && panelRoot.transform.IsChildOf(transform))
                panelRoot.SetActive(false);

            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(OnAcceptClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDisable()
        {
            StopOpenRevealRoutine();
            SetGameplayLocked(false);
        }

        private void Update()
        {
            if (!closeOnEscape || !IsOpen)
                return;
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        /// <summary>퀘스트 지정 NPC 에서 호출합니다.</summary>
        public void OpenFromGiver(QuestGiverInteractable giver, GameObject interactorRoot = null)
        {
            lastInteractorRoot = interactorRoot;

            if (giver == null)
            {
                OpenWithIds(System.Array.Empty<string>());
                return;
            }

            var ids = giver.OfferedQuestIds;
            if (ids == null || ids.Length == 0)
                OpenWithIds(null);
            else
                OpenWithIds(ids.ToArray());
        }

        /// <summary>ids 가 null 이면 카탈로그의 모든 퀘스트 id(정렬)를 노출합니다.</summary>
        public void OpenWithIds(string[] ids)
        {
            ResolveRefs();

            if (scrollContent == null || rowPrefab == null)
            {
                Debug.LogWarning(
                    "[QuestOfferView] scrollContent 또는 rowPrefab 이 없습니다. Tools/DataDrivenDemo 의 Build Quest Offer UI 로 생성했는지 확인하세요.");
                lastInteractorRoot = null;
                return;
            }

            offeredIds = ids ?? ResolveAllCatalogQuestIds();

            RootGo.SetActive(true);

            var cg = GetOrAddCanvasGroupOnRoot();

            StopOpenRevealRoutine();

            // 프롬프트 버튼 클릭과 같은 입력 프레임에 패널이 뜨면 이벤트가 배경 등으로 넘어가 바로 닫히거나 상태만 꼬일 수 있어 1프레임 뒤에 표시합니다.
            if (cg.alpha > 0.99f && cg.blocksRaycasts && cg.interactable)
            {
                ApplyOpenRevealState();
                return;
            }

            // alpha=0 이어도 blocksRaycasts 를 켜 두면 같은 캔버스의 프롬프트 버튼 클릭이 2번째로 새는 것을 막고,
            // 그 사이 들어오는 불필요한 포인터가 배경 종료까지 닿지 않게 합니다.
            cg.alpha = 0f;
            cg.blocksRaycasts = true;
            cg.interactable = false;

            openRevealRoutine = StartCoroutine(OpenRevealAfterInputClearsCoroutine());
        }

        public void Close()
        {
            StopOpenRevealRoutine();

            if (!IsOpen)
                return;

            RootGo.SetActive(false);
            SetGameplayLocked(false);
            selectedQuestId = null;
            lastInteractorRoot = null;

            foreach (var row in rows)
            {
                if (row != null)
                    row.SetRowSelected(false);
            }

            RestoreCanvasGroupForNextOpen();
        }

        private void StopOpenRevealRoutine()
        {
            if (openRevealRoutine != null)
            {
                StopCoroutine(openRevealRoutine);
                openRevealRoutine = null;
            }
        }

        private CanvasGroup GetOrAddCanvasGroupOnRoot()
        {
            var cg = RootGo.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = RootGo.AddComponent<CanvasGroup>();
            return cg;
        }

        private IEnumerator OpenRevealAfterInputClearsCoroutine()
        {
            yield return null;

            openRevealRoutine = null;

            if (!RootGo.activeInHierarchy)
                yield break;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            var cg = RootGo.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            ApplyOpenRevealState();
        }

        private void ApplyOpenRevealState()
        {
            SetGameplayLocked(true);
            selectedQuestId = null;

            RefreshList();
            if (detailText != null) detailText.text = "";
            if (hintText != null)
                hintText.text = "퀘스트를 선택한 뒤 수락할 수 있습니다.";

            if (acceptButton != null)
                acceptButton.interactable = false;

            foreach (var row in rows)
            {
                if (row == null || !row.gameObject.activeSelf) continue;
                SelectQuestId(row.QuestId);
                break;
            }
        }

        /// <summary>닫힌 뒤 다음 오픈에서 레이아웃/알파 기본값이 남지 않게 합니다.</summary>
        private void RestoreCanvasGroupForNextOpen()
        {
            var cg = RootGo.GetComponent<CanvasGroup>();
            if (cg == null)
                return;
            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        public void NotifyRowClicked(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return;
            SelectQuestId(questId);
        }

        private void SelectQuestId(string questId)
        {
            selectedQuestId = questId;
            foreach (var row in rows)
            {
                if (row == null || !row.gameObject.activeSelf) continue;
                row.SetRowSelected(string.Equals(row.QuestId, questId, System.StringComparison.Ordinal));
            }

            var sys = ResolveQuestSystem();
            if (sys != null && sys.TryGetOfferDetailText(questId, out var body) && detailText != null)
                detailText.text = body;

            if (acceptButton != null)
                acceptButton.interactable = sys != null && sys.CanAcceptOffer(questId);

            RebuildDetailLayout();
        }

        private void OnAcceptClicked()
        {
            if (string.IsNullOrWhiteSpace(selectedQuestId))
                return;

            var sys = ResolveQuestSystem();
            if (sys == null || !sys.CanAcceptOffer(selectedQuestId))
                return;

            if (!sys.Accept(selectedQuestId))
                return;

            var def = ResolveCatalog()?.Get(selectedQuestId);
            var name = def?.title ?? selectedQuestId;
            hud?.ShowToast($"퀘스트 수락: {name}");
            RefreshList();
            SelectQuestId(selectedQuestId);
        }

        private void RefreshList()
        {
            if (scrollContent == null || rowPrefab == null)
                return;

            var sys = ResolveQuestSystem();
            var cat = ResolveCatalog();

            var idList = offeredIds != null && offeredIds.Length > 0
                ? offeredIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                : ResolveAllCatalogQuestIds().ToList();

            foreach (var id in idList.ToList())
            {
                if (cat != null && cat.Get(id) == null)
                    idList.Remove(id);
            }

            while (rows.Count < idList.Count)
            {
                var go = Instantiate(rowPrefab.gameObject, scrollContent);
                var row = go.GetComponent<QuestOfferRowView>();
                if (row == null)
                {
                    Destroy(go);
                    break;
                }

                rows.Add(row);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;

                if (i >= idList.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var qid = idList[i];
                row.gameObject.SetActive(true);

                var def = cat?.Get(qid);
                var title = def?.title ?? qid;
                var status = sys != null ? sys.GetOfferStatusLabel(qid) : "";
                row.Bind(this, qid, title, status);
            }

            RebuildLayouts();
        }

        private void RebuildLayouts()
        {
            Canvas.ForceUpdateCanvases();
            if (scrollContent is RectTransform crt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
                var p = crt.parent as RectTransform;
                if (p != null) LayoutRebuilder.ForceRebuildLayoutImmediate(p);
                var gp = p?.parent as RectTransform;
                if (gp != null) LayoutRebuilder.ForceRebuildLayoutImmediate(gp);
            }

            RebuildDetailLayout();
        }

        private void RebuildDetailLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (detailText == null)
                return;

            var dre = detailText.rectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(dre);

            var scroll = detailText.GetComponentInParent<ScrollRect>();
            if (scroll != null && scroll.content != null && scroll.viewport != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.viewport);
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        private string[] ResolveAllCatalogQuestIds()
        {
            var cat = ResolveCatalog();
            if (cat == null) return System.Array.Empty<string>();

            cat.Rebuild();

            return cat.All().Select(x => x.id).Where(id => !string.IsNullOrWhiteSpace(id))
                .OrderBy(id => id, System.StringComparer.Ordinal).ToArray();
        }

        private void SetGameplayLocked(bool locked)
        {
            QuarterViewPlayerController playerController = null;
            ProximityInteractor proximityInteractor = null;

            if (lastInteractorRoot != null)
            {
                playerController = lastInteractorRoot.GetComponentInParent<QuarterViewPlayerController>();
                proximityInteractor = lastInteractorRoot.GetComponentInParent<ProximityInteractor>();
            }

            if (playerController == null)
                playerController = PlayerLocator.Controller;
            if (proximityInteractor == null)
                proximityInteractor = PlayerLocator.Interactor;

            if (playerController != null)
                playerController.SetMovementLock(QuarterViewMovementLockSource.QuestOffer, locked);

            if (proximityInteractor != null)
                proximityInteractor.enabled = !locked;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void ResolveRefs()
        {
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            if (catalog == null && questSystem != null)
                catalog = questSystem.GetComponent<QuestCatalog>();
            if (catalog == null)
                catalog = FindFirstObjectByType<QuestCatalog>(FindObjectsInactive.Include);
            if (hud == null)
                hud = FindFirstObjectByType<QuestHudView>(FindObjectsInactive.Include);

            PlayerLocator.Refresh();
        }

        private QuestSystem ResolveQuestSystem()
        {
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            return questSystem;
        }

        private QuestCatalog ResolveCatalog()
        {
            if (catalog == null && questSystem != null)
                catalog = questSystem.GetComponent<QuestCatalog>();
            if (catalog == null)
                catalog = FindFirstObjectByType<QuestCatalog>(FindObjectsInactive.Include);
            return catalog;
        }
    }
}
