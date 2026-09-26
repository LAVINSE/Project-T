# Project T 메인 거점 시안 v2

생성 방식: 내장 image_gen
피드백: 첫 시안이 게임용 이미지처럼 느껴지지 않음.
수정: 공방과 캐릭터 축소, 배경 장식 감소, 이동 공간 확대, 중복 메뉴 제거, 화면 가장자리 중심 UI.
화면 목업이며 Unity에 구현한 결과는 아님. 재화는 표시 예시.

## 생성 프롬프트

Use case: ui-mockup / substantial visual revision.
Create a revised Project T main hub GAME SCREEN, wide 16:9, target 1920x1080. The user rejected image 1 as not feeling like an actual game screen. Treat image 1 as the previous edit target, whose over-rendered illustration, giant props and duplicated menu layout must be replaced. Image 2 is the actual in-game wagon sprite and is the authoritative scale/detail/palette reference.

Redesign the composition dramatically to feel like a screenshot of a real playable 2D pixel-art PC indie defense RPG, built with a tilemap and discrete sprites. Quiet flat-colored repeatable grass tiles, simple chunky tree sprites, a legible open walking area, one consistent orthographic elevated three-quarter camera, zero perspective vanishing point. True crisp pixel clusters. Sparse purposeful prop placement and simple 3-tone material shading. No photographic texture, painted grass noise, cinematic depth of field, bloom, volumetric lighting, diorama presentation or lavish concept illustration. No giant foreground object. The game world fills the whole canvas with UI overlaid. A believable level rather than a postcard.

WORLD:
Forest camp clearing with tree line at top and left and a simple cobbled/dirt footpath running left-to-right across the clearing, branching to the upper-right exit. Use calm mid-value green ground with minimal small patches, coherent simple tile edges, very few flowers. Most of the ground must be open and uncluttered. Green/teal tree crowns with obvious hand-placed pixel clusters; solid dark shadows under trees. No river, no foreground bridge, no miniature diorama base.
The actual wooden alchemy wagon stands near center at x=960 y=450, only about 230 pixels wide in a 1920 output, using the SAME visual detail density as reference 2. Preserve the purple canopy with crescent, wood chassis, right wheel, forward shafts, lantern, cauldron, shelves, and crystals. Do not upscale it into a monumental building. It is a game sprite.
One small research table at x~620 y~350; one small crafting anvil/bench at x~600 y~600; training target at x~1260 y~470. Only two tiny chibi game sprites, each about 45 to 55 pixels high at 1920: blond warrior by the target, blue-robed mage beside wagon. Keep all object scales internally consistent. Discrete contact shadows and clear silhouettes. Tiny ambient props, maybe three crates total. No extra characters.
A restrained pale-gold outline around the wagon indicates a hover target, plus a tiny UI plaque above it '마법 공방'. Other scenery is unlabelled to remove duplicated menus. Do not add buildings, castle or a large settlement.

UI: Match the real Project T Figma UI language: near-black blue-gray fills (#171e26 / #10161e), thin muted brass double pixel borders (#8e734a) with clipped 3-pixel corners, tiny warm gold highlights (#dfb766), ivory labels, flat medium-brown primary button. Borders must be thin and understated, no thick bright gold filigree, no glowing menu frames, no modern rounded rectangles. Clean Korean sans-serif UI labels with pixel-style icons, no enormous marketing headings.
Top LEFT compact plate at margin 24: wagon icon and '마법 공방' on one line, small '거점' secondary. Overall width about 250, height about 62. OMIT PROJECT T giant branding and large wagon portrait.
Top RIGHT: small blue soul icon, '1,280', and a cog settings button in a compact bar at margin 24. No combat coins, no energy, no premium currency.
Bottom LEFT: compact horizontal four-button toolbar about 590 wide by 78 high at x=24, y=978. Four equal buttons, small sprite icons above or beside exact labels '공통 성장', '영웅 관리', '장비 제작', '인벤토리'. Icon motifs book, warrior bust, anvil, backpack. Do not duplicate these labels around the scene.
Bottom RIGHT: compact departure panel about 310 wide by 196 high at x=1586, y=860. It must occupy less than 4% of the screen. On its first line small heading '출전 준비'. Second line '초원 경계 · 스테이지 1'. Then a subtle text button '지역 선택' with right chevron. Bottom full-width brown-gold primary button with crossed swords icon and '출전하기'. No landscape thumbnail, no portraits row, no oversized info panel covering right side. No arbitrary statistics.
Maintain UI within safe margins, consistent thin frame weight, excellent legibility and hierarchy, all exact Korean copy spelled correctly. Leave the center and right-upper world visually open. This is the base hub before any popup is opened. It is a game interface concept, no browser/device frame, no presentation annotations.
Constraints: preserve project identity and functional destinations, remove illustration look and duplicated controls. Do not add battle enemies, health HUD, banners, live-service offers, tutorial arrows, reward tables or upgrade costs. The final should feel much closer to actual reusable pixel game assets and a modest desktop game HUD than to an AI fantasy poster.
