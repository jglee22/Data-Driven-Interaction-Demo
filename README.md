# Data-Driven-Interaction-Demo

3D 공간 상호작용을 데이터(JSON/CSV)로 제어하고 퀘스트·UI·보상·진행도를 연동하는 Unity 데모 프로젝트(Firebase 저장/랭킹, Photon 선택).

## 목표

- 3D 공간 탐색 + 오브젝트 상호작용
- 데이터 기반(외부 파일 수정만으로) 콘텐츠/퀘스트 흐름 제어
- UI 상태(안내/진행도/보상) 실시간 반영
- Firebase 연동(로그인/저장/랭킹)으로 “외부 서비스 연결” 증명
- (선택) Photon으로 간단한 멀티플레이 동기화

## 핵심 기능(예정)

- **탐색**: 모바일/PC 입력 대응(터치/키보드+마우스)
- **상호작용**: `IInteractable` 기반(대화/획득/트리거 등 기능 분리)
- **데이터 구동**: JSON/CSV로 오브젝트/퀘스트/보상/문구 제어
- **퀘스트**: 상호작용 이벤트 → 조건 체크 → 진행도 갱신 → 보상 지급
- **UI/UX**: UGUI + TMP, 안내/퀘스트/보상/랭킹 화면 구성
- **Firebase**: 로그인, 진행도 저장/로드, 랭킹(리더보드) 및 간단 로그
- **Photon(선택)**: 룸 입장 + 위치/상태 등 최소 단위 동기화

## 기술 스택

- **Unity**: 6000.4.2f1
- **UI**: UGUI, TextMeshPro
- **Data**: JSON/CSV
- **Backend**: Firebase, Photon (선택)

## 실행 방법

1. Unity Hub에서 이 프로젝트를 열고 플레이.
2. (Firebase 사용 시) `Firebase` 콘솔 프로젝트 생성 후, Unity SDK를 추가하고 설정 파일을 적용.
3. (Photon 사용 시) Photon AppId를 설정 후, 룸 생성/입장 기능을 확인.

## 문서/데모

- **퀘스트·의뢰 데모 설정**: [docs/QUEST_DEMO.md](docs/QUEST_DEMO.md) (씬 체크리스트, Wire, 마커, 단축키)
- 데모 영상/스크린샷: 추후 추가
- 데이터 스키마/샘플(JSON/CSV): 추후 추가
