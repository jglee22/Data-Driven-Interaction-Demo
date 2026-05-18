# Test Checklist

수동 검증용 체크리스트입니다. Unity **DemoScene** · **StartScene** 기준입니다.

**자동 테스트**: **Window → General → Test Runner → EditMode**에서 `QuestSystemTests`를 실행합니다.

## 저장·복원 시나리오 (수동)

| ID | 할 일 | 통과 기준 |
|----|--------|-----------|
| A | 수락·진행(예: 1/3) 후 씬/Play 재진입 | 진행도·수락 목록 유지 |
| B | StartScene **Continue** | A 상태로 DemoScene 진입 |
| C | **Edit → Clear All PlayerPrefs** → 동일 Firebase 계정 로그인 | Firestore에서 복원, HUD·저널 일치 |
| D | **New Game** 또는 F12 | 진행·수락 초기화 |
| E | 저장 없을 때 Continue | 버튼 비활성 |

## 저장·복원 (자동 · Edit Mode)

- [ ] `Accept_AddsRuntime` 통과
- [ ] `PickupEvent_AdvancesStepCount` 통과
- [ ] `TurnIn_SubmitAfterCompleted_SetsTurnedIn` 통과
- [ ] `Hydrate_RestoresAcceptedQuestFromPlayerPrefs` 통과
- [ ] `InteractableRegistry_PrefersNonGiver` 통과

## 퀘스트 플로우

- [ ] 의뢰 NPC에서 퀘스트 수락
- [ ] HUD 트래커에 진행 중 퀘스트 표시
- [ ] 목표 오브젝트 상호작용 시 진행도 증가
- [ ] 터미널 제출 시 완료 처리
- [ ] 보상 코인 표시·지급 확인

## 저장 / 불러오기

- [ ] 시나리오 A — 퀘스트 수락 후 재시작 시 수락·진행 복원
- [ ] 시나리오 A — 진행 중(예: 1/3) 상태에서 재시작 시 진행도 유지
- [ ] 완료 상태 재시작 후 유지
- [ ] 시나리오 C — PlayerPrefs 삭제 후 동일 Firebase 계정 → Firestore 복원
- [ ] 시나리오 D — New Game 또는 F12 후 진행·수락 목록 초기화
- [ ] 시나리오 B/E — 메인 메뉴 Continue: 저장 있을 때만 활성화

## UI

- [ ] 의뢰 패널 닫은 뒤 이동 잠금 해제
- [ ] Q 키 저널 열기/닫기
- [ ] 완료된 퀘스트는 저널에서 「포기」 버튼 숨김
- [ ] 월드 마커(?) / 의뢰(!) 표시·갱신

## Firebase (선택)

- [ ] StartScene 이메일 로그인 → DemoScene 진입
- [ ] Firestore `users/{uid}/quests`, `saves/quest_meta`에 데이터 생성
- [ ] 콘솔에 `FirestoreQuestSaveService` Save/Load failed 없음

## 빌드·보안

- [ ] StartScene에 비밀번호 「기억」 토글 없음 (PlayerPrefs 미저장, Firebase Auth 세션만)
- [ ] `google-services*.json`은 샘플 복사본으로 로컬만 구성 (저장소에 실키 미커밋)
