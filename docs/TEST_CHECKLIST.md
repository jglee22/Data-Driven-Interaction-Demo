# Test Checklist

수동 검증용 체크리스트입니다. Unity **DemoScene** / **StartScene** 기준입니다.

## 퀘스트 플로우

- [ ] 의뢰 NPC에서 퀘스트 수락
- [ ] HUD 트래커에 진행 중 퀘스트 표시
- [ ] 목표 오브젝트 상호작용 시 진행도 증가
- [ ] 터미널 제출 시 완료 처리
- [ ] 보상 코인 표시/지급 확인

## 저장 / 불러오기

- [ ] 퀘스트 수락 후 재시작 시 수락/진행 복원
- [ ] 진행 중(예: 1/3) 상태에서 재시작 시 진행도 유지
- [ ] 완료 상태 재시작 후 유지
- [ ] **Edit → Clear All PlayerPrefs** 후 동일 Firebase 계정 로그인 → Firestore에서 복원
- [ ] New Game 또는 F12 후 진행/수락 목록 초기화
- [ ] 메인 메뉴 Continue: 저장 있을 때만 활성화

## UI

- [ ] 의뢰 패널 닫은 뒤 이동 잠금 해제
- [ ] Q 키 저널 열기/닫기
- [ ] 완료된 퀘스트는 저널에서 「포기」 버튼 숨김
- [ ] 월드 마커(?) / 의뢰(!) 표시/갱신

## Firebase (선택)

- [ ] StartScene 이메일 로그인 → DemoScene 진입
- [ ] Firestore `users/{uid}/quests`, `saves/quest_meta`에 데이터 생성
- [ ] 콘솔에 `FirestoreQuestSaveService` Save/Load failed 없음

## 빌드/보안

- [ ] **릴리즈 빌드**에서 비밀번호 「기억」 토글 UI 비표시
- [ ] `google-services*.json`은 샘플 복사본으로 로컬만 구성 (저장소에 실키 미커밋)
