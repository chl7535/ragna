## 최신: 라그나르 가로 전용 전환

사용자 확정: 세로 화면은 앞으로 지원하지 않는다. AGENTS.md에도 기록했다.
1920×1080(16:9) 기준, 고정 높이 카메라로 20:9 이상 좌우 배경 확장. 모바일 LandscapeLeft, 세로 방향 둘 다 비활성.
씬 PixelPerfectCamera는 320×180/16PPU, UI와 타이틀 CanvasScaler는 1920×1080/높이 기준 크기 조절.
Resources/UI/LandscapeHUD.prefab으로 상단 HP·진행/하단 상태·물약·보호막·준비·성장·정지를 편집한다.
Resources/UI/LandscapeDisplay.asset에서 공통 기준과 배경 전투 영역·카메라 크기를 편집한다.
타이틀은 전체 16:9 원화를 보존하며 SafeArea 적용. 더 넓은 화면의 타이틀은 좌우 여백, 전투는 배경 전체 채움.
모달과 준비창 재배치, 타이틀/HUD Safe Area, 가로 캡처 명령과 Android 검증 도구 반영.
16:9/20:9에서 가로설정14·타이틀13 통과. UI31·준비창13·자동사냥20·시각72 통과.
6개 배경은 16:9/20:9/21:9 범위 검사42개 통과. 갤러리도 가로 미리보기로 갱신했다.
Screenshots/landscape-title-16x9.png, landscape-battle-16x9.png, landscape-battle-20x9.png 참고.
DISPLAY_LAYOUT.md에 편집 방법을 정리했다. 과거 세로 설명과 출력은 현재 설정이 아니다.
기존 APK/iOS 내보내기는 이전 세로 빌드다. 새 모바일 빌드·실기기 노치/터치/회전 검증·GitHub 업로드는 미실행.
## 최신: 사용자 이미지 첫 화면 적용

사용자 제공 원본 PNG를 Resources/Title/TitleArtwork.png로 복사하고 TitleScreen.prefab에 연결했다.
게임 실행 시 저장 여부와 관계없이 첫 화면을 먼저 표시한다. START만 새 시작/이어가기로 연결한다.
SETTINGS/CREDITS/QUIT는 투명한 실제 Button과 눌림 효과만 제공하며 기능은 없다. QUIT도 종료하지 않는다.
세로 방향을 유지하며 중앙 로고·메뉴를 포함하는 75% 높이 뷰포트와 위아래 여백을 적용했다.
첫 화면에 머무르거나 앱을 닫아도 아직 열지 않은 저장 도전이 지워지지 않도록 저장을 막는다.
프리팹에서 배경·뷰포트·버튼 RectTransform과 색상을 수정한다. TITLE_SCREEN.md 참고.
타이틀13(실제 UI 레이캐스트 포함) 및 기존 게임 UI31 검사 통과. 실제 화면은 Screenshots/title-screen.png.
최신 모바일 재빌드와 실기기 검증은 아직 하지 않았다. GitHub에는 이번 변경을 업로드하지 않았다.
## 최신: 검사 루트 외 직업 제거

사용자가 다른 직업을 삭제하도록 명시적으로 요청했다. 모험가→검사→기사/광전사만 남겼다.
다른 직업의 enum 항목·이름·스킬·패시브·전투 보너스·전용 스킬 연출·무기 모양·선택지·미리보기 이미지24개를 제거했다.
기존 ID0/7은 유지하며 폐기ID1~6은 재사용하지 않는다. 첫 전직·캠프·계승 도감도 검사 루트만 표시한다.
저장버전7: 이전 타 직업은 LV20 미만 모험가/이상 검사로 전환, 기존 2·3차 승급은 기사로 연결한다.
레벨·층·전승 횟수·슬롯과 검사 패시브는 보존하며 타 직업 패시브는 제거한다.
CareerDefinition/CombatStats/CombatActionDefinition과 직업 등록 목록은 향후 확장용으로 유지했다. JOB_EXTENSION.md 참고.
검사루트/저장변환32, 모델59, 자동사냥20, 능력치11, UI31, 시각72 검사 통과. 검사 스킬4단계 렌더 확인.
첫 전직 화면은 Screenshots/warrior-only-promotion.png. 검사 외 선택지가 없는 것을 확인했다.
모바일 출력 재빌드·실기기·GitHub 업로드는 아직 하지 않았다. 과거 기록의 다른 직업 관련 설명은 현재 구현에 해당하지 않는다.
## 모험가 → 검사 전용 편집과 전투 표시

사용자 지침: 모험가 → 검사 루트를 먼저 완성하고 다른 직업은 수정하지 않는다. AGENTS.md에 개발 범위를 기록했다.
Resources/CombatStats/Adventurer.asset, Warrior.asset에서 기본 피해·레벨당 증가·초당 공격 횟수·스킬 배율을 편집한다.
모험가·검사에만 새 능력치 프로필을 연결했으며 다른 직업의 기존 계산은 유지했다.
Resources/CombatUI/EnemyHealth.prefab: 적 현재HP/최대HP와 체력 막대. 겹치는 적 표시는 위로 비켜 배치한다.
Resources/CombatUI/DamageNumber.prefab 및 CombatText.mat: 피해 숫자·기본 크기·외곽선 편집.
ragna_warrior_authoring은 없는 자산만 생성하며 기존 편집을 보존한다. 능력치·자산 검사11 / 검사 자동사냥20 통과.
실제 전투에서 적 HP 2개(9789/10000,10000/10000)와 활성 피해 숫자3개를 확인했다.
사용법은 WARRIOR_EDITING.md. 화면은 Screenshots/warrior-damage-health.png.
모바일 재빌드·기기 검증 및 GitHub 업로드는 이번에 수행하지 않았다.
## 2026-10-29 최신: 턴 폐기 · 자동사냥과 바닥 위아래 이동

플레이어와 적은 바닥 X/Y 좌표를 사용하며, 가까운 적에게 자동 접근한다. 점프는 없다.
좌우 사거리와 깊이 폭이 모두 맞아야 공격하며 슬롯 순서대로 반복한다. 적은 독립 주기로 공격한다.
그림자·몸체·방어막이 이동을 따르고 바닥 Y에 따라 앞뒤를 정렬한다. 세로 화면은 유지했다.
Inspector 설정: Assets/Ragna/Resources/HuntingMap.asset. 경계·속도·검사 사거리·깊이 폭·적 주기·첫 공격 대기·보스 체력/피해를 수정한다.
저장 버전6: 위치·방향·콤보·적 타이머 보존, 이전 저장 바닥 좌표 변환.
자동사냥20 / 게임 규칙71 / 전체 UI31 / 준비창13 검사 통과. 고정 시드에서 적극 조작은 승리, 방치는 사망.
검증 화면: Screenshots/auto-hunting-depth.png, auto-hunting-contact.png. 자세한 내용 AUTO_HUNTING.md.
이전 TURN_CHECKS.txt와 아래 턴 교대 기록은 과거 규칙이다. 현재 명령은 ragna_hunting_checks.
현재는 장애물 없는 바닥 구역이다. 검사 중심 개발을 계속하며 다른 직업의 전용 이동·사거리는 추후 진행한다.
모바일 출력은 여전히 이전 버전이다. 최신 재빌드와 실기기 검증이 남아 있다. 변경 파일은 로컬에 유지했으며 GitHub에는 아직 올리지 않았다.
## 2026-10-23 슬롯별 단일 타깃 수정

각 슬롯의 연타·스킬 피해는 한 적에게 고정하며, 처치 후 남은 타격은 종료한다. 다음 슬롯에서 뒤의 적을 새로 선택한다.
모험가와 일곱 직업의 생존 타깃 고정·처치 시 피해 전이 방지·다음 슬롯 재선택 검사 포함 턴 검사52개, 전체 규칙71개 통과.

## 2026-10-23 전투 준비와 턴 교대 적용

사용자 확정: 내 턴에 전체 슬롯 순차 실행 후 적 한 번 공격. 여러 칸 스킬은 한 턴에 한 번 발동.
기본1 + 첫 전직1 + 전승 횟수 영구 슬롯. 60·100레벨 슬롯 추가 없음.
준비창 배치·점유 표시·스크롤·페이지, 1/2/3/6칸 스킬 데이터, 중간 콤보 보존, 저장 버전5 적용.
검사 통과: 게임 규칙71 / 턴·슬롯28 / 전체 UI31 / 준비창13.
검사 기록은 MODEL_CHECKS.txt, TURN_CHECKS.txt, UI_CHECKS.txt, PREPARATION_UI_CHECKS.txt.
상세 규칙은 COMBAT_SLOTS.md, 실제 준비창은 Screenshots/combat-preparation-3slots.png.
최신 모바일 재빌드·실기기·장기 밸런스 검증은 남아 있다. 이 변경은 로컬 파일에 저장했다.
GitHub master의 이전 저장 커밋 b3673b3에는 이번 전투 변경이 포함되지 않는다.
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
