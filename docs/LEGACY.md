# 레거시 코드

## QuestManager (`Assets/Scripts/Quest/Legacy/QuestManager.cs`)

- **단일 JSON** 퀘스트 샘플용 스크립트입니다.
- `DemoScene`의 본선 런타임은 **`QuestSystem` + `QuestCatalog`**입니다.
- 같은 씬에 `QuestSystem`이 있으면 `QuestManager`는 Awake에서 자동 비활성화됩니다. 씬의 `QuestManager` 오브젝트는 참고용으로만 두었으며 기본 **비활성**입니다.

신규 기능·포폴 시연에는 `QuestSystem`을 사용합니다.
