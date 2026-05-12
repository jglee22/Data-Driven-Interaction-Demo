using System;
using System.Collections.Generic;
using System.Linq;
using DataDrivenDemo.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataDrivenDemo.UI
{
    /// <summary>전체 퀘스트 목록 스크롤(+선택 시 상세). HUD 트래커는 최대 개수만 표시.</summary>
    [DisallowMultipleComponent]
    public sealed class QuestJournalView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform scrollContent;
        [SerializeField] private QuestTrackerRowView rowPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailBody;
        [SerializeField] private TMP_Text emptyHint;
        [SerializeField] private Button abandonButton;
        [Header("Confirm (optional)")]
        [SerializeField] private GameObject confirmRoot;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [SerializeField] private bool closeOnEscape = true;

        private readonly List<QuestTrackerRowView> rows = new();
        private string selectedQuestId;

        private GameObject RootGo => panelRoot != null ? panelRoot : gameObject;

        public bool IsOpen => RootGo.activeSelf;

        private void Update()
        {
            if (!closeOnEscape || !IsOpen)
                return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsConfirmOpen())
                    HideConfirm();
                else
                    Close();
            }
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            RootGo.SetActive(true);
            Refresh();
            if (scrollRect != null) scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        public void Close() => RootGo.SetActive(false);

        public void Refresh()
        {
            if (!IsOpen || scrollContent == null || rowPrefab == null)
                return;

            var list = QuestTrackerService.Items?
                           .Where(x => x != null)
                           .OrderBy(x => StatusRank(x.status))
                           .ThenByDescending(x => x.sortKey)
                           .ToList()
                       ?? new List<QuestTrackerItem>();

            if (emptyHint != null)
                emptyHint.gameObject.SetActive(list.Count == 0);

            if (list.Count == 0)
            {
                if (detailTitle != null) detailTitle.text = "";
                if (detailBody != null) detailBody.text = "";
                selectedQuestId = null;
                if (abandonButton != null) abandonButton.gameObject.SetActive(false);
            }

            while (rows.Count < list.Count)
            {
                var go = Instantiate(rowPrefab.gameObject, scrollContent);
                var row = go.GetComponent<QuestTrackerRowView>();
                if (row == null)
                {
                    Destroy(go);
                    Debug.LogError("[QuestJournalView] rowPrefab 에 QuestTrackerRowView 가 없습니다.");
                    break;
                }
                rows.Add(row);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;

                if (i >= list.Count)
                {
                    row.SetSelected(false);
                    row.gameObject.SetActive(false);
                    continue;
                }

                var it = list[i];
                row.gameObject.SetActive(true);
                row.Set(it.title, it.detail, it.status, it.questId);
                EnsureRowClickable(row);
            }

            RebuildScrollLayout();

            if (list.Count > 0)
                SelectQuest(list[0].questId ?? "", list[0].title ?? "", list[0].detail ?? "");
        }

        private static int StatusRank(QuestTrackerStatus status)
        {
            return status switch
            {
                QuestTrackerStatus.TurnInReady => 0,
                QuestTrackerStatus.Active => 1,
                QuestTrackerStatus.Completed => 2,
                _ => 99
            };
        }

        public void SelectQuest(string questId, string title, string body)
        {
            selectedQuestId = questId;
            var sys = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            if (sys != null && sys.TryGetJournalDetail(questId, out var t, out var b))
            {
                if (detailTitle != null) detailTitle.text = t ?? "";
                if (detailBody != null) detailBody.text = b ?? "";
            }
            else
            {
                if (detailTitle != null) detailTitle.text = title ?? "";
                if (detailBody != null) detailBody.text = body ?? "";
            }

            if (abandonButton != null)
                abandonButton.gameObject.SetActive(ShouldShowAbandonForSelectedQuest());

            ApplyRowSelection();
        }

        /// <summary>완료된 퀘스트는 포기할 수 없으므로 버튼을 숨깁니다.</summary>
        private bool ShouldShowAbandonForSelectedQuest()
        {
            if (string.IsNullOrWhiteSpace(selectedQuestId))
                return false;

            var it = QuestTrackerService.Items?
                .FirstOrDefault(x =>
                    x != null && string.Equals(x.questId, selectedQuestId, StringComparison.Ordinal));
            if (it == null)
                return true;

            return it.status != QuestTrackerStatus.Completed;
        }

        private void ApplyRowSelection()
        {
            foreach (var row in rows)
            {
                if (row == null || !row.gameObject.activeSelf) continue;
                var on = !string.IsNullOrEmpty(selectedQuestId) &&
                         string.Equals(row.QuestId, selectedQuestId, StringComparison.Ordinal);
                row.SetSelected(on);
            }
        }

        private void Awake()
        {
            if (abandonButton != null)
            {
                abandonButton.onClick.RemoveAllListeners();
                abandonButton.onClick.AddListener(AbandonSelected);
                abandonButton.gameObject.SetActive(false);
            }

            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.RemoveAllListeners();
                confirmYesButton.onClick.AddListener(ConfirmYes);
            }
            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.RemoveAllListeners();
                confirmNoButton.onClick.AddListener(HideConfirm);
            }

            HideConfirm();
        }

        private void AbandonSelected()
        {
            if (string.IsNullOrWhiteSpace(selectedQuestId))
                return;

            ShowConfirm();
        }

        private void ConfirmYes()
        {
            if (string.IsNullOrWhiteSpace(selectedQuestId))
            {
                HideConfirm();
                return;
            }

            var sys = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            if (sys == null)
            {
                HideConfirm();
                return;
            }

            sys.Abandon(selectedQuestId, clearSavedState: true);
            selectedQuestId = null;
            HideConfirm();
            Refresh();
        }

        private bool IsConfirmOpen()
        {
            return confirmRoot != null && confirmRoot.activeSelf;
        }

        private void ShowConfirm()
        {
            if (confirmRoot == null)
            {
                // confirm UI가 없으면 즉시 실행(레거시 씬)
                ConfirmYes();
                return;
            }

            if (confirmText != null)
                confirmText.text = "이 퀘스트를 포기하고 삭제할까요?\n(진행도/저장 데이터가 삭제됩니다)";
            confirmRoot.SetActive(true);
        }

        private void HideConfirm()
        {
            if (confirmRoot != null)
                confirmRoot.SetActive(false);
        }

        private void EnsureRowClickable(QuestTrackerRowView row)
        {
            if (row == null) return;
            var btn = row.GetComponent<Button>();
            if (btn == null) btn = row.gameObject.AddComponent<Button>();

            var img = row.GetComponent<Image>();
            if (img != null)
            {
                btn.targetGraphic = img;
                img.raycastTarget = true;
            }

            btn.onClick.RemoveAllListeners();
            var r = row;
            btn.onClick.AddListener(() => SelectQuest(r.QuestId, r.LastTitle, r.LastDetail));
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private void RebuildScrollLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (scrollContent is RectTransform crt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
                var p = crt.parent as RectTransform;
                if (p != null) LayoutRebuilder.ForceRebuildLayoutImmediate(p);
            }
        }
    }
}
