# Ragna

세로 화면에서 좌→우로 전투하는 횡스크롤 모바일 도트 RPG. Android / iOS 공통 소스, 광고·결제 없음.

사용자 제공 배경 6종을 5층 단위로 적용했습니다. `Docs/BackgroundGallery/index.html`에서 적용 화면을 확인할 수 있습니다.

캐릭터 7개 동작 프레임과 직업별 4단계 스킬 연출을 적용했습니다.
`Docs/SkillGallery/animations.html`과 `index.html`에서 움직임과 28개 성장 상태를 볼 수 있습니다.
현재 APK/iOS 출력은 애니메이션 보강 전 버전입니다. 기기 검증은 최신 소스를 다시 빌드한 후 진행합니다.

## 프로젝트 열기

Unity 프로젝트 폴더는 `Ragna/`이며 Unity **6000.3.26f1 LTS**를 사용합니다.

```powershell
.\Tools\unity.ps1 open .
```

게임 씬: `Assets/Ragna/Scenes/Ragna.unity`. Play를 눌러 모험가로 시작합니다. 20레벨에 첫 직업을 선택합니다.
터치 또는 마우스로 물약과 회피 보호막을 사용합니다. PC에서는 Q: 물약, W: 보호막, Esc: 일시정지입니다.
첫 실행에는 조작 안내가 나옵니다. 전투는 자동이고 앱을 떠나면 일시정지됩니다.

## 게임 구조

- 시작 직업 모험가와 전직 직업 일곱 종류, 30층, 5층마다 보스, 층 클리어 시 세 개 중 하나를 고르는 무작위 강화.
- 20레벨에 검사/궁수/법사/거너/격투가/성직자/도적 중 선택합니다. 검사는 60레벨에 기사/광전사로 전직하며 100레벨에 승급합니다. 다른 세부 분기는 추후 함께 결정합니다.
- 스킬 4·8·12레벨에 시각적 진화. 승급 시 장비와 오라 변화.
- 100레벨 3차 승급 이후 환생해 현재 직업 패시브를 영구 계승(각 5단계).
- 환생당 기본 HP +2%, 공격력 +2.5%. 다음 도전은 모험가로 시작하고 20레벨에 다른 직업을 선택할 수 있습니다.
- 사망 시 현재 도전 종료. 영구 계승과 최고 기록 유지.

`RunModel`은 전투와 성장, `RagnaGame`은 화면 흐름, `BattleView`는 도트와 이펙트,
`GameUI`는 터치 UI, `SaveStore`는 로컬 저장을 담당합니다.

## 개발 명령

Unity가 열리고 Pipeline이 준비되면 저장소 루트에서 실행합니다.
`Tools/unity.ps1`은 프로젝트 전용 CLI beta.13을 사용하고 작업 폴더를 자동으로 맞춥니다.
다른 PC에서는 먼저 `Tools/Setup-Cli.ps1`을 실행합니다(공식 파일 다운로드와 SHA-256 검증).

```powershell
.\Tools\unity.ps1 status --format json
.\Tools\unity.ps1 command ragna_checks
.\Tools\unity.ps1 command ragna_setup
.\Tools\unity.ps1 command editor_play
.\Tools\unity.ps1 command ragna_ui_checks
```

`ragna_setup`은 생성된 게임 씬을 다시 만듭니다. 수동으로 편집한 씬을 보존하려면 먼저 다른 이름으로 저장하세요.
Unity 메뉴에도 `Ragna` 아래 씬 생성, 검증, Android 빌드, iOS 내보내기가 있습니다.

## 모바일 빌드

Android APK: Unity 메뉴 `Ragna > Build Android APK`, 출력 `Ragna/Builds/Android/Ragna.apk`.
ARM64 / IL2CPP, 최소 Android API 26. 배포용 서명과 스토어 등록은 별도입니다.

2026-10-20 횡스크롤 버전 APK 생성과 서명·구성 검사 통과. Android Debug 서명 테스트용입니다.
실기기 설치·플레이 검증은 아직 진행하지 않았습니다. `Docs/DEVICE_TESTING.md`를 참고하세요.
APK를 다시 검사하려면 저장소 루트에서 `./Tools/Verify-Android.ps1`을 실행합니다.

현재 원본 경로에 한글이 있으므로 Android 빌드는 저장소 루트에서 다음 도구를 사용합니다.

```powershell
.\Tools\Build-Mobile.ps1 -Target Android
```

영문 임시 폴더에 입력 파일을 복사해 빌드한 뒤 결과를 원본 프로젝트의 `Builds` 폴더로 가져옵니다.
원본 Assets/Packages/ProjectSettings는 삭제하거나 이동하지 않습니다.
임시 작업 폴더는 `%TEMP%/RagnaBuild-01a14433`이며 이후 빌드에 캐시를 재사용합니다.

iOS: iOS Build Support를 설치한 Unity에서 `Ragna > Export iOS Xcode project` 실행 후
macOS의 Xcode에서 실제 Apple 팀/번들 ID와 서명을 설정해 기기에 설치합니다.
Windows만으로 iPhone용 서명·설치까지 마칠 수는 없습니다.
2026-10-20 Xcode 프로젝트 내보내기와 주요 파일·설정 검사도 통과했습니다.
결과는 `Ragna/Builds/iOS/Unity-iPhone.xcodeproj`이며 Mac의 Xcode에서 컴파일·서명해야 합니다.

## 저장과 자산

저장: `Application.persistentDataPath/ragna-save.json`, 직전 저장은 `.bak`.
5초마다, 중요 선택 직후, 앱 일시정지 시 저장하며 오프라인 자동 전투는 없습니다.
저장은 버전 4입니다. 버전 1·2의 수직 좌표 변환에 더해, 이전 20레벨 미만 캐릭터는 레벨을 유지한 모험가로 전환하고 20레벨 이상은 기존 직업을 보존합니다. 로컬 JSON으로 서버 권위나 치트 방지를 제공하지 않습니다.

도트와 효과음은 이 프로젝트에서 생성한 임시 원본 자산입니다. 최종 상용 아트와 사운드 제작이 필요합니다.
나눔고딕은 `Assets/Ragna/Fonts/OFL.txt`에 따른 SIL Open Font License 자산입니다.
그 밖의 Unity 패키지는 각 패키지 라이선스를 따릅니다.

게임 규칙과 최종 아트 규격은 `Docs/GAME_DESIGN.md`를 참고하세요.
전직 분기와 외형을 추가하는 방법은 `Docs/CAREER_AUTHORING.md`에 있습니다.
테스트와 빌드의 실제 결과는 작업 종료 시 별도 상태 문서에 기록합니다.
