# 가로 화면 전용 설정

라그나르는 세로 화면을 지원하지 않는다. 이후 화면·UI·빌드는 가로 전용으로 제작한다.

## 기준

- UI 기준 해상도: 1920×1080 (16:9)
- 20:9 및 21:9: 화면 높이와 캐릭터 크기를 유지하고 좌우 배경을 더 표시
- 도트 카메라 내부 기준: 320×180 / 16 PPU. UI 해상도와 별도이며 캐릭터·적의 기존 Point 필터를 유지
- 모바일 방향: Landscape Left, Portrait 및 Portrait Upside Down 비활성
- 자동사냥·바닥 위아래 이동 유지. 점프 없음

## Unity에서 편집

Assets/Ragna/Resources/UI/LandscapeHUD.prefab
상단 HP·지역·진행 정보·준비·성장·정지, 하단 상태·물약·보호막의 크기·위치·색·글자를 수정한다.
BattleHUD 컴포넌트의 참조와 버튼 연결 이름은 유지한다. 런타임은 이 프리팹을 복제하고 동작만 연결한다.

Assets/Ragna/Resources/UI/LandscapeDisplay.asset
UI 기준 해상도, 전투 배경 영역의 위·아래 경계, 카메라 반높이를 설정한다.
현재 HUD 상단 경계0.90/하단 경계0.17과 배경 설정이 맞춰져 있다. 경계를 바꾸면 HUD 프리팹도 함께 조정한다.

Assets/Ragna/Resources/Title/TitleScreen.prefab
SafeArea 아래 ArtworkViewport와 실제 버튼을 수정한다. 16:9 원화는 전체 비율을 보존한다.
20:9 이상 타이틀은 원화 비율과 버튼을 보존하기 위해 좌우에 여백이 생긴다. 전투 배경은 화면 전체 폭을 채운다.
타이틀과 HUD 모두 Screen.safeArea를 적용해 노치·카메라 영역을 피한다.

## 적용과 검증

ragna_landscape_setup: 가로 Player Settings와 카메라, HUD 자산 생성, 기존 타이틀 가로 배치를 적용한다.
기존 HUD 프리팹의 수동 편집은 덮어쓰지 않는다. 타이틀의 가로 기준·Safe Area·뷰포트는 재적용한다.
ragna_setup으로 씬을 다시 만들어도 가로 카메라와 빌드 방향을 사용한다.
ragna_capture --width 1920 --height 1080 또는 --width 2400 --height 1080.
세로 비율 캡처는 허용하지 않는다.

16:9/20:9에서 가로 설정14·타이틀 터치13 검사 통과. UI31·준비창13·자동사냥20 검사 통과.
여섯 배경은 16:9/20:9/21:9 범위 검사42개 통과.
실기기 회전·노치·터치 검증은 아직 하지 않았다. 기존 APK/iOS 내보내기는 세로 시절 결과라 다시 빌드해야 한다.
Tools/Verify-Android.ps1도 가로 orientation=0을 검사하도록 바꿨다. 이전 APK는 새 검사에 통과하지 않는다.
