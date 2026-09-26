# Project T 메인 거점 화면 시안 v1

생성일: 2026-09-26
생성 방식: 내장 image_gen 도구
이미지: main-hub-concept-v1.png

기준 문서: Assets/00_DevPlanned의 전체 기획서·개발 체크·변경 기록 버전 0.30.
Figma MCP 참고: Project R 파일의 Main 페이지 안 Project T 프레임(5197:99), Battle Result / Stats(5241:177).
공방 외형 참고: Assets/03_Sprites/ArcaneWorkshop/ArcaneWorkshop_Portrait.png.

메인 화면은 출전 전 거점으로 해석했다. 공통 성장·영웅 관리·장비 제작·지역 선택 기능을 연결하는 시각 제안이며 확정 화면 설계나 구현 결과가 아니다. 소울 1,280은 표시 예시다. 기획서와 Unity 장면·프리팹은 변경하지 않았다.

## 생성 프롬프트

Use case: ui-mockup.
Create one polished full-screen 16:9 landscape PC pixel-art fantasy defense game MAIN HUB screen for Project T, designed at 1920x1080. The output is a finished raster concept screenshot, no device frame, no surrounding presentation canvas.

Reference inputs: the most recent two images are visual references, not edit targets. The first is the user's actual Project T battle-results UI from Figma: faithfully carry over its understated near-black navy panel fills, fine double antique-gold pixel borders, chamfered stepped corners, small diamond dividers, warm ivory Korean text, amber subheads, restrained flat brown-gold buttons and chunky hand-drawn pixel icons. The second is the actual ArcaneWorkshop portrait: faithfully preserve the identity of that wooden alchemy wagon with a purple crescent-moon canopy, two spoked wheels, lantern hanging from left post, bottles and rolled scrolls on shelves, green bubbling cauldron, side purple crystals, and wooden shafts. Render it naturally in the new scene. Do not copy the battle-results screen layout or victory wording.

Main concept: a peaceful forest camp before departure, a home for a traveling magic workshop. Broad elevated three-quarter/top-down RPG camera, richly crafted true pixel art with visible coherent pixel clusters, clean edges, controlled palette and stepped shadows, like premium classic 2D RPG environment assets. Not a smooth painting, not realistic 3D. Late-afternoon warm dappled sunlight, layered emerald/teal trees along upper edge, grass, mossy rocks, small flowers, a winding dirt path leaving upper-right. Warm amber light from wagon lantern contrasts with a subtle cyan-green cauldron glow. The scene should occupy about 70% of the screen and feel spacious, playable, cozy, and grounded.

Place the detailed purple-canopy alchemy wagon prominently near the center, at x~960 y~450, large enough to appreciate its recognizable parts but correctly scaled to camp props. A small research table with open book, star chart and candles is to its upper-left; a compact anvil and equipment crafting bench to its lower-left; a modest training patch with straw target and TWO small chibi pixel characters (one blond warrior, one blue-robed mage) is to the right of the wagon. Few crates and bottles, no additional major structures, no castle, no crowded village. All scene objects sit in consistent perspective on the ground.

UI arrangement and exact Korean copy, with clear generous spacing, large legible type:
1. Top-left compact dark gold-framed identity plate: small wagon pixel crest, 'PROJECT T' in small text and '마법 공방' in larger Korean. Under the title in small text: '거점'.
2. Top-right compact dark gold-bordered resource bar: blue soul-flame icon then '소울 1,280'; beside it separate small cog settings button. This is an illustrative saved balance. Do not show combat coins or premium currency in the hub.
3. Small tasteful dark-gold scene labels above the three interactable stations: '공통 성장' by research table, '장비 제작' by anvil bench, '영웅 관리' by training patch. Each has one recognizable pixel icon and tiny chevron. These labels do not obstruct their objects.
4. Rightmost quarter: one elegant restrained navy/gold departure panel, approximately x=1470..1880, y=245..850. Header '출전 지역'. A compact landscape thumbnail showing green meadow and a winding dirt route. Main title '초원 경계', secondary line '스테이지 1'. One simple row '라운드 3'. A row containing two small blond-warrior and blue-mage portraits with the label '클래스'. Below is a modest secondary button '지역 선택'. At the bottom a large brown-and-gold primary button '출전하기' with a small crossed-swords pixel icon and rightward chevron. This departure action is the clearest UI focus. No invented reward table, no clear rewards, no gacha, battle pass, event notices, daily missions, danger multipliers, energy systems or build slot counts.
5. Bottom center a single low-profile horizontal navy/gold navigation dock, with FOUR equally spaced icon+text buttons: '공통 성장', '영웅 관리', '장비 제작', '인벤토리'. Icons: open research book, paired character busts, anvil+hammer, brown backpack. Keep bottom dock moderate in scale and leave the environment visible above it. No huge decorative game logo, no slogan, no English marketing copy.
6. Small dark tooltip-like label adjacent to the wagon reading '마법 공방' can identify it.

Ensure typographic hierarchy and pixel frame thickness feel like the supplied Figma reference, not ornate mobile fantasy UI: slim gold rails, small stepped corners, flat navy interiors, crisp off-white text. Use subtle copper highlights, simple beveled gold primary button edges, limited decorative flourishes. Full-bleed landscape environment behind the UI. All Korean strings must be spelled accurately, no duplicated ghost lettering, no lorem ipsum, no annotations or mockup captions. No battle HUD, health bars, wave counter in top bar, victory banner, enemy crowds, or combat action. Composition communicates prepare/grow/craft/depart.
