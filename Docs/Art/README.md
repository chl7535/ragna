# Reference adventurer art

User reference: adventurer-reference.png. Selected sheet is stored in the Unity project at
Ragna/Assets/Ragna/Resources/Heroes/AdventurerSheet.png.
Built-in image_gen was used, not the CLI/API fallback. Two passes: initial animation sheet, then removal of built-in VFX and extra frame spacing.
The selected sheet has transparent alpha, 948×1659 pixels. Its visual gutters are not an exact uniform grid;
Tools/Import-Adventurer.cs slices inspected row/column regions through Unity Sprite Editor data providers and aligns feet using custom pivots.
Imported sprites have point sampling, no mipmaps, no compression and 90 PPU. 28 poses map to Idle, Run, Attack, Cast, Hurt, Death and Guard.
ReferenceHero.cs supplies these authored frames for Warrior. Other classes retain their previous art.
Career-specific armour and weapon animation variants matching this reference remain to be authored. Current Warrior promotions share the reference outfit while retaining growth aura, career colours and skill evolution.
Existing mobile build outputs predate this art and must be rebuilt before device testing.

## Final edit prompt (built-in tool)

Edit this exact character sprite sheet for production use. Preserve the character identity, colours, outfit and the same seven rows of four animation poses (idle, run, sword attack, cast, hurt, death, guard). CRITICAL FIX: remove ALL magic, slashes, trails, sparkles, impact symbols and other VFX from every cell. Character and physical sword ONLY. Each sprite must fit completely inside its own square cell with a 15% TRANSPARENT EMPTY BORDER on ALL FOUR SIDES. No limb, hair, cape or sword can cross or touch the cell boundary. Shrink character uniformly in every cell to achieve generous margins. Do NOT stretch sword forward beyond the cell; shorten visible sword if needed. Exact equal 4-column x7-row square-cell grid, total aspect4:7. Same scale on all 28 frames. Feet at consistent baseline 85% from cell top; death poses lie on same floor. Right facing full-body side view, crisp detailed pixel art, transparent background, no text, no grid lines.
