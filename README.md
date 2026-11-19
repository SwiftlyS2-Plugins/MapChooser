<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" alt="MapChooser logo" />
  <h1><strong>MapChooser</strong></h1>
  <p>Map votes for CS2 servers: near the end of each map, on demand, and by admins.</p>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <a href="https://github.com/SwiftlyS2-Plugins/MapChooser/releases/latest"><img src="https://img.shields.io/github/v/release/SwiftlyS2-Plugins/MapChooser?style=flat&logo=github&cacheSeconds=3600" alt="Latest Release"></a>
  <img src="https://img.shields.io/github/downloads/SwiftlyS2-Plugins/MapChooser/total?style=flat&logo=github&cacheSeconds=3600" alt="Downloads">
  <img src="https://img.shields.io/github/stars/SwiftlyS2-Plugins/MapChooser?style=flat&logo=github&cacheSeconds=3600" alt="Stars">
</p>

---

## ✨ Features

MapChooser picks the next map by vote, from the maps in `maps.jsonc`.

- 🗳️ **End-of-map vote** — opens near the time or round limit
- 🔁 **Rock the vote** — enough `!rtv`s start a vote now
- 📌 **Nominations** — players put maps on the ballot
- ⏩ **Extends** — from the ballot or `!ext`
- ⏳ **Cooldown** — recent maps stay off the ballot
- 👥 **Player limits** — maps offered only within their player range
- 🔄 **Map cycle** — in order or at random, reordered in game
- 🛠️ **Admin pages** — change map, set the next one, start a vote
- 🌐 **Workshop maps** — by ID
- 🧩 **Shared API** — the vote and nominations, for other plugins

---

## 🚀 Installation

### Requirements

- SwiftlyS2 1.5.0 or later

### Steps

1. Unzip the release into `addons/swiftlys2/plugins/`.
2. Restart the server. MapChooser writes `config.jsonc` and an empty `maps.jsonc`.
3. List your maps in `maps.jsonc` — [maps.jsonc](#mapsjsonc) — and restart, or add them in game with `!addmap`.

Until `maps.jsonc` lists maps, votes offer none and `!maplist` replies `There are no maps configured.`

### Where things are

| What | Path |
|------|------|
| Plugin | `addons/swiftlys2/plugins/MapChooser/` |
| Config | `addons/swiftlys2/configs/plugins/MapChooser/` — `config.jsonc` and `maps.jsonc` |
| Translations | `addons/swiftlys2/plugins/MapChooser/resources/translations/` — `en`, `fi`, `lt`, `pt-BR`, `zh-CN` |
| API contract | `addons/swiftlys2/plugins/MapChooser/resources/exports/MapChooser.Contract.dll` — [Using the API](#-using-the-api) |

### Verify

| Check | Expect |
|-------|--------|
| The config folder | `config.jsonc` and `maps.jsonc` |
| `!maplist` | Your maps, in your console |
| `!maps`, as an admin | **Change map**, **Set next map**, **Start a vote** and **Map cycle** |

---

## 💬 Commands

Chat: `!maps` (or `/maps`). Console: `sw_maps`. A `[map]` is a map's name, or part of one.

### Player commands

| Command | Aliases | Description |
|---------|---------|-------------|
| `!maps` | `!mapmenu` | Open the map menu — [Menus](#-menus) |
| `!timeleft` | — | Time or rounds left |
| `!nextmap` | — | The next map, when known |
| `!nominate [map]` | `!nom`, `!yd` | Put a map on the next ballot. Without one, opens **Nominate** |
| `!rtv` | — | Ask for a vote now. Enough players asking starts one |
| `!unrtv` | — | Take back your `!rtv`, and with it your ballot during a vote |
| `!revote` | — | Reopen the ballot during a vote |
| `!votemap [map]` | — | Vote to change to a map. Without one, opens a list |
| `!ext` | `!extendmap` | Vote to extend the map |
| `!maplist` | — | The map pool in your console, cooldowns marked |

### Admin commands

Permissions are defaults, changed by the setting in brackets. `""` lets everyone in.

| Command | Aliases | Permission | Description |
|---------|---------|------------|-------------|
| `!map [map]` | `!setmap` | `admin.changemap` (`ChangeMapPermission`) | Change map now. Without one, opens **Change map** |
| `!setnextmap [map]` | — | `admin.changemap` (`SetNextMapPermission`) | Set the map for after this match. Without one, opens **Set next map** |
| `!mapsvote` | — | `admin.mapsvote` (`MapsVotePermission`) | Open **Start a vote** |
| `!cyclemenu` | `!mapcycle` | `admin.changemap` (`Cycle.CycleMenuPermission`) | Open **Map cycle** |
| `!addmap <name> [id]` | — | `admin.changemap` (`Cycle.AddMapPermission`) | Add a map to the end of `maps.jsonc`. `id` defaults to `name` |
| `!removemap <name>` | — | `admin.changemap` (`Cycle.RemoveMapPermission`) | Remove a map from `maps.jsonc`, by its whole name |

From the server console, `sw_map`, `sw_setnextmap`, `sw_addmap` and `sw_removemap` take the same arguments.

---

## 📋 Menus

```
Map vote                                               !maps
├─ Vote             the ballot                         !revote
├─ Nominate         every map but the current one      !nominate
├─ Change map       every map                          !map
├─ Set next map     every map                          !setnextmap
├─ Start a vote     a toggle per map                   !mapsvote
└─ Map cycle        every map, in cycle order          !cyclemenu
   └─ <map>         ↑ Move Up, ↓ Move Down, ✕ Remove, ← Back
      └─ ✕ Remove   Yes, Remove · Cancel
```

**Vote** shows during a vote, to players who may vote, unless `DisableVoteMenuExit` is on; **Nominate** while `Rtv.NominationEnabled` is on; each admin page with its command's permission. With none of them, `!maps` replies `There is nothing to open right now.` `!votemap` opens its list outside the map menu.

On **Nominate**, a map that can't be nominated now is greyed out, with the reason: **Played recently**, **Needs 4 players** or **Up to 3 players**. On **Map cycle**, the title shows **Mode: Sequential** or **Mode: Random**, **◄** marks the current map and **→** the next.

### Start a vote

| Row | Shows |
|-----|-------|
| **START VOTE** | The maps ticked, as **START VOTE (3)**. Greyed out until one is, and while a vote is running or about to start; its hint says which |
| A map | A toggle |

Starting counts down 5 seconds in chat, cancelled by a map change or match start. The vote lasts `EndOfMap.VoteDuration` seconds, drops ticked maps in cooldown, and its winner is played when the match ends.

### Keys

Defaults: **Shift** up, **F** down, **E** select, **Tab** close. Set them in SwiftlyS2's `configs/menus/menus.jsonc` (template: `menus.example.jsonc`), under `Menus`: in `"core.menu"` for every menu, or in a block named by a menu's [id](#menus) for that menu.

**Map cycle** rows also take `MoveUp`, `MoveDown` and `Remove`, unbound by default. Bound, they act on the highlighted map without opening its page, and a moved map stays highlighted, so the same key moves it on:

```jsonc
{
  "Menus": {
    "mapchooser.cycle": {
      "MoveUp": "space",
      "MoveDown": "ctrl",
      "Remove": "alt"
    }
  }
}
```

---

## ⚙️ Configuration

Created on first start in `addons/swiftlys2/configs/plugins/MapChooser/`:

| File | Holds |
|------|-------|
| `config.jsonc` | The settings below, under `MapChooser` |
| `maps.jsonc` | The map pool, under `MapChooserMaps` — [maps.jsonc](#mapsjsonc) |

MapChooser reads both files only when it loads: **restart** the server after editing them. Keys missing from `config.jsonc` are added with their defaults.

### Rtv

`!rtv` starts a vote once `VotePercentage` of players have used it, rounded up: 6 of 10 at `60`.

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Allow `!rtv` and `!unrtv` |
| `EnabledInWarmup` | `false` | Allow them in warmup |
| `NominationEnabled` | `true` | Allow `!nominate` and **Nominate** |
| `MinPlayers` | `0` | Humans needed on the server. `0` = no minimum |
| `MinRounds` | `0` | Rounds to play first. `0` = no minimum |
| `ChangeMapImmediately` | `true` | Change map when the vote ends. `false` = at round end |
| `ChangeMapDelay` | `3` | Seconds' countdown to the change |
| `MapsToShow` | `6` | Maps on the ballot, at most |
| `VoteDuration` | `30` | Seconds the vote is open |
| `VotePercentage` | `60` | Players, in %, whose `!rtv` starts it |
| `VoteCooldownTime` | `300` | Seconds `!rtv` waits after a vote nobody voted in |

### Votemap

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Allow `!votemap` |
| `VotePercentage` | `60` | Players, in %, who must pick the same map |
| `ChangeMapImmediately` | `false` | Change map at once. `false` = when the match ends |
| `MinPlayers` | `0` | Humans needed on the server. `0` = no minimum |

### EndOfMap

The vote that opens on its own near the end of the map. Its winner is played when the match ends.

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Hold the end-of-map vote |
| `MapsToShow` | `6` | Maps on the ballot, at most, besides **Extend Current Map** |
| `VoteDuration` | `30` | Seconds the vote is open. `!mapsvote` votes too |
| `ChangeMapDelay` | `5` | Seconds' countdown to any map change an RTV vote didn't decide, `!map` included |
| `TriggerSecondsBeforeEnd` | `120` | Open it with this many seconds of `mp_timelimit` left |
| `TriggerRoundsBeforeEnd` | `4` | Open it with this many rounds of `mp_maxrounds` left, or a team this many wins from winning |
| `AllowExtend` | `true` | Offer **Extend Current Map** while extends are left |
| `ExtendTimeStep` | `15` | Minutes an extend adds to `mp_timelimit`, if set |
| `ExtendRoundStep` | `5` | Rounds an extend adds to `mp_maxrounds`, if set. `mp_winlimit`, if set, rises by half, rounded up |
| `ExtendLimit` | `3` | Extends per map, by vote and `!ext` together |

### ExtendMap

`!ext` extends the map by the [EndOfMap](#endofmap) steps once `VotePercentage` of players have used it.

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Allow `!ext` |
| `EnabledInWarmup` | `false` | Allow it in warmup |
| `MinPlayers` | `0` | Humans needed on the server. `0` = no minimum |
| `MinRounds` | `0` | Rounds to play first. `0` = no minimum |
| `VotePercentage` | `60` | Players, in %, whose `!ext` extends the map |

### Cycle

With `Enabled` on, a match that ends with no vote running and no next map decided changes to the map after the current one in `maps.jsonc`, ignoring cooldown and player limits. `!addmap`, `!removemap` and **Map cycle** change the pool at once and rewrite `maps.jsonc`, dropping its comments.

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `false` | Change to the next map when the match ends |
| `RandomOrder` | `false` | A random map other than the current one instead |
| `AddMapPermission` | `"admin.changemap"` | Permission for `!addmap` |
| `RemoveMapPermission` | `"admin.changemap"` | Permission for `!removemap` |
| `CycleMenuPermission` | `"admin.changemap"` | Permission for `!cyclemenu` and **Map cycle** |

### MapChooser

Keys directly under `MapChooser`.

| Setting | Default | Description |
|---------|---------|-------------|
| `MapsInCooldown` | `3` | Previous maps kept off ballots, nominations and `!votemap`. `0` = none. Forgotten on restart |
| `AllowSpectatorsToVote` | `false` | Let spectators vote, nominate and use `!rtv`, `!votemap` and `!ext`. They then count in every share |
| `AnnounceVotes` | `true` | Announce each `!rtv`, `!votemap` and ballot, with counts. `false` = only the voter hears of their ballot |
| `DisableVoteMenuExit` | `false` | Ballot with no close key. It closes once the player votes |
| `DetailedLogging` | `false` | Log map-change decisions to the console |
| `SetNextMapPermission` | `"admin.changemap"` | Permission for `!setnextmap` and **Set next map** |
| `MapsVotePermission` | `"admin.mapsvote"` | Permission for `!mapsvote` and **Start a vote** |
| `ChangeMapPermission` | `"admin.changemap"` | Permission for `!map` and **Change map** |

### Commands

Each value is a comma-separated list, and every name in it is registered as its own command; `""` registers none. A name an earlier setting already registered is skipped, with a line in the console.

| Setting | Default |
|---------|---------|
| `Menu` | `"maps,mapmenu"` |
| `Rtv` | `"rtv"` |
| `UnRtv` | `"unrtv"` |
| `Nominate` | `"nominate,nom,yd"` |
| `Timeleft` | `"timeleft"` |
| `Nextmap` | `"nextmap"` |
| `Votemap` | `"votemap"` |
| `Revote` | `"revote"` |
| `SetNextMap` | `"setnextmap"` |
| `Extend` | `"ext,extendmap"` |
| `MapsVote` | `"mapsvote"` |
| `ChangeMap` | `"map,setmap"` |
| `MapList` | `"maplist"` |
| `AddMap` | `"addmap"` |
| `RemoveMap` | `"removemap"` |
| `CycleMenu` | `"cyclemenu,mapcycle"` |

### maps.jsonc

`Maps`, under `MapChooserMaps`, lists the map pool in cycle order. It is empty on first start.

| Field | Default | Description |
|-------|---------|-------------|
| `Name` | — | Shown in menus and chat |
| `Id` | — | The map to load: a map name, `"de_dust2"`, or a workshop ID, `"3124567099"` or `"ws:3124567099"` |
| `MinPlayers` | `0` | Humans on the server before the map is offered. `0` = no minimum |
| `MaxPlayers` | `0` | Most humans the map is offered with. `0` = no maximum |

```jsonc
{
  "MapChooserMaps": {
    "Maps": [
      {
        "Name": "Dust II",
        "Id": "de_dust2",
        "MinPlayers": 4
      },
      {
        "Name": "Mirage",
        "Id": "de_mirage",
        "MaxPlayers": 3
      }
    ]
  }
}
```

With up to 3 humans on the server, votes and nominations offer only Mirage; from 4, only Dust II.

---

## 📚 API reference

`IMapChooserApi` in `MapChooser.Contract.dll`, namespace `MapChooser.Contract`, key `MapChooser.Api.v1` (`IMapChooserApi.InterfaceKey`). Read-only. Getting it: [Using the API](#-using-the-api).

- Requires MapChooser 1.4.0+. Members marked 1.5.0+ or 1.6.0+ need that version; against an older one, a call into them throws `MissingMethodException`.
- Read members on the game thread. Events are raised there once MapChooser has stored the change, so a handler reads the new state.
- `VoteChanged` and `NominationsChanged` can fire inside map load, when a map change ends a vote or clears nominations: leave entities alone there.
- A handler that throws is logged as a warning; the other handlers still run.
- After MapChooser unloads, an instance you still hold reports no vote, maps or nominations, and raises nothing.

### Vote

| Member | Description |
|--------|-------------|
| `const string ExtendOption` | `"map_chooser.extend_option"`, the ballot entry that extends the map |
| `bool IsVoteActive { get; }` | Whether a vote is running: end of map, RTV or `!mapsvote` |
| `IReadOnlyList<string> VoteOptions { get; }` | The ballot in menu order: map names, plus `ExtendOption` when offered. Empty with no vote |
| `int GetVoteCount(string option)` | Ballots for an option. `0` off the ballot or with no vote |
| `int TotalVotes { get; }` | Ballots cast in the running vote |
| `string? GetPlayerVote(int playerSlot)` | That player's option, or `null` |
| `int VoteSecondsLeft { get; }` | Seconds until the vote closes, rounded up. `0` with no vote. 1.5.0+ |
| `int EligibleVoters { get; }` | Players who may vote: humans, spectators only with `AllowSpectatorsToVote`. `0` with no vote. 1.5.0+ |
| `event Action? VoteChanged` | A vote started, ended or was cancelled, or a ballot was cast, changed or withdrawn |
| `event Action<int, string>? PlayerVoted` | `(playerSlot, option)` of a ballot cast or changed, after its `VoteChanged` |

### Map pool

| Member | Description |
|--------|-------------|
| `IReadOnlyList<MapChooserMap> Maps { get; }` | The pool, in `maps.jsonc` order |
| `MapChooserMap? GetMap(string name)` | A pool map by the name menus show, case-insensitive. `null` for any other name, `ExtendOption` included |
| `MapChooserAvailability GetAvailability(string name)` | Whether a pool map can be nominated or voted for now, and if not, why. 1.5.0+ |
| `string? NextMap { get; }` | The next map, as `!nextmap` reports it: a vote's or an admin's pick, else the cycle's next while `Cycle.Enabled` is on and `RandomOrder` off. `null` while undecided. 1.6.0+ |

```csharp
public sealed record MapChooserMap(string Name, string? MapName, string? WorkshopId, int MinPlayers, int MaxPlayers);
```

`MapName` is the map the server loads, `null` for a workshop map, which has a `WorkshopId` instead. `0` = no player limit. `MapChooserAvailability` (1.5.0+), in check order: `NotInPool` (a name `GetMap` doesn't resolve), `CurrentMap`, `Cooldown`, `TooFewPlayers`, `TooManyPlayers` (bots not counted), else `Available`.

### Map clock and extends

Requires MapChooser 1.5.0+.

| Member | Description |
|--------|-------------|
| `int ExtendsLeft { get; }` | Extends left on this map |
| `int ExtendMinutes { get; }` | Minutes an extend adds: `EndOfMap.ExtendTimeStep`. `0` without `mp_timelimit` |
| `int ExtendRounds { get; }` | Rounds an extend adds: `EndOfMap.ExtendRoundStep`. `0` without `mp_maxrounds` |
| `int? MapSecondsLeft { get; }` | Seconds left, as `!timeleft` counts them, never below `0`. `null` without `mp_timelimit`, or in warmup |
| `int? MapRoundsLeft { get; }` | Rounds left, the current one included, as `!timeleft` counts them, never below `0`. `null` without `mp_maxrounds`, or in warmup |

### Nominations

Requires MapChooser 1.5.0+.

| Member | Description |
|--------|-------------|
| `IReadOnlyList<string> Nominations { get; }` | Maps nominated for the next vote, each once, by the name `GetMap` takes |
| `string? GetPlayerNomination(int playerSlot)` | That player's nomination, or `null` |
| `event Action? NominationsChanged` | A nomination was made, replaced or cleared. They clear on every map load |

### Menus

`MapChooserMenuIds` (1.5.0+), each a `const string`:

| Member | Description |
|--------|-------------|
| `Hub` | `"mapchooser.hub"`, the map menu |
| `Vote` | `"mapchooser.eof-vote"`, the ballot |
| `LockedVote` | `"mapchooser.eof-vote.locked"`, the ballot with `DisableVoteMenuExit` on |
| `Nominate` | `"mapchooser.nominate"` |
| `Votemap` | `"mapchooser.votemap"`, not in the map menu |
| `ChangeMap` | `"mapchooser.admin-change-map"` |
| `SetNextMap` | `"mapchooser.set-next-map"` |
| `StartVote` | `"mapchooser.admin-maps-vote"` |
| `Cycle` | `"mapchooser.cycle"` |
| `CycleMap` | `"mapchooser.cycle-map-actions"`, a map's page. 1.6.0+ |
| `CycleRemove` | `"mapchooser.cycle-remove-confirm"`, **Yes, Remove** then **Cancel**. 1.6.0+ |

`MapChooserCycleActions` (1.6.0+): `MoveUp`, `MoveDown` and `Remove`, which a `Cycle` row takes besides Select. Declared with no key; a server binds them — [Keys](#keys) — and a renderer sends one through `IMenuComponent.HandleActionAsync`. A row returns `false` for a move its map can't make. `Remove` opens `CycleRemove`.

For a renderer:

- Ballot rows carry their option as `Tag`, a map name or `ExtendOption`, for `GetVoteCount` and `GetPlayerVote`. Map rows elsewhere carry the map's name, for `GetMap`.
- A `Nominate` row that can't be picked is disabled, with its translated reason as `Comment`; `GetAvailability` gives it as data.
- On `StartVote`, maps are `ToggleComponent`s, ticked per player; the start button is the one `ButtonComponent`, and its `GetHint` says what it does or why it can't.
- `Hub` entries are `SubmenuComponent`s whose `Id` is their page's id. Pages open with `Hub` as their `Parent`, `LockedVote` excepted.

---

## 🧩 Using the API

### 1. Reference the contract

`MapChooser.Contract.dll` is in the release, under `resources/exports/`.

```xml
<Reference Include="MapChooser.Contract">
  <HintPath>path\to\MapChooser.Contract.dll</HintPath>
  <Private>false</Private>
</Reference>
```

`Private` must be `false`: a copy beside your plugin can load as a second `IMapChooserApi` that never resolves, with no build error.

### 2. Get the interface

SwiftlyS2 re-runs `UseSharedInterface` whenever a plugin loads or unloads, and MapChooser drops every subscriber just before: subscribe on every run. While MapChooser isn't loaded, the field is `null`.

```csharp
private IMapChooserApi? _mapChooser;

public override void UseSharedInterface(IInterfaceManager interfaceManager)
{
    if (!interfaceManager.TryGetSharedInterface<IMapChooserApi>(IMapChooserApi.InterfaceKey, out _mapChooser))
        return;

    _mapChooser.VoteChanged += OnVoteChanged;
    _mapChooser.PlayerVoted += OnPlayerVoted;
}
```

### When MapChooser is optional

Without MapChooser, `MapChooser.Contract.dll` is missing, and a method that names a contract type throws as soon as it is compiled — in its caller, where its own `try` can't catch it. From `UseSharedInterface`, that stops the handshake for every plugin after yours. Hold the API as `object?`, name the contract types in one class only, mark its methods `[MethodImpl(MethodImplOptions.NoInlining)]`, and call into it only once `HasSharedInterface("MapChooser.Api.v1")` is `true`.

### Checklist

- [ ] `MapChooser.Contract` referenced with `Private` set to `false`
- [ ] Events subscribed in `UseSharedInterface`, on every run, and a `null` interface handled
- [ ] Game thread only, and no entities touched by handlers during map load
- [ ] Members marked 1.5.0+ or 1.6.0+ used only with that MapChooser
- [ ] Contract types in one `NoInlining` class, held as `object?`, if MapChooser is optional

---

## 🔧 Building

### Requirements

| What | Version |
|------|---------|
| .NET SDK | 10 |
| SwiftlyS2 | Restored from NuGet |

### Build

```bash
git clone https://github.com/SwiftlyS2-Plugins/MapChooser.git
cd MapChooser
dotnet publish -c Release
```

This writes the plugin to `build/publish/MapChooser/`, with `MapChooser.Contract.dll` in `resources/exports/` and beside `MapChooser.dll`, and zips it to `build/MapChooser.zip`. Unzip that into `addons/swiftlys2/plugins/`.

---

## 🩺 Troubleshooting

Most console lines start with `MapChooser:`. `DetailedLogging` adds map-change decisions.

| Problem | Cause | Solution |
|---------|-------|----------|
| `!maplist` replies `There are no maps configured.` | `maps.jsonc` is empty, as on first start | List maps — [maps.jsonc](#mapsjsonc) — and restart, or use `!addmap` |
| An edit to `config.jsonc` or `maps.jsonc` did nothing | Both are read only when MapChooser loads | Restart the server |
| `MapChooser: Commands.MapList lists 'maps', which Commands.Menu already registers, so it was skipped; it can be removed from config.jsonc.` | An older `config.jsonc` has `MapList` `"maplist,maps"` | Expected: `!maps` opens the map menu. Remove `maps` from `MapList` to silence it |
| `!rtv`, `!nominate`, `!votemap` or `!ext` does nothing, with no reply | `Rtv.Enabled`, `Rtv.NominationEnabled`, `Votemap.Enabled` or `ExtendMap.Enabled` is `false` | Set it `true` |
| `Command disabled during warmup.` | `Rtv.EnabledInWarmup` or `ExtendMap.EnabledInWarmup` is `false`. `!nominate` and `!timeleft` never work in warmup | Set it `true`, or wait for warmup to end |
| `You cannot vote while in spectator.` | `AllowSpectatorsToVote` is `false` | Join a team, or set it `true` |
| No end-of-map vote | `EndOfMap.Enabled` is `false`, or the map has no `mp_timelimit`, `mp_maxrounds` or `mp_winlimit` | Set it `true`, or set a limit |
| The cycle picks a recent map, or one outside its player limits | The cycle ignores `MapsInCooldown`, `MinPlayers` and `MaxPlayers` | Expected |
| `!map` or `!votemap` also runs another plugin's command | Another plugin registers the same name, such as Admins with `map` and `votemap`. SwiftlyS2 runs both | Remove the name from one plugin: here, from `Commands.ChangeMap` or `Commands.Votemap` |
| `MapChooser: a <event> subscriber (<type>) threw; the vote is unaffected.` | Another plugin's handler failed | Report it to that plugin |

---

## 🤝 Report issues or request features

- **Issues**: [GitHub Issues](https://github.com/SwiftlyS2-Plugins/MapChooser/issues)
