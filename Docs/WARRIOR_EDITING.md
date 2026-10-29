# 모험가 → 검사 편집 안내

개발 범위는 모험가 → 검사 루트에 한정한다. 다른 직업의 개발·수치 조정은 이 루트 완성 이후 진행한다.

## 공격력과 공격속도

Unity Project 창에서 아래 자산을 선택해 Inspector에서 수정한다.

- Assets/Ragna/Resources/CombatStats/Adventurer.asset: 모험가
- Assets/Ragna/Resources/CombatStats/Warrior.asset: 검사 및 기존 기사·광전사 검사 계열

Base Damage: 기본 공격력.
Damage Per Level: 레벨당 공격력 증가.
Attacks Per Second: 초당 공격 횟수. 높이면 빨라진다. 기존 강화·전직 배율이 추가 적용되며 최소 공격 간격은 0.22초다.
Skill Base Multiplier / Skill Multiplier Per Level: 스킬 기본 피해 배율과 스킬 레벨당 증가.
기본 피해는 Base Damage + 캐릭터 레벨 × Damage Per Level이다. 강화·전직·전승 배율은 이후 적용된다.
검사에서 기사·광전사로 전직한 경우 기존 Careers 자산의 Damage Multiplier / Attack Speed Multiplier도 곱해진다.
각 슬롯 스킬의 배율·슬롯 비용·연타 수는 Resources/CombatActions의 해당 자산에서 수정한다.
Play 모드에서 자산 값을 변경하면 즉시 적용된다. 자산 변경은 플레이 종료 후에도 남으므로 원래 값을 기록해 둔다.

## 피해 숫자와 몬스터 HP

- Assets/Ragna/Resources/CombatUI/EnemyHealth.prefab: HP 숫자와 막대의 크기·색·배치.
- Assets/Ragna/Resources/CombatUI/DamageNumber.prefab: 피해 숫자 기본 글꼴·크기·외곽선.

몬스터 머리 위에 현재 HP / 최대 HP와 체력 막대를 표시한다. 사망하면 표시를 숨긴다.
기본 공격·추가 공격·스킬 적중 시 피해 숫자를 표시하며 치명타는 색과 크기로 구분한다.
연타 숫자는 약간씩 위치를 달리해 겹침을 줄인다.
프리팹 구조 Background/Fill, Value 이름은 연결에 사용하므로 유지한다.
ragna_warrior_authoring은 없는 자산과 프리팹만 생성하며 기존 편집은 덮어쓰지 않는다.
