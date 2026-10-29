# 첫 화면 편집

Resources/Title/TitleScreen.prefab을 Prefab Mode에서 열어 수정한다.

- Backdrop: 첫 화면 위아래 여백의 색상
- ArtworkViewport: 그림이 표시되는 영역과 클리핑 범위
- Artwork: 사용자 원본 이미지, Aspect Ratio Fitter로 비율 유지
- GAME START / SETTINGS / CREDITS / QUIT: 원본 이미지의 버튼 위치에 겹친 실제 Button 오브젝트

그림에는 이미 글자와 버튼 테두리가 들어 있으므로 버튼은 투명한 입력 영역과 눌림 효과만 제공한다.
각 Button의 RectTransform으로 위치·크기를, Color Tint로 눌림 효과를 수정한다.
원본 가로 이미지를 1920×1080 기준으로 표시한다.
게임 화면은 가로 전용이다. Safe Area를 적용하며 20:9에서는 원화 비율을 유지해 좌우 여백이 생긴다.

GAME START만 연결한다. 저장이 있으면 복원하고, 없으면 모험가로 새 도전을 시작한다.
SETTINGS / CREDITS / QUIT는 표시와 눌림 효과만 있고 동작은 없다. QUIT도 종료하지 않는다.
처음 실행할 때 저장 여부와 관계없이 첫 화면을 먼저 보여준다.
첫 화면을 닫거나 백그라운드로 전환해도 아직 열지 않은 저장 도전을 지우지 않는다.

ragna_title_setup은 없는 프리팹만 생성해 기존 편집을 보존한다.
ragna_title_checks는 버튼 터치 영역과 새 시작·이어가기·무동작 버튼을 저장과 분리해 검사한다.
