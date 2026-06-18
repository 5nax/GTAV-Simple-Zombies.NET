# Simple Zombies (.NET) — Enhanced Edition Port

A zombie‑apocalypse survival overhaul for **Grand Theft Auto V** single‑player:
infection mode, two zombie archetypes (walkers + runners), looting, crafting,
base‑building, survival stats (hunger/thirst/stamina), recruitable bodyguards,
saved vehicles, and roaming survivor factions (friendly, hostile, Merryweather
crate drops).

This branch is a **modernization of the original `ZombiesMod.dll`** so it builds
and runs on **GTA V Enhanced** (the 2025 DirectX 12 build) as well as **GTA V
Legacy**, via [ScriptHookVDotNetEnhanced](https://github.com/Chiheb-Bacha/ScriptHookVDotNetEnhanced).

> **Provenance:** no original source survived, so the code was recovered by
> decompiling the shipped `ZombiesMod.dll` (preserved under
> [`reference/original-build/`](reference/original-build/)) and then ported.
> See **What changed** below.

---

## Compatibility

| | Status |
|---|---|
| GTA V **Enhanced** (1.0.10xx) | ✅ via ScriptHookVDotNetEnhanced (v3 API) |
| GTA V **Legacy** (1.0.3xxx) | ✅ same build (SHVDNE supports both) |
| Runtime | .NET Framework 4.8 |
| Script API | ScriptHookVDotNet **v3** |
| UI | LemonUI (SHVDN3 build) |
| Save format | binary (`BinaryFormatter`, local data only) |

> ⚠️ I was **not able to run GTA V Enhanced** during this port, so the build is
> verified to **compile cleanly** against the Enhanced SDK and all known logic
> bugs are fixed in code, but it has **not been play‑tested in‑game**. See the
> [verification checklist](#in-game-verification-checklist) before relying on it.

---

## Gameplay (3.0 — survival overhaul)

Tuned for tense, realistic survival (think *The Last of Us*) rather than arcade hordes:
the dead are **scarce, slow and deadly**, sound is your biggest enemy, and supplies matter.

**The dead**
- **8 zombie types** — mostly slow **Walkers**; rare **Runners**; plus **Brute** (tanky,
  knockdown), **Crawler** (fast/fragile), **Bloater** (explodes), **Spitter** (ranged),
  **Screamer** (calls others), and the blind **Stalker** that hunts by *sound only*.
- **Stealth matters** — unsuppressed gunfire pulls walkers from far away; sprinting is
  loud; suppressors and moving quietly keep you alive. Hordes are **off by default**;
  blood moons are rare.

**Survival**
- **Hunting** — stalk skittish wildlife (they flee) and skin them for meat + **hides**.
- **Infection** — bites can infect you (HUD meter); cure with a crafted **Antidote**.
- **Bleeding** — hits can open wounds that drain health until you **Bandage**/**Medkit**.
- **Hunger / thirst / stamina** survival stats; **vehicle fuel** (refuel at pumps or a
  **Fuel Can**); **progression** (kills, days survived, XP/levels, perks).

**Crafting**
- **Dedicated crafting workshop** — press **K**: categorized recipes (Medical & Gear /
  Weapons / Building / Food / Tools) with live have/need costs and a craftable tick.
- New craftables: Antidote, Medkit, Leather Armor, Pipe Bomb, Jerky, Fuel Can — on top
  of the original buildables, weapons, food and base pieces.

**Everything is tunable** in `scripts/ZombiesMod.ini`
(`[zombies] [variants] [stealth] [bleeding] [hunting] [infection] [hordes] [progression]
[fuel] [combat]`); the main systems also toggle in the menu.

## AI survivor companions (optional — Google Gemini)

Talk to survivors and have them think, speak, and act for themselves. **Off by default**
— it does nothing until you add an API key.

**Setup**
1. Get a Gemini API key from [Google AI Studio](https://aistudio.google.com/apikey).
2. Run the mod once so `scripts/ZombiesMod.ini` is created, then under `[ai]` set:
   ```ini
   [ai]
   enabled = True
   gemini_api_key = YOUR_KEY_HERE
   gemini_model = gemini-2.5-flash
   tts_enabled = True            ; companions speak out loud
   tts_provider = gemini         ; "gemini" = neural voices (best); "sapi" = offline Windows TTS
   gemini_tts_model = gemini-2.5-flash-preview-tts
   gemini_tts_voice = Kore       ; default voice (companions are auto-assigned distinct voices)
   voice_input_enabled = False   ; set True for push-to-talk voice commands (needs a mic)
   ```
3. In-game, walk up to any survivor and press **B** to type to them (or **V** to speak,
   if voice input is on). They'll answer out loud and act on it.

**What they do** — they're aware of the situation (zombies nearby, location, time,
weather, their health, their current order), hold a persona, follow / hold / attack /
hunt / flee / wander on command, **fend for themselves**, and chime in unprompted.

**Notes**
- Companions speak with **Gemini neural TTS** by default (natural, per-companion voices),
  played through your audio device; it falls back to **Windows SAPI** automatically if
  synthesis fails (or set `tts_provider = sapi`). Voice *input* uses Windows SAPI (Windows
  only) — if unavailable the mod simply runs without it.
- Each conversation/bark is a Gemini API call (you pay per your Google quota); the
  bark interval is configurable. Zombies do **not** use the API — they use a built-in
  smarter rule-based brain.
- Diagnostics are logged to `scripts/ZombiesAi.log`.

## Installation (players)

1. **ScriptHookV** — install the build matching your game patch from
   [dev‑c.com](http://www.dev-c.com/gtav/scripthookv/) (one download supports both
   editions). Place `ScriptHookV.dll` + `dinput8.dll` in the game folder.
2. **ScriptHookVDotNetEnhanced** — download the latest release
   ([GitHub](https://github.com/Chiheb-Bacha/ScriptHookVDotNetEnhanced/releases) /
   [gta5‑mods](https://www.gta5-mods.com/tools/script-hook-v-net-enhanced)) and copy
   `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll`, `ScriptHookVDotNet3.dll`,
   `ScriptHookVDotNet.ini`, and `MinHook.x64.dll` into the game folder.
3. **The mod** — copy into `<game>\scripts\`:
   - `ZombiesMod.dll`
   - `LemonUI.SHVDN3.dll`
4. **Enhanced only:** install
   [DirectStorageFix](https://www.gta5-mods.com/tools), and disable BattlEye for
   story‑mode modding (`-nobattleye` launch option, or uncheck it in the Rockstar
   launcher). **Never load mods into GTA Online.**

The mod creates its files under `<game>\scripts\` on first run: `ZombiesMod.ini`
(keys + tuning for every system), `ZombiesProgress.ini` (kills/level/days), and
`Inventory.dat` / `Map.dat` / `Vehicles.dat` / `Guards.dat` (saves). Errors are logged
to `scripts\ZombiesModCrashLog.txt`.

### Default keys (editable in `ZombiesMod.ini`)
- **F10** — open the Simple Zombies menu (`zombies_menu_key`)
- **I** — open the inventory (`inventory_key`)
- **K** — open the crafting workshop (`crafting_key`)
- **B** — talk to the nearest survivor (type), **V** — talk by voice (push-to-talk) *(AI companions)*

Toggle **Infection Mode** in the menu to start the apocalypse.

---

## Building from source

The project targets `net48` but builds **cross‑platform** (Windows/macOS/Linux)
via the .NET SDK + reference assemblies — no Visual Studio required.

```bash
# 1. .NET SDK 8 (once)
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
export PATH="$HOME/.dotnet:$PATH"

# 2. build
dotnet build -c Release
# -> bin/Release/ZombiesMod.dll  (+ LemonUI.SHVDN3.dll)
```

Dependencies:
- `libs/ScriptHookVDotNet3.dll` — committed (from ScriptHookVDotNetEnhanced); the
  reference is `Private=false` so it is **not** redistributed in the build output.
- `LemonUI.SHVDN3` — pulled from NuGet (v2.2.0).
- `Microsoft.NETFramework.ReferenceAssemblies` — provides net48 reference assemblies.

---

## What changed (port summary)

The repo originally contained **incomplete decompiler output** (missing method
bodies, ~21 files of non‑compilable artifacts) and targeted the **SHVDN v2 + NativeUI**
stack on a `.csproj` that wouldn't build. This branch:

- **Recovered complete source** by re‑decompiling `ZombiesMod.dll` with ILSpy 9
  (resolves the menu handlers, `Database` spawn tables, and ctors the old repo lost).
- **Modern project**: SDK‑style `net48` `.csproj` that builds cross‑platform.
- **NativeUI → LemonUI**: every menu, list item, submenu, HUD timer bar, and the
  big‑message banner.
- **SHVDN v2 → v3 API**: ~600 call‑site changes — raw native hashes, control inputs,
  notifications, relationships (`int` → `RelationshipGroup` struct), the entire
  vehicle mod‑kit save/load (`vehicle.Mods`), doors/windows/bones collections, and
  `ZombiePed` converted from `: Entity` to composition (v3 makes entity ctors internal).
- **Bug fixes** (see [`CHANGES.md`](CHANGES.md)): collection‑mutation crashes,
  swapped health/armor, a broken stealth native, NRE guards, event/wrapper leaks,
  hardened save/load, and more.
- **Optimization**: managed distance math instead of a native call per tick in the
  zombie‑AI loop; save paths centralized; snapshotted tick iteration.

---

## Known limitations / non‑changes

- **Not play‑tested** on a live game (no Enhanced install available here).
- Raw world coordinates and asset names (prison gate, 24/7 shops, animal spawns,
  model/anim names) assume the SP map; Enhanced map/streaming changes could mis‑place
  a spawn. Validate the hash‑dependent natives in‑game.
- Save format is still binary; old `.dat` files from the original mod are not
  compatible (the version gate wipes them on upgrade).
- A few cosmetic spawn‑count off‑by‑ones and the ambiguous `OnDied` spawn‑blocker
  were intentionally left as‑is to avoid changing gameplay balance untested.

---

## In‑game verification checklist

Because this couldn't be run here, please confirm in **story mode** on Enhanced:

- [ ] All scripts load (no SHVDN error popup); menu opens on **F10**.
- [ ] Each menu toggle works: Infection Mode, Fast Zombies, Electricity, Survivors, Stats.
- [ ] Zombies spawn, chase, attack, and infect low‑health peds; runners can leap repeatedly.
- [ ] World sanitization runs without crashing (traffic/cops cleared, prison gate opens).
- [ ] Inventory (**I**) opens; crafting, blueprint preview, and item use work; **bandage gives health/armor correctly**.
- [ ] Survival stats drain and the HUD timer bars render; eating/drinking sustains them.
- [ ] Save/Load round‑trips vehicles (with mods/colors), guards, map props, and inventory.
- [ ] Survivor events fire (friendly/hostile/Merryweather); toggling Survivors off doesn't crash.
- [ ] Re‑test after any Rockstar Enhanced patch and bump ScriptHookV + SHVDNE to match.

---

## Credits

Original **Simple Zombies** by *sollaholla* (Sean). This is a community port for
GTA V Enhanced compatibility. ScriptHookV © Alexander Blade; ScriptHookVDotNetEnhanced
© Chiheb‑Bacha; LemonUI © Lemon.
