# GitHub 저장 요청 — 2026-10-23

사용자가 https://github.com/chl7535/ragna.git 에 현재 프로젝트를 저장하도록 승인했다.
이전 커밋 보류 요청은 이번 저장 요청으로 해제되었다. 아래 기록의 보류 문구는 과거 상태다.
Git 작성자는 저장소 소유자 계정 chl7535와 GitHub noreply 주소를 로컬 저장소에만 설정한다.
Unity 캐시, 빌드 바이너리, 에디터 복구 파일과 서명 키는 업로드하지 않는다.

# 2026-10-23 최신 전직 구조

- 모든 새 도전과 환생 후 도전은 모험가 LV.1로 시작한다.
- 20레벨에 일곱 직업 선택, 검사 60레벨에 기사/광전사, 100레벨 승급.
- 직업 enum의 기존 0~6 ID는 보존하고 Adventurer=7을 추가했다. 패시브 배열은 일곱 직업 기준 그대로 유지한다.
- RunModel.Start(seed)는 모험가로만 시작한다. ChooseJob은 20레벨 전직 상태에서 한 번만 허용.
- 기사·광전사 자산을 60레벨/승급 단계2로 이동. 새 CareerDefinition도 기본60/2.
- 저장 버전4. 기존 저레벨은 모험가로 전환, 20레벨 이상 기존 직업/전직은 보존. 구버전 전직 대기 상태 복원도 검사했다.
- 컴파일 성공. 게임 규칙68 / UI24 검사 통과. 시작/첫 전직 화면은 Docs/Screenshots/adventurer-camp.png, first-job-choice.png.
- 모바일 출력은 최신 그래픽·배경·전직 구조 이전 빌드다. 다시 빌드해야 한다. 커밋은 계속 보류.

--- 이전 작업 기록 ---
# 작업 재개 메모 — 2026-10-20

## 최신 연출 변경
사용자 요청으로 애니메이션·스킬을 먼저 진행하고 기기 검증은 이후로 미뤘다.
대기·이동·공격·시전·피격·사망·보호막 프레임, 적 동작, 직업별 4단계 스킬 연출을 적용했다.
VisualChecks 168개, 28개 성장 렌더 검사 통과. 전투 규칙 46개와 UI 20개도 통과했다.
미리보기는 Docs/SkillGallery/index.html, animations.html, character-animations.gif.
기존 APK/iOS 출력에는 최신 애니메이션이 없다. 기기 테스트 전에 다시 빌드할 것.

## 이전 완료
- 세로 화면 + 좌→우 횡스크롤 전투. 적 이동·타깃·근접 판정·공격·스킬·배경 적용.
- 저장 버전 3으로 기존 버전 1·2 수직 좌표를 변환한다. 성장·전직·환생은 유지.
- Unity 컴파일, 게임 규칙 46개 및 UI 흐름 20개 검사 통과. side-*.png는 실제 에디터 화면.
- Android APK 빌드 성공: Ragna/Builds/Android/Ragna.apk (36,908,223바이트).
- APK 서명(v2, Android Debug), 앱 ID, ARM64 IL2CPP, 최소 API 26, 세로 방향 검증 통과.
- iOS Xcode 프로젝트 내보내기 성공: Ragna/Builds/iOS/Unity-iPhone.xcodeproj.
- iOS 게임 소스·데이터·앱 ID·최소 iOS 15·세로 방향 확인. Mac Xcode 컴파일·서명은 아직 하지 않음.
- Docs/APK_VERIFICATION.json, IOS_EXPORT_VERIFICATION.json, BUILD_Android.json, BUILD_iOS.json에 기록.
- 커밋은 사용자 요청으로 계속 보류한다. Git 작성자 정보도 설정하지 않는다.

## 다음 개발
- 실제 Android/iPhone 터치·발열·프레임·저장 복원 테스트 (현재 연결 기기 없음).
- 원본 임시 도트·사운드를 최종 애니메이션·스킬 아트로 교체.
- 직업 상세 루트는 사용자와 함께 결정. 현재 검사 20레벨 기사·광전사는 데이터로 추가 가능.
- 장기 밸런스와 플레이 분량 확장. 현재 고정 시드 검증 봇 기준 도전 5~6분.
- 스토어 키·실제 앱 ID·아이콘·출시 준비는 미완료. 현재는 플레이 가능한 0.1.0 테스트 버전.

## 환경
Unity 6000.3.26f1 LTS / URP 2D / Android+iOS, 프로젝트 Ragna/.
Tools/UnityCli/unity.exe beta.13을 사용한다.
원본 한글 경로 때문에 Tools/Build-Mobile.ps1이 %TEMP%/RagnaBuild-01a14433으로 입력만 복사해 빌드한다.
Library 캐시는 보존하며 중단 후 남은 잠금은 배타적 열기 확인 후에만 정리한다.
빌드: ./Tools/Build-Mobile.ps1 -Target Android 또는 -Target iOS (동일 임시 폴더를 순차 사용).
APK 검증: ./Tools/Verify-Android.ps1.
Docs/DEVICE_TESTING.md에 설치·실기기 확인 방법, Docs/CAREER_AUTHORING.md에 전직 확장 방법.


첨부 원화 적용 완료: 검사 28프레임, ReferenceHero.cs와 Resources/Heroes/AdventurerSheet.png. 시각 검사 196개 통과. 전직별 새 갑옷·무기 애니메이션은 아직 필요. 자세한 프롬프트·경로·분리 규격은 Docs/Art/README.md.

사용자 제공 배경 6종 적용 완료: 1~5층 초원, 6~10층 숲, 11~15층 해안, 16~20층 폐성, 21~25층 화산, 26~30층 마성. PanoramaView가 세로 영역과 길 높이를 맞추고 이미지 안쪽에서 패닝한다. 반복 타일/레이어 패럴랙스는 아님. BACKGROUND_CHECKS 42개 및 기존 전투46/UI20 통과. Docs/Art/BACKGROUNDS.md와 Docs/BackgroundGallery/index.html 참고. 기존 APK/iOS는 새 배경이 포함되지 않은 이전 빌드.
