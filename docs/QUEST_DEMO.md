# 퀘스트/의뢰 데모 설정 가이드

`DemoScene` 기준으로 **멀티 퀘스트(`QuestSystem`)**, **의뢰 UI**, **저널/트래커**, **월드 마커**를 사용할 때의 체크리스트와 역할 정리입니다.

---

## 빠른 체크리스트 (씬 새로 맞출 때)

1. 씬에 **`EventSystem`**이 있는지 확인합니다. 없으면 UI 클릭/수락이 동작하지 않을 수 있습니다.
2. 메뉴 **`Tools → DataDrivenDemo → Build Quest Offer UI`**로 의뢰 패널을 생성합니다. 이미 있으면 생략합니다.
3. **`Tools → DataDrivenDemo → Wire Quest Offer + npc_010 Giver`**를 실행합니다.  
   - `npc_010`이 없으면 `npc_001` 옆에 **`NPC_QuestGiver_010`**이 생성되고, **`npc_001`은 Talk용 `NpcInteractable`로 유지**됩니다.
4. (선택) **`QuestObjectiveWorldMarkerManager`** 오브젝트를 추가한 뒤 스프라이트를 지정합니다.  
   - 의뢰 NPC만 아이콘을 쓰려면 **`Quest Giver Only`**에 해당 `QuestGiverInteractable` 하나만 넣습니다.
5. 플레이어 루트에 **`QuarterViewPlayerController`** + **`ProximityInteractor`** + 트리거 콜라이더를 둡니다.  
   - Tag **`Player`**를 권장합니다(없어도 `QuarterViewPlayerController`로 찾습니다).  
   - 카메라/UI/의뢰 패널은 **`PlayerLocator`**가 시작 시 플레이어를 자동 연결합니다. 인스펙터에 플레이어를 끌어다 넣을 필요는 없습니다.
6. (권장) 빈 GameObject에 **`GameplaySceneContext`**를 추가하고 `QuestSystem`/`QuestHud`/`QuestJournal`/`QuestObjectiveWorldMarkerManager`를 연결합니다. 선택으로 **`QuestOfferView`**·**`ProximityInteractor`**를 연결하면 Find 폴백을 더 줄입니다. 없어도 Awake에서 1회 탐색으로 동작합니다.

---

## 런타임: QuestSystem vs QuestManager

| 구분 | 사용 |
|------|------|
| **`QuestSystem` + `QuestCatalog`** | 데모/포폴 **본선** (멀티 퀘스트, 수락 목록, 트래커/저널/마커) |
| **`QuestManager`** (`Quest/Legacy/`) | 단일 JSON 샘플. `QuestSystem`이 있으면 자동 비활성. 자세한 내용은 [LEGACY.md](LEGACY.md) |

메인 메뉴 **New Game** / **Continue**는 `QuestCatalog` 전체/수락 목록(`ddidemo.quest.accepted`) 기준입니다(`quest_001` 단일 키가 아님).

---

## 역할 분리 (중요)

| 대상 | 역할 |
|------|------|
| **`npc_001`** 등 일반 NPC | `NpcInteractable` — `Talk` 이벤트로 퀘스트 진행 (예: `quest_001` 첫 목표). |
| **`npc_010`** (또는 `NPC_QuestGiver_010`) | `QuestGiverInteractable` — 의뢰 목록 UI만 열고 Talk 이벤트는 발생시키지 않음. |
| **`offeredQuestIds`** | 비어 있으면 카탈로그 전체가 의뢰 목록에 나올 수 있음. Wire는 **`quest_001`~`quest_005`**로 채웁니다. |

**같은 오브젝트에 `QuestGiverInteractable`과 `NpcInteractable`을 동시에 두면** Talk 목표가 깨집니다.

---

## 에디터 메뉴 (`Tools / DataDrivenDemo`)

| 메뉴 | 설명 |
|------|------|
| **Build Quest Offer UI** | `QuestOffer` 루트/스크롤/상세/버튼 등을 생성합니다. Canvas에 `GraphicRaycaster`가 없으면 추가합니다. |
| **Wire Quest Offer + npc_010 Giver** | 의뢰 NPC에 `QuestGiverInteractable`과 Offer를 연결하고, `offeredQuestIds` 1~5를 채우며, `QuestDebugAccepter`의 F1~F5 단축 수락을 끕니다. |
| **Build Quest Journal UI** | 저널 UI(목록/상세/포기/확인)를 생성합니다. |
| **Build Quest Tracker UI** | HUD 트래커를 생성합니다. |
| **Build Test Interactables (002~005)** | `npc_002`~`005` 등 테스트용 오브젝트를 배치합니다. |

---

## 플레이 중 단축키/동작

- **`E`**: 프롬프트가 떠 있을 때 상호작용합니다(클릭과 동일).
- **`F12`** (`QuestDebugAccepter`가 씬에 있을 때): 퀘스트 전체 리셋과 저장 정리를 수행합니다.  
  - Wire 후 **`acceptShortcutKeys`**가 꺼져 있어도 **F12는 동작**합니다.
- **의뢰 패널**: `Esc`로 닫고, 배경 클릭으로 닫으며, **완료된 퀘스트는 저널에서 「퀘스트 포기」가 비표시**됩니다.

---

## 월드 마커 (`QuestObjectiveWorldMarkerManager`)

- **진행 목표**: **수락한 퀘스트가 있을 때만** 현재 단계 `targetId`와 같은 `Interactable` 위에 **`Quest Objective Sprite`**(예: `bonus_01`)를 표시합니다.
- **의뢰 NPC**: `QuestGiverInteractable` 위에 **`Quest Giver Sprite`**(예: `bonus_02`)를 표시합니다. 진행 마커와 같은 위치여도 **의뢰 아이콘이 위에 그려지도록** 정렬 순서를 더 높게 둡니다.
- **`Quest Giver Only`**: 비어 있으면 씬의 **모든** `QuestGiverInteractable`에 의뢰 아이콘이 붙습니다. **한 명만** 쓰려면 배열에 그 컴포넌트만 넣습니다.
- **`QuestSystem`**이 UI를 갱신할 때 씬의 매니저들에게 자동으로 `RefreshFrom`을 호출합니다(`QuestSystem`에 매니저를 직접 연결하지 않아도 됨).

---

## 데이터/저장

- 퀘스트 정의: `Assets/Data/Json/quest_*.json`, 카탈로그는 **`QuestCatalog`** 컴포넌트의 배열에 등록합니다.
- 수락 목록/진행: `QuestSystem` + `SaveServices.QuestSave`(기본 `PlayerPrefs`, Firebase 사용 시 `FirestoreQuestSaveService`).
- **시작 시 복원**: `QuestSystem`이 `Start`에서 비동기로 수락 목록/각 퀘스트 상태를 불러온 뒤(`IsHydrated`) HUD/마커를 갱신합니다. Firestore 사용 시 수락 목록은 `users/{uid}/saves/quest_meta` 문서의 `accepted` 필드에도 저장됩니다.
- **Continue(메인 메뉴)**: `QuestDemoSaveHelper.HasAnySavedProgressAsync`로 로컬/Firestore 저장 존재 여부를 확인합니다.
- **리셋 후 재시작 시 빈 상태**가 되려면 F12 등으로 **`ResetAllQuests(clearSavedStates: true)`**가 호출되어야 합니다(카탈로그에 있는 퀘스트 id의 로컬/클라우드 진행도까지 지움).

---

## 관련 스크립트 (참고)

| 영역 | 주요 파일 |
|------|-----------|
| 런타임 퀘스트 | `Assets/Scripts/Quest/QuestSystem.cs`, `QuestCatalog.cs`, `QuestTrackerService.cs` |
| 의뢰 UI | `Assets/Scripts/UI/QuestOfferView.cs`, `QuestOfferBackdropCloser.cs`, `QuestOfferRowView.cs` |
| 상호작용 | `Assets/Scripts/Interaction/ProximityInteractor.cs`, `QuestGiverInteractable.cs`, `NpcInteractable.cs` |
| 플레이어 잠금 | `Assets/Scripts/Player/QuarterViewPlayerController.cs` (`SetMovementLock`), `GameplayInputLock.cs` |
| 마커 | `Assets/Scripts/Quest/QuestObjectiveWorldMarkerManager.cs`, `QuestFloatingMarker.cs` |
| 저널 | `Assets/Scripts/UI/QuestJournalView.cs` |
| 에디터 빌드 | `Assets/Editor/QuestJournalUiBuilder.cs` (`QuestOfferUiBuilder` 포함) |

문제가 발생하면 **콘솔 경고**(`QuestOfferUiBuilder`, `QuestOfferView`, Wire 등)를 먼저 확인합니다.
