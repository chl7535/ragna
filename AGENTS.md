# Ragna development scope

- Current authorized route is Adventurer → Warrior (including the existing Knight/Berserker Warrior branches).
- Other jobs have been removed at the user's explicit request. Keep only Adventurer → Warrior and Knight/Berserker branches.
- Keep generic career/stat/action extension points for future additions, but do not recreate other jobs until asked.
- Prefer Inspector-editable ScriptableObjects for stats, conditions and content, and editable prefabs for UI. Keep code for common runtime behavior.
- Current combat is automatic hunting on a bounded X/Y ground plane. No turns, and no jumping now or in future work.
- Ragnar is landscape-only. Use 1920×1080 UI reference, 16:9 baseline and wider landscape displays with Safe Area. Do not add portrait layouts or portrait build settings.
- One attack action targets one enemy. Multi-hit attacks stop when that target dies; the next action may acquire another target.
- Use targeted Adventurer/Warrior checks; avoid expanding other-job work to satisfy an unrelated balance target.
