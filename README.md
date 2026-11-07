<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong>MapChooser</strong></h2>
  <h3>A powerful and customizable map voting system for SwiftlyS2.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/SwiftlyS2-Plugins/MapChooser/total" alt="Downloads">
  <img src="https://img.shields.io/github/stars/SwiftlyS2-Plugins/MapChooser?style=flat&logo=github" alt="Stars">
  <img src="https://img.shields.io/github/license/SwiftlyS2-Plugins/MapChooser" alt="License">
</p>

## Overview

MapChooser is a map voting plugin for SwiftlyS2. It handles Rock The Vote (RTV), map nominations, and manual voting. It also triggers an automated vote at the end of the map based on time or rounds played.

## Commands

| Command | Description |
| :--- | :--- |
| `!maps` / `!mapmenu` | Opens the map menu: the vote, nominations and the admin pages you have access to (see [Menus](#menus)). |
| `!rtv` | Votes to trigger a map vote immediately (Rock The Vote). |
| `!nominate [map]` / `!nom` | Nominates a map to be included in the next map vote. |
| `!timeleft` | Shows the remaining time or rounds left on the current map. |
| `!nextmap` | Shows which map will be played next (if decided). |
| `!votemap [map]` | Directly votes for a specific map to change to. |
| `!revote` | Reopens the map vote menu if a vote is currently active. |
| `!ext` / `!extendmap` | Votes to extend the current map. |
| `!maplist` | Prints the full configured map list to the console. |
| `!unrtv` | Removes your current RTV vote. |
| `!setnextmap [map]` | Sets the next map directly or opens a selection menu (Admin only). |
| `!mapsvote` | Opens a menu to select multiple maps and start a custom vote (Admin only). |
| `!map` / `!setmap` | Loads requested map (Admin only). |
| `!addmap <name> [id]` | Adds a map to the cycle and saves it to `maps.jsonc` (Admin only). |
| `!removemap <name>` | Removes a map from the cycle and saves the change to `maps.jsonc` (Admin only). |
| `!cyclemenu` / `!mapcycle` | Opens the cycle management menu — view order, move maps up/down, remove (Admin only). |

## Configuration (`config.jsonc`)

### RTV Settings (`Rtv`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `true` | Enable or disable the Rock The Vote system. |
| `EnabledInWarmup` | `false` | Allow players to RTV during warmup. |
| `NominationEnabled` | `true` | Allow players to nominate maps for the vote. |
| `MinPlayers` | `0` | Minimum number of players required to enable RTV. |
| `MinRounds` | `0` | Minimum rounds that must be played before RTV is allowed. |
| `ChangeMapImmediately` | `true` | Change the map immediately after a successful RTV vote. |
| `ChangeMapDelay` | `3` | Delay in seconds before changing the map after a successful RTV vote (used when `ChangeMapImmediately` is `false`). |
| `MapsToShow` | `6` | Number of maps to display in the RTV vote menu. |
| `VoteDuration` | `30` | How long the vote menu remains open (seconds). |
| `VotePercentage` | `60` | Percentage of players required to trigger the vote. |
| `VoteCooldownTime` | `300` | Cooldown time in seconds between failed RTV votes. |

### Votemap Settings (`Votemap`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `true` | Enable or disable the manual `!votemap` command. |
| `VotePercentage` | `60` | Percentage of players required to reach the vote threshold. |
| `ChangeMapImmediately` | `false` | Change map immediately once the threshold is reached. |
| `MinPlayers` | `0` | Minimum players required to use `!votemap`. |

### End Of Map Settings (`EndOfMap`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `true` | Enable or disable the automated vote at the end of the map. |
| `MapsToShow` | `6` | Number of maps to show in the automated vote. |
| `VoteDuration` | `30` | Duration of the automated vote menu. |
| `ChangeMapDelay` | `5` | Delay in seconds before changing the map after the vote ends. |
| `TriggerSecondsBeforeEnd` | `120` | Seconds before timelimit to trigger the vote. |
| `TriggerRoundsBeforeEnd` | `4` | Rounds (or score difference from winning) before map end to trigger the vote. |
| `AllowExtend` | `true` | Allow extending the map from the end-of-map vote menu. |
| `ExtendTimeStep` | `15` | Minutes to add when extending by time. |
| `ExtendRoundStep` | `5` | Rounds to add when extending by rounds. |
| `ExtendLimit` | `3` | Maximum number of times the map can be extended. |

### Extend Map Settings (`ExtendMap`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `true` | Enable or disable the `!extend` command. |
| `EnabledInWarmup` | `false` | Allow players to vote to extend during warmup. |
| `MinPlayers` | `0` | Minimum players required to use `!extend`. |
| `MinRounds` | `0` | Minimum rounds required before `!extend` can be used. |
| `VotePercentage` | `60` | Percentage of players required to pass the extend vote. |

### Cycle Settings (`Cycle`)
| Setting | Default | Description |
| :--- | :--- | :--- |
| `Enabled` | `false` | Enable automatic advance to the next map in the cycle when a match ends without an end-of-map vote. Disabled by default. |
| `RandomOrder` | `false` | Pick the next map randomly from the cycle (excluding maps in cooldown) instead of following the configured order. |
| `AddMapPermission` | `admin.changemap` | Permission flag required for `!addmap`. |
| `RemoveMapPermission` | `admin.changemap` | Permission flag required for `!removemap`. |
| `CycleMenuPermission` | `admin.changemap` | Permission flag required for `!cyclemenu` / `!mapcycle`. |

### Global Settings
| Setting | Default | Description |
| :--- | :--- | :--- |
| `MapsInCooldown` | `3` | Number of recently played maps to exclude from the next vote. |
| `AllowSpectatorsToVote` | `false` | Allow spectators to participate in votes. |
| `AnnounceVotes` | `true` | Broadcast vote counts to all players during voting. |
| `DisableVoteMenuExit` | `false` | When `true`, the map vote menu has no close key, so players must pick a map. The menu closes once they have voted. |
| `DetailedLogging` | `false` | Verbose logging for vote flow and map cycle decisions. |
| `SetNextMapPermission` | `admin.changemap` | Permission flag required for the `!setnextmap` command. |
| `MapsVotePermission` | `admin.mapsvote` | Permission flag required for the `!mapsvote` command. |
| `ChangeMapPermission` | `admin.changemap` | Permission flag required for the `!map` / `!setmap` command. |
| `Maps` | (List) | List of maps available. Workshop maps accept either a bare numeric ID (e.g. `3124567099`) or the explicit `ws:` prefix (e.g. `ws:3124567099`). |

### Commands Settings (`Commands`)

All command aliases are fully configurable. Each setting accepts a comma-separated list of aliases.

| Setting | Default | Description |
| :--- | :--- | :--- |
| `Menu` | `maps,mapmenu` | Aliases for the map menu command. |
| `Rtv` | `rtv` | Aliases for the Rock The Vote command. |
| `UnRtv` | `unrtv` | Aliases for the undo RTV command. |
| `Nominate` | `nominate,nom,yd` | Aliases for the nominate command. |
| `Timeleft` | `timeleft` | Aliases for the timeleft command. |
| `Nextmap` | `nextmap` | Aliases for the nextmap command. |
| `Votemap` | `votemap` | Aliases for the votemap command. |
| `Revote` | `revote` | Aliases for the revote command. |
| `SetNextMap` | `setnextmap` | Aliases for the set next map command. |
| `Extend` | `ext,extendmap` | Aliases for the extend map command. |
| `MapsVote` | `mapsvote` | Aliases for the admin maps vote command. |
| `ChangeMap` | `map,setmap` | Aliases for the admin change map command. |
| `MapList` | `maplist` | Aliases for the map list command. |
| `AddMap` | `addmap` | Aliases for the add-map admin command. |
| `RemoveMap` | `removemap` | Aliases for the remove-map admin command. |
| `CycleMenu` | `cyclemenu,mapcycle` | Aliases for the cycle management menu command. |

An alias is registered once, for the first setting that lists it, starting with `Menu`; a later setting's copy is skipped and logged with the setting to clean up. That is what happens to `maps` in an older `config.jsonc`, whose `MapList` is still `maplist,maps`: `!maps` opens the map menu, and `maps` can be removed from `MapList`.

### Map Configuration

Each map entry supports the following properties:

| Property | Default | Description |
| :--- | :--- | :--- |
| `Name` | (required) | Display name shown in menus and chat. |
| `Id` | (required) | Map identifier. Use the map name (e.g. `de_dust2`) or a workshop ID (e.g. `3124567099`). |
| `MinPlayers` | `0` | Minimum number of real players required for this map to appear in votes and nominations. `0` means no minimum. |
| `MaxPlayers` | `0` | Maximum number of real players allowed for this map to appear in votes and nominations. `0` means no maximum. |

> **Note:** Maps without `MinPlayers`/`MaxPlayers` (or set to `0`) are considered valid for any number of players. If you want a map to **only** appear when there are few players, you must also set `MinPlayers` on your other maps to exclude them at low player counts. Bots are not counted.

#### Example
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
                "Name": "Inferno Night",
                "Id": "3124567099",
                "MinPlayers": 4
            },
            {
                "Name": "Mirage",
                "Id": "de_mirage",
                "MinPlayers": 1,
                "MaxPlayers": 3
            },
            {
                "Name": "Overpass",
                "Id": "de_overpass",
                "MinPlayers": 1,
                "MaxPlayers": 3
            }
        ]
    }
}
```

In this example, when there are 1–3 real players on the server, only `Mirage` and `Overpass` will appear in votes and nominations. Once there are 4+ players, only `Dust II` and `Inferno Night` will be available.

## Map Cycle

When a match ends and no end-of-map vote fires, the plugin can automatically advance to the next map in the cycle. This is controlled by `Cycle.Enabled` and is **disabled by default** — enable it explicitly if you want the rotation. The cycle order follows `maps.jsonc` by default; set `Cycle.RandomOrder` to `true` to pick the next map at random from the cycle instead. Maps in cooldown are skipped either way. Use `!cyclemenu` to view the order, move maps up/down, or remove them in-game. Use `!addmap` / `!removemap` to manage the list from chat — changes persist to disk immediately.

## Menus

`!maps` opens the map menu, built for each player as it opens. Its entries, in order:

| Entry | Shown |
| :--- | :--- |
| Vote | While a vote is running, to players who may vote in it. Reopens the ballot, as `!revote` does. Left out when `DisableVoteMenuExit` is on. |
| Nominate | While `Rtv.NominationEnabled` is on. |
| Change map | With `ChangeMapPermission`. |
| Set next map | With `SetNextMapPermission`. |
| Start a vote | With `MapsVotePermission`. |
| Map cycle | With `Cycle.CycleMenuPermission`. |

A divider separates the admin entries from Vote and Nominate. A player with no entries gets a line in chat instead.

Every page in the map menu opens as its child — from the menu, from its own command (`!nominate`, `!revote`, `!map`, `!setnextmap`, `!mapsvote`, `!cyclemenu`) or when a vote starts — so a page's close key goes back to the map menu. A page that has done its job — a vote cast, a map nominated or picked, a vote started — still closes outright. Opened from the menu, a page runs its command's checks first, with the same chat replies: Nominate refuses during warmup and to spectators, as `!nominate` does. Two menus open on their own: the ballot when `DisableVoteMenuExit` is on, so the map menu is no way out of it, and `!votemap`, which is not in the map menu.

The nominate page lists every map in the pool but the current one. A map that cannot be nominated right now is greyed out, with the reason as its hint: played recently, or too few or too many players for it.

The Start a vote page (`!mapsvote`) lists every map in the pool as a toggle; ticking one changes it in place, so the page stays where it was. Its start button counts the ticked maps, and its hint says what it does. It stays disabled until a map is ticked, and while a vote is running or counting down to open, when its hint says so instead. Starting closes the page and counts down five seconds in chat before the vote opens; a map change or match start during the countdown cancels it.

## Features

- **Live Vote Counts**: With `AnnounceVotes`, every ballot is announced in chat with the map's running count, and the whole tally is published to other plugins through the [Shared API](#shared-api), so a HUD or menu plugin can draw it live.
- **Map Menu**: `!maps` gathers the vote, nominations and the admin pages in one [menu](#menus).
- **Smart Triggers**: Automated votes trigger based on time remaining, rounds remaining, or when a team is close to winning.
- **Map Cooldown**: Prevents recently played maps from appearing in the vote too soon.
- **Map Extension**: Players can vote to extend the current map time or round limit.
- **Spectator Control**: Configurable option to allow or disallow spectators from voting.
- **Vote Removal**: Players can change their mind and remove their RTV vote with `!unrtv`.
- **Workshop Support**: Seamlessly change to workshop maps using their IDs.
- **Localized**: Full translation support via JSONC files.

## Installation

1. Download the latest release or build the plugin yourself.
2. Copy the published plugin folder to your server:
   `.../game/csgo/addons/swiftlys2/plugins/MapChooser/`
3. Keep the plugin's `resources/` folder alongside the DLL. It includes `resources/exports/MapChooser.Contract.dll`, which other plugins use to read the vote (see [Shared API](#shared-api)).
4. Start/restart the server.

## Building

- Open the project in your preferred .NET IDE (e.g., VS Code, Visual Studio).
- Build the project. The output DLL and resources will be placed in the `build/` directory.
- Building `MapChooser.csproj` builds `MapChooser.Contract` first and copies `MapChooser.Contract.dll` into `build/net10.0/resources/exports/` and beside `MapChooser.dll`. `dotnet publish -c Release` does the same under `build/publish/MapChooser/` and zips the result to `build/MapChooser.zip`.

## Shared API

MapChooser publishes the running map vote to other plugins, read-only, as the shared interface `MapChooser.Api.v1`: the ballot and its tally, the vote's clock and voters, the time left on the map, the nominations, and whether each map can be picked right now. A HUD or menu plugin draws vote counts, share bars, each player's pick and a post-vote tally from it; MapChooser's own menus stay renderer-agnostic and show none of that.

The contract is `MapChooser.Contract.dll` (namespace `MapChooser.Contract`, interface `IMapChooserApi`). It ships in `resources/exports/` and, as a fallback for an install on an already-running server, beside `MapChooser.dll`.

| Member | Description |
| :--- | :--- |
| `const string InterfaceKey` | `"MapChooser.Api.v1"`. |
| `const string ExtendOption` | `"map_chooser.extend_option"` — the ballot entry that extends the current map instead of changing it. |
| `bool IsVoteActive` | `true` while an end-of-map, RTV or `!mapsvote` ballot is open. |
| `IReadOnlyList<string> VoteOptions` | The ballot in menu order: map names, plus `ExtendOption` when extending is offered. Empty when no vote is running. |
| `int GetVoteCount(string option)` | Votes for one ballot entry; `0` for an option not on the ballot, or when no vote is running. |
| `int TotalVotes` | Ballots cast in the running vote — the sum of every `GetVoteCount`. |
| `string? GetPlayerVote(int playerSlot)` | The option that player voted for, or `null`. |
| `event Action? VoteChanged` | The tally changed: a vote started; a ballot was cast, changed or withdrawn (`!unrtv`); or the vote ended or was cancelled, including by a map change or the match going live. |
| `event Action<int, string>? PlayerVoted` | A player cast or changed a ballot: `(playerSlot, option)`. Raised right after that ballot's `VoteChanged`. |
| `IReadOnlyList<MapChooserMap> Maps` | The map pool in config order. |
| `MapChooserMap? GetMap(string name)` | A pool map by the name its menus and ballot show (case-insensitive); `null` for any other name, `ExtendOption` included. |
| `int VoteSecondsLeft` | Seconds until the running vote closes, rounded up; `0` when no vote is running. |
| `int EligibleVoters` | Players who may vote in the running vote: connected humans, spectators only with `AllowSpectatorsToVote`. `0` when no vote is running. |
| `int ExtendsLeft` | How many more times the current map can be extended. |
| `int ExtendMinutes` | Minutes an extend adds to `mp_timelimit` (`EndOfMap.ExtendTimeStep`); `0` when the map has no time limit. |
| `int ExtendRounds` | Rounds an extend adds to `mp_maxrounds` (`EndOfMap.ExtendRoundStep`); `0` when the map has no round limit. |
| `int? MapSecondsLeft` | Seconds left on the map under `mp_timelimit`, as `!timeleft` counts them, never below `0`; `null` without a time limit or during warmup. |
| `int? MapRoundsLeft` | Rounds left under `mp_maxrounds`, the one being played included, as `!timeleft` counts them, never below `0`; `null` without a round limit or during warmup. |
| `IReadOnlyList<string> Nominations` | The maps nominated for the next vote, each once, by the name `GetMap` takes. |
| `string? GetPlayerNomination(int playerSlot)` | The map that player nominated, or `null`. |
| `event Action? NominationsChanged` | A nomination was made, replaced or cleared. Nominations clear on every map load. |
| `MapChooserAvailability GetAvailability(string name)` | Whether a pool map can be nominated or voted for now, and if not, why. |

`MapChooserMap(Name, MapName, WorkshopId, MinPlayers, MaxPlayers)`: `Name` is what menus show; `MapName` is the map the server loads (`de_dust2`, `null` for a workshop map); `WorkshopId` is the Steam Workshop ID (`null` otherwise); `0` means no player limit.

`MapChooserAvailability`, checked in this order: `NotInPool` (any name `GetMap` does not resolve, `ExtendOption` included); `CurrentMap` (the map being played); `Cooldown` (one of the last `MapsInCooldown` maps); `TooFewPlayers` / `TooManyPlayers` (outside its `MinPlayers` / `MaxPlayers`, bots not counted); otherwise `Available`.

`MapChooserMenuIds` holds the ids of MapChooser's menus: `Hub` (`mapchooser.hub`), `Vote` (`mapchooser.eof-vote`), `LockedVote` (`mapchooser.eof-vote.locked`), `Nominate` (`mapchooser.nominate`), `Votemap` (`mapchooser.votemap`), `ChangeMap` (`mapchooser.admin-change-map`), `SetNextMap` (`mapchooser.set-next-map`), `StartVote` (`mapchooser.admin-maps-vote`) and `Cycle` (`mapchooser.cycle`).

- **Needs MapChooser 1.5.0 for the newer members.** Everything in the table from `VoteSecondsLeft` on, `MapChooserAvailability` and `MapChooserMenuIds` came with MapChooser 1.5.0 (contract 1.1). An older MapChooser exports contract 1.0, and a call into a member it lacks throws `MissingMethodException`.
- **Threading.** Every event is raised on the game thread once MapChooser has stored the change — by `PlayerVoted`, the voter's vote menu is already closed — so a handler reads the new state. Read the members on the game thread too. A vote cut short by a map change raises `VoteChanged`, and the nominations' reset raises `NominationsChanged`, from inside map load: read the API there, but leave entities alone.
- **A throwing handler is contained.** It is logged as a warning and skipped; other subscribers and the vote carry on.
- **Subscribe in the handshake, every time.** SwiftlyS2 re-runs `ConfigureSharedInterface` on every plugin load and unload, and MapChooser drops all subscriptions there, as SwiftlyS2's shared-API guidance asks of APIs with events. Subscribe in `UseSharedInterface` or `OnSharedInterfaceInjected` on every call, not once in `Load`. After MapChooser unloads, an instance you still hold reports no vote and raises nothing.
- **Do not ship the contract.** Reference `MapChooser.Contract` with `Private="false"`: a second copy gives `IMapChooserApi` a second type identity, and `TryGetSharedInterface` stops matching. If MapChooser may be absent, the contract DLL is absent too, and the JIT fails any method that names `IMapChooserApi` — hold the API as `object?` and keep those methods `[MethodImpl(MethodImplOptions.NoInlining)]`, reached only after `HasSharedInterface("MapChooser.Api.v1")` returned `true`.
- **Menu rows carry their map.** Each row of the vote menu has its ballot option as the component's `Tag` (the map name, or `ExtendOption`), so a renderer can match a row to `GetVoteCount` and `GetPlayerVote`. The nominate, `!votemap`, change-map, set-next-map, `!mapsvote` and map cycle menus tag each row with its map's name too, for `GetMap`. A nominate row for a map that cannot be nominated now is disabled, with the reason, translated, as its `Comment`; `GetAvailability` gives the same answer as data. On the Start a vote page each map is a `ToggleComponent`, ticked or not per player, and the start button is the page's one `ButtonComponent`; its hint (`GetHint`) says what it does, or why it can't start a vote now.
- **The map menu is navigation.** Each entry of the hub is a plain `SubmenuComponent` whose `Id` is its page's `MapChooserMenuIds` id, and every page in the hub opens with the hub as its `Parent` — the locked ballot excepted (see [Menus](#menus)) — so a renderer can draw the hub's entries beside whichever page is open.

```csharp
// MapChooser installed alongside: the plain binder. Otherwise isolate it, as above.
public override void UseSharedInterface(IInterfaceManager interfaceManager)
{
    interfaceManager.TryGetSharedInterface<IMapChooserApi>(IMapChooserApi.InterfaceKey, out _mapChooser);
    if (_mapChooser is null) return;

    // MapChooser cleared its subscribers before this ran, so these never double up.
    _mapChooser.VoteChanged += OnVoteChanged;
    _mapChooser.PlayerVoted += OnPlayerVoted;
}
```
