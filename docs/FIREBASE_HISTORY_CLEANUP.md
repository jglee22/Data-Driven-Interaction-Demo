# Firebase 설정 파일 — Git 히스토리에서 완전히 제거하기

과거에 `Assets/google-services.json` 또는 `Assets/StreamingAssets/google-services-desktop.json` 이 **커밋된 적이 있으면**, 지금 `.gitignore`에 넣어도 **이전 커밋에서 내용을 읽을 수 있습니다**.  
민감 정보가 들어 있었다면 **히스토리 재작성** 후 협업자에게 **새로 clone** 하도록 안내하는 것이 안전합니다.

> **주의:** 아래 작업은 **force push**가 필요하고, 모든 브랜치·PR·fork와 충돌할 수 있습니다. 백업·팀 합의 후 진행하세요.

## 전제

- [git-filter-repo](https://github.com/newren/git-filter-repo) 설치 (권장)  
  또는 [BFG Repo-Cleaner](https://rtyley.github.io/bfg-repo-cleaner/)

## git-filter-repo 예시 (로컬에서)

```bash
# 새 작업 디렉터리에서 클론 (mirror)
git clone --mirror https://github.com/OWNER/REPO.git
cd REPO.git

# 히스토리에서 해당 경로만 제거 (파일이 존재하던 모든 커밋에서 삭제)
git filter-repo --force \
  --path Assets/google-services.json \
  --path Assets/StreamingAssets/google-services-desktop.json \
  --invert-paths

# 원격에 반영 (모든 브랜치를 덮어씀)
git push --force --mirror origin
```

`--invert-paths` 는 나열한 `--path` 들을 **히스토리 전체에서 제거**합니다.

## 이후 할 일

1. **GitHub** 등에서 오래된 캐시·fork 가 있다면 각자 `git fetch` / 재clone.  
2. 로컬에는 **`.sample` 복사**만 커밋하고, 실제 JSON은 각자 머신에만 둡니다 (`README.md` 참고).  
3. 이미 노출된 키가 있다면 **Firebase 콘솔에서 키·앱 제한 재발급**을 검토합니다.

## 현재 추적 여부 확인

```bash
git log --oneline -- Assets/google-services.json
git log --oneline -- Assets/StreamingAssets/google-services-desktop.json
```

로그에 커밋이 나오면 위 절차 대상입니다. **비어 있으면** 히스토리 정리는 필요 없고, 앞으로만 `.gitignore` + 샘플 복사로 유지하면 됩니다.
