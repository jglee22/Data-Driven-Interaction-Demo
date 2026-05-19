# CI — Unity Edit Mode 테스트

[`.github/workflows/unity-editmode.yml`](../.github/workflows/unity-editmode.yml)에서 **Edit Mode** `QuestSystemTests`를 실행합니다.

## 필요한 GitHub Secrets

[game-ci](https://game.ci/docs/github/test-runner/) 요구 사항에 맞게 저장소에 다음 시크릿을 등록합니다.

| Secret | 설명 |
|--------|------|
| `UNITY_LICENSE` | Unity Personal/Plus 라이선스(ULF) 본문 |
| `UNITY_EMAIL` | Unity 계정 이메일 |
| `UNITY_PASSWORD` | Unity 계정 비밀번호 |

시크릿이 없으면 워크플로가 실패합니다. 포크·개인 저장소에서 CI를 끄려면 워크플로 파일을 제거하거나 `on:` 조건을 조정하세요.

## 로컬에서 동일 검증

**Window → General → Test Runner → EditMode** → `QuestSystemTests` 실행.

## Unity 버전

워크플로는 `unityVersion: auto`로 **`ProjectSettings/ProjectVersion.txt`** 와 맞춥니다.

## “docker exit code 2” 만 보일 때

GitHub는 Docker 단계 실패 시 **한 줄 요약**만 보여 주는 경우가 많습니다.

1. 실패한 실행 → **`EditMode` 잡** → **`Run tests`** 단계를 펼친 뒤 **위에서부터 스크롤**해 `Error` / `Exception` / `activation` / `license` 문구를 찾습니다.
2. 실행 페이지 **맨 아래 Artifacts**에서 **`unity-test-results`** ZIP을 받아 압축을 풀면 Unity 로그·테스트 결과가 들어 있는 경우가 많습니다.
3. 더 자세한 Actions 로그가 필요하면 저장소 **Settings → Secrets and variables → Actions → Variables** 에서  
   `ACTIONS_STEP_DEBUG` = `true` 를 추가한 뒤 워크플로를 다시 실행합니다. (디버그 후에는 끄는 것을 권장합니다.)
