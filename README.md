# Hotel Merge

**Merge items, serve your guests and turn a shabby 1-star inn into a 5-star, 7-floor hotel.**

Hotel Merge is a cosy portrait-mode merge game by **Pakue Games**, made with Unity 6 (URP). Guests check in
with requests, you merge items on a board to make what they want, deliver the orders for coins and XP, and spend
the coins on new rooms, floors and furniture upgrades until the hotel shines with five stars.

Platforms: **Android** and **WebGL** (playable in the browser, e.g. on itch.io). Version 0.1.0.

---

## Features

- **Merge boards:** three areas (Kitchen, Hotel Services, Pool), each with its own board, a golden generator and
  two six-step item chains: drinks, food, bath, care, pool fun and beach.
- **Guests and orders:** every room brings a guest with a request. Each order shows who asked (guest portraits)
  and has a timer. Accept it, prepare it on the board and deliver it in the hotel.
- **A 3D dollhouse hotel:** open-front rooms, windowed side wings, an entrance with an awning, a rooftop pool
  and a living street. It visibly grows and improves with every purchase.
- **Upgrades and stars:** buy beds, lamps, rugs, wallpaper and lobby decor in the shop. Room upgrades apply to
  every room (price × room count). Stars come from upgrade progress, and 5★ means everything is maxed.
- **Player level:** XP from guests (accepting and delivering orders), merges, upgrades, building, tasks and the
  daily reward. Levels pay coins and energy, and some rooms, floors and areas need a level.
- **Daily tasks:** five tasks a day, drawn from a pool of task patterns. Targets and rewards scale with your level.
- **Daily reward:** a 7-day streak of coins and energy.
- **Energy** keeps refilling even while the game is closed.
- **Polished UI kit:** glossy buttons, cream cards, currency pills, a bottom navigation bar and level badge.
  All art is drawn by code (see *Art pipeline*).
- **English and Turkish**, switchable in the settings.

## Coming soon

The hotel is only the first business. The game is growing into a **map of businesses**: when the hotel reaches
5★ and 7 floors, the next plot opens next to it.

- **New businesses with new game styles.** Each business starts shabby and is upgraded with its own kind of play,
  not only merging.
- **First up: match & pop levels.** A Candy Crush-style match-3 game is already prototyped in
  `Assets/Scenes/Match3Test.unity` (*Fruit Pop*). You swap neighbouring fruits to match 3 or more. Matching 4,
  making an L/T shape or matching 5 creates line blasters, bombs and rainbow stars, and two specials together
  make a bigger combo. Levels give stars for score within a move limit, and will fund the next business's upgrades.
- More areas, more item chains, and more ways to decorate.

---

## How it works (backend)

Everything runs on the device; there is no server. The game is built around one state object, events, and
scene objects created by editor tools.

### Scenes and startup
- `Splash.unity` shows the logo and a loading bar while `SampleScene.unity` (the game) loads in the background.
- `GameRoot` (in the game scene) is the entry point. It loads the save and wires up the screens:
  title, hotel, merge board, shop, orders board, popups and effects layer.
- It also handles navigation between screens and the background pause: when the app loses focus, time stops
  and only energy refills by real elapsed time.

### Game state and rules
- **`GameState`** (`GameState.cs` + `GameProgress.cs`) holds every rule and all persistent data (`SaveData`).
  Views never change data directly. They call methods like `Accept`, `Deliver`, `TryUnlockRoom`,
  `TryUpgrade` or `ClaimTask`, and listen to events (`OrderAdded`, `BoardChanged`, `StarsChanged`,
  `LeveledUp`, `TasksChanged`, …).
- **Static data** lives in `GameData.cs`:
  - `Items`: chains, levels, names, values;
  - `Areas`: costs and level/room requirements;
  - `Econ`: room and floor costs, XP curve, level gates, energy, daily rewards.
- **Orders:** every open room spawns a guest on a timer. The order's items and difficulty grow with rooms and
  guests served, and the reward grows with the hotel's stars.
- **Merging:** item id = `chain * 10 + level`. Generators (id ≥ 100) spend 1 energy per tap and lean towards
  what open orders still need.
- **Progression:**
  - XP has a rising curve (`Econ.XpToNext`).
  - Level gates: the last room of every floor, each new floor, and the Services/Pool areas need a level.
  - Daily tasks come from `GameState.TaskPool`: text patterns with `{0}` (amount) and `{1}` (item level or
    area) whose targets and rewards are `base + perLevel × (level − 1)`. Add a `TaskKind` and a pool entry
    to create a new task type.
- **Upgrades:** `UpgradeCatalog` (ScriptableObject) lists each upgrade's levels, costs, icons and 3D models.
  Star progress = bought levels ÷ all levels.

### Saving
- `SaveData` is serialized with `JsonUtility` into `PlayerPrefs`. On the web, that is the browser's storage.
- The game saves only after something changed, at most every 15 seconds, plus on pause and quit, so it doesn't
  hitch.
- Older saves are migrated on load (`GameState.Migrate`).
- Energy catches up from the saved timestamp when the game opens.

### Presentation
- **Screens are built by code** (`Build` methods) and saved into the scene by editor menus, so every object is
  editable in the Inspector. At runtime, `Init` only binds data.
- **UI kit** (`Assets/Scripts/UI`):
  - `Kit` creates buttons, cards, pills, bars, tabs, the nav bar and portraits; `KitBar`, `KitTabs`,
    `KitNavBar` and `KitLevelBadge` are the reusable components.
  - Ready-made prefabs are in `Assets/HotelMerge/Prefabs/Kit`, and `Assets/Scenes/UIKitShowcase.unity` shows
    them all.
  - Old sprite names are aliased to the kit's drawings in `Art.cs`.
- **3D:**
  - `Hotel3D` builds the hotel, rooms, areas and street from procedural rounded meshes (`MeshKit`).
  - Furniture comes from the imported model packs via the upgrade catalog.
  - `CameraRig` frames the building for any screen shape and scrolls between floors.
- **Helpers:**
  - `Pool`: object pooling for items, cards, effects and popups.
  - `Tween`: a tiny coroutine tweener.
  - `Fx`: bursts, flying coins, toasts.
  - `Sfx`: sounds synthesized at startup, so no audio files are needed.
  - `ScreenFit`: safe-area aware scaling for phones and tablets.
- **Localization:** the game is written in Turkish. `Assets/Resources/Localization/Strings.txt` maps each
  text to other languages (one column per language), and `Loc.T` / `Loc.F` look them up.
- **Performance:**
  - Each screen, the room bubbles, the popups and the effects have their own nested canvas.
  - HUD texts update only when values change.
  - The static street is batched, and small parts don't cast shadows.
  - The 3D camera pauses behind the full-screen merge board.
  - On WebGL, the frame rate is paced by the browser.

### Web build
- `Assets/WebGLTemplates/HotelMerge` is a custom loader page in the splash screen's style.
  `Assets/Plugins/WebGL/HotelMerge.jslib` lets the game pass the chosen language to it.
- Builds use Brotli with a decompression fallback.

---

## Project layout

```
Hotel Merge/                     Unity project (open this folder in Unity Hub)
  Assets/
    Scripts/                     game code (state, screens, views, components, UI kit, editor tools)
    Scenes/                      Splash, SampleScene (game), Match3Test, UIKitShowcase, AssetShowcase
    HotelMerge/                  generated prefabs, upgrade catalog, app icon
    Resources/Art/               sprites: Kit (UI kit), Items, Icons, UI, Packs, Furniture thumbnails
    Resources/Localization/      Strings.txt
    ThirdParty/                  imported furniture models and licenses
    WebGLTemplates/, Plugins/    web loader page and its jslib
  Tools/art/                     Python art generators (SVG → PNG via Inkscape)
  Marketing/itchio/              store page art and texts
```

## Working on the project

1. Open `Hotel Merge/` with **Unity 6000.3** (URP). The `Library/` folder is rebuilt on first open.
2. Unity's **Hotel Merge** menu rebuilds everything that is generated:
   - *Sahneyi Kur* — builds the game scene.
   - *Prefabları Oluştur* — builds the runtime prefabs.
   - *Otel Yükseltmelerini Oluştur* — builds the upgrade catalog.
   - *Açılış Sahnesini Kur* — builds the splash scene.
   - *UI Kit: Prefablar ve Vitrin* — builds the UI kit prefabs and showcase scene.
   - *Match-3 Test Sahnesini Kur* — builds the match-3 test scene.
   - *Build Ayarlarını Uygula* — applies Player, icon and quality settings.
   - *Kaydı Sıfırla* — clears the save.
3. To redraw art, run the generators (they need Python 3 and Inkscape):
   ```bash
   python3 "Hotel Merge/Tools/art/make_kit.py"     # UI kit frames, icons, portraits
   python3 "Hotel Merge/Tools/art/make_items.py"   # merge items (512 px)
   python3 "Hotel Merge/Tools/art/make_art.py"     # legacy icons and UI pieces
   python3 "Hotel Merge/Tools/art/make_icon.py"    # app icon
   python3 "Hotel Merge/Tools/art/make_itch.py"    # itch.io cover, banner, background
   ```
4. Build Android or WebGL from *File → Build Profiles*. Builds go to `Hotel Merge/Builds/`, which is not committed.

## Third-party assets

The furniture models and icon packs are third-party assets. Their licenses are in `Assets/ThirdParty/Licenses`.
The icon packs may be used in a commercial game but **their files may not be redistributed**, so keep this
repository private, or uncomment the matching lines at the end of `.gitignore` before making it public.

## Credits

Game by **Pakue Games**. Icons: Cartoon Game Icons and Forest Magic icon packs. Furniture: cute cartoon
furniture and interior model packs.
