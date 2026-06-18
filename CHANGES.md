# Changes — Enhanced Edition port

Chronological summary of the modernization. Each bullet maps to a commit on the
`enhanced-port` branch.

## 2.0 — Content expansion (more variety & dynamism)

New, INI-tunable systems (all gated in `[section]`s of `ZombiesMod.ini` and most
toggleable in the in-game menu):

**Zombie variety** — five new variants rolled per spawn alongside walkers/runners:
- **Brute** — 3× health, slow, heavy hit that knocks you down.
- **Crawler** — fast, fragile, crouched scuttle.
- **Bloater** — bursts into a fiery explosion on death.
- **Spitter** — attacks from range with corrosive spit.
- **Screamer** — shrieks on contact and summons a reinforcement wave.

**Dynamic threats** (`HordeController`)
- Ambient **hordes** periodically converge on the player.
- Random **blood-moon nights**: all zombies sprint, weather thickens, horde size/rate double.
- Screamer **reinforcement waves**.

**Player infection** (`PlayerInfection`) — zombie bites can infect you; the meter
climbs over time (HUD bar), and at 100% you take "turning" damage. Cure with the new
craftable **Antidote**.

**Progression** (`PlayerProgression`) — kills counter, **days survived**, **XP &
leveling** (specials worth more), **max-health perks** per level, and a HUD readout;
saved to `scripts/ZombiesProgress.ini`.

**Vehicle fuel** (`VehicleFuel`) — vehicles burn fuel (HUD bar + dashboard gauge),
engine cuts out when empty; refuel at gas pumps or with the new craftable **Fuel Can**.

**Combat & world** — headshots are lethal crits (`CanSufferCriticalHits`), distant
zombie **corpses are cleaned up** above a cap (perf), and you can **revive a downed
survivor** (INPUT_CONTEXT) to recruit them.

**Config** — new `Static/GameConfig.cs` exposes every knob via `ZombiesMod.ini`
(`[variants] [infection] [hordes] [progression] [fuel] [combat]`).

Also fixed while here: infection-spread zombies are now registered with the spawner
(no longer escape ClearAll), survivor/merryweather spawn-count off-by-ones, the
unbounded Loot247 looted-shelf list, and a leaked Merryweather flavor vehicle. Removed
dead code (LootPickupType, IUpdatable, an empty stub, a duplicate enum value).

---

## Build / platform
- Re‑decompiled `ZombiesMod.dll` with ILSpy 9 to recover complete, compilable
  source (the prior repo was incomplete decompiler output: missing menu handlers,
  `Database` static ctor, and several other ctors).
- New SDK‑style `net48` `.csproj` that builds cross‑platform via
  `Microsoft.NETFramework.ReferenceAssemblies`.
- References ScriptHookVDotNet**3** + `LemonUI.SHVDN3`; original DLL archived under
  `reference/original-build/`.

## UI: NativeUI → LemonUI
- `MenuPool` → `ObjectPool`, `UIMenu` → `NativeMenu`, `UIMenuItem` → `NativeItem`,
  `UIMenuCheckboxItem` → `NativeCheckboxItem`, `UIMenuListItem` → `NativeListItem<T>`.
- HUD: `TimerBarPool`/`BarTimerBar` → `TimerBarCollection`/`TimerBarProgress`
  (percentage 0–1 rescaled to LemonUI's 0–100).
- Submenus rewired with `AddSubMenu`; main inventory/resource menus, work‑bench and
  weapons‑crate trade menus, guard task list, and the Merryweather banner ported.
- `UI.Notify` (37 sites) → a `Notifier.Show` shim over `Notification.PostTicker`.

## SHVDN v2 → v3 API (~600 call sites)
- Raw `Hash._0x…` enum members → `(Hash)0x…uL` casts.
- Control inputs: dropped the player‑index arg; added a `Ctrl` helper for the
  disabled‑control natives v3 no longer wraps.
- `Entity` renames: `CurrentBlip`→`AttachedBlip`, `Alpha`→`Opacity`,
  `HasCollision`→`IsCollisionEnabled`, `FreezePosition`→`IsPositionFrozen`,
  `ResetAlpha`→`ResetOpacity`; `Blip.Remove`→`Blip.Delete`.
- Relationships: `int` → `RelationshipGroup` struct + `SetRelationshipBetweenGroups`.
- Vehicles: full mod‑kit save/load onto `vehicle.Mods` (colors, tint, wheel type,
  neon, mod/toggle indexers, `InstallModKit`); `Doors[]`/`Windows[]`/`Bones[]`
  collections; `LocalizedName`, `PassengerCapacity`.
- `WeaponHash` namespace move; `WeaponComponent` enum → `WeaponComponentHash` +
  `weapon.Components[hash].Active`.
- `Entity/Ped/Prop(handle)` ctors → `Entity.FromHandle`. **`ZombiePed` converted
  from `: Entity` to composition** (v3 makes entity ctors internal, so a mod
  assembly can no longer subclass them) with a delegated surface + implicit
  `→Ped` operator.
- Misc: `Ped.DiesOnLowHealth`, `PedGroup.Formation`, `RaycastResult.HitPosition`,
  `World.Blackout`, `World.CurrentTimeOfDay`, `Game.GetUserInput(WindowTitle…)`,
  `Screen.FadeIn/FadeOut`, `IntersectOptions`→`IntersectFlags`.

## Bug fixes
**High**
- `SurvivorController.Destroy`/`OnTick` mutated `_survivors` during `ForEach`
  (`InvalidOperationException` that could abort the script) → iterate snapshots.
- Bandage **GiveHealth/GiveArmor were swapped** → corrected.
- `SetStealthMovement` omitted the ped handle (no‑op) → fixed.

**Medium (crashes / leaks)**
- `ScriptEventHandler`: snapshot handler lists so register/unregister mid‑cycle
  can't skip/replay or corrupt iteration.
- `EntityEventWrapper`: detach the `Aborted` handler in `Dispose` (was a permanent
  leak); removed a redundant second list removal.
- NRE guards: `PlayerMap.OnAborted`, `MerryweatherSurvivors.CleanUp` particle,
  `HostileSurvivors` blip/driver.
- Buildable‑preview busy‑wait given a failsafe timeout.
- `Serializer`: `using` blocks (no handle leak/file lock), full‑timestamp +
  stack‑trace logging (appended, not overwritten), corrupt saves backed up rather
  than silently destroyed.

**Gameplay / logic**
- `SurvivorController`: honor `survivor_spawn_chance` (roll was discarded).
- Runner re‑arms its leap (`_jumpAttack` was set once and never reset).
- `ZombiePed.Update` returns after self‑`Delete` (was issuing natives on a deleted ped).
- `Database.GetRandomVehicleByClass` guards an empty filter (`IndexOutOfRange`).
- `AnimalSpawner` requests the model before `CreatePed` (empty herds otherwise) and
  honors `MaxAnimalsPerSpawn`.
- `VehicleRepair` only repairs + consumes a kit if the vehicle still exists and the
  player is still beside it when the task ends (was consuming on interrupt).
- `MapInteraction`: subscribe the weapons‑crate handlers once (was leaking a handler
  per interaction); fixed `IsEnemy` operator precedence; door `Exists()` guard.
- Duplicate "Save All" menu labels disambiguated; empty‑menu craft guard.
- Restored saved vehicle `Heading`/`Wheels` that were captured but never re‑applied.

> Several audit‑flagged "bugs" (a `PickupLoot` variable shadow, a
> `HasClearLineOfSight` `!= null` comparison, duplicated `if` blocks) were
> **artifacts of the original weak decompile** and do not exist in the clean ILSpy
> source — verified, no change required.

## Optimization
- `VDist` uses managed `Vector3.DistanceTo` instead of `GET_DISTANCE_BETWEEN_COORDS`
  (called per‑ped every tick in zombie AI); added `VDistSquared`.
- All save/load paths routed through `Config.*FilePath` constants.
