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

`ProjectSettings/ProjectVersion.txt`의 `m_EditorVersion`과 워크플로 `unityVersion`이 일치해야 합니다(현재 **6000.4.2f1**).
