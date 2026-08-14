# GPDebugger

A debugging plugin for the **EXILED framework** in SCP: Secret Laboratory.
It captures event logs, handler activity, and network activity, then prints readable output to the in-game client console.



https://github.com/user-attachments/assets/d0eafc0c-2b5f-4a38-ad05-4150d8e74eec



## Features

- **Event logging**: Automatically subscribes to `Exiled.Events.Handlers` events and prints their values.
- **Handler filtering**: Restrict logging to selected handlers with `handler_whitelist`, or hide specific handlers at runtime.
- **Event ignoring**: Ignore spammy event names from the console output.
- **Network logging**: Track network methods and network messages in real time.
- **Network ignoring**: Ignore specific network method or message names from the console output.
- **Object inspection**: Use `gpdebug print hit` to inspect the object you are looking at.
- **Component inspection**: Use `gpdebug print player/hit ComponentName` to inspect a specific component on a player or game object.
- **Feature inspection**: Use `gpdebug print <class>` or `gpdebug print player` to inspect Exiled feature classes or players.
- **Search**: Use `gpdebug search <name>` to find scene transforms by name, inspect their position/size, and teleport to a numbered result.
- **Live pointer inspection**: Use `gpdebug pointer on` to show detailed information about the Transform under your crosshair in a top-left HintServiceMeow overlay.

## Commands

Use these commands in the Remote Admin console.

| Command | Description |
|---|---|
| `gpdebug` or `gpdebug help` | Show detailed help. |
| `gpdebug handler start` | Enable event handler logging for you. |
| `gpdebug handler stop` | Disable event handler logging for you. |
| `gpdebug handler ignore add <HandlerName>` | Ignore a handler from event logging. |
| `gpdebug handler ignore remove <HandlerName>` | Remove a handler from the ignore list. |
| `gpdebug handler list` | Show handler whitelist, ignored handlers, active handlers, and available `Exiled.API.Features` classes. |
| `gpdebug network start` | Enable network logging for you. |
| `gpdebug network stop` | Disable network logging for you. |
| `gpdebug network ignore add <Name>` | Ignore a network method or network message. |
| `gpdebug network ignore remove <Name>` | Remove a network method or network message from the ignore list. |
| `gpdebug network list` | Show ignored and active network methods/messages. |
| `gpdebug pointer on` | Enable the live pointer Transform inspector. |
| `gpdebug pointer off` | Disable the live pointer Transform inspector. |
| `gpdebug print <class>` | Print public static properties of an Exiled feature class. |
| `gpdebug print player [playerName]` | Print player properties for yourself or a target player. |
| `gpdebug print player [playerName] <ComponentName>` | Print component properties of a player. |
| `gpdebug print hit` | Inspect the object you are looking at and matched Exiled API features. |
| `gpdebug print hit <ComponentName>` | Inspect a specific component on the object you are looking at. |
| `gpdebug search <name>` | Search scene transforms by name and list numbered results with position, rotation, scale, and bounds size. |
| `gpdebug search <name> <number>` | Teleport yourself to the numbered search result. |

### Print Command Examples

```
gpdebug print player                           # Print your own player properties
gpdebug print player 8                         # Print player ID 8's properties
gpdebug print player CharacterController       # Inspect your CharacterController component
gpdebug print player 8 Rigidbody               # Inspect player ID 8's Rigidbody component
gpdebug print hit                              # Inspect the object you are looking at
gpdebug print hit Transform                    # Inspect the Transform component of the object you're looking at
gpdebug print Server                           # Print Server class properties
```

### Search Command Examples

```
gpdebug search Door                            # List transforms with names containing Door
gpdebug search Door 3                          # Teleport to result #3 from the Door search
gpdebug search capybara                        # Matches names like capybara, capybara (1), and BigCapybaraDoor
gpdebug search capybara 2                      # Teleport to result #2 from the capybara search
```

## Configuration

```yml
gp_debugger:
  is_enabled: true
  debug: false
  console_message_length_limit: 100
  console_message_color: 'white'
  pointer_inspector_update_interval: 0.25
  pointer_inspector_max_distance: 200
  pointer_inspector_x_coordinate: 0
  pointer_inspector_y_coordinate: 400
  pointer_inspector_font_size: 15
  pointer_inspector_max_lines: 40
  pointer_inspector_transform_selection_radius: 0.35
  pointer_inspector_scene_cache_lifetime: 5
  handler_whitelist: []
  ignored_handlers: []
  ignored_events:
  - 'Player.MakingNoiseEventArgs'
  - 'Player.TriggeringTeslaEventArgs'
  - 'Item.UsingRadioPickupBatteryEventArgs'
  - 'Item.UsingRadioBatteryEventArgs'
  ignored_network_methods:
  - 'TargetReplyEncrypted'
  - 'TargetSyncGameplayData'
  - 'CmdSendEncryptedQuery'
  ignored_network_messages:
  - 'SpawnMessage'
  - 'ObjectDestroyMessage'
  - 'NetworkPingMessage'
  - 'NetworkPongMessage'
  - 'FpcFromClientMessage'
  - 'SubroutineMessage'
  - 'StatMessage'
  - 'VoiceMessage'
  - 'TransmitterPositionMessage'
  - 'ElevatorSyncMsg'
  - 'FpcOverrideMessage'
  - 'TimeSnapshotMessage'
  - 'EntityStateMessage'
  - 'FpcPositionMessage'
  - 'EncryptedMessageOutside'
```

## Notes

- `handler_whitelist` is an allow-list. If it contains items, only those handlers are shown.
- `ignored_handlers` hides specific handlers from handler logging.
- `ignored_events` suppresses specific event names.
- `network` logging is controlled at runtime with `gpdebug network start/stop`.
- `ignored_network_methods` and `ignored_network_messages` hide specific network items.
- Component names are case-sensitive (e.g., `CharacterController`, `Rigidbody`, `Transform`).
- `search` uses case-insensitive partial matching, so `Door` can match `Door`, `Door (1)`, and `BreakableDoor`.
- Search result numbers start at 1. `gpdebug search <name> <number>` teleports you to the numbered result shown by the same search.
- Search output is limited to the first 50 results to keep the Remote Admin console readable.

## Requirements

- [EXILED Framework](https://github.com/Exiled-Team/EXILED)
- HintServiceMeow for EXILED

## Author

- **GoldenPig1205**








