# GPDebugger

A comprehensive debugging plugin for the **EXILED framework** in SCP: Secret Laboratory.
It dynamically tracks all events occurring in-game using Reflection and outputs the property values of those events directly to the in-game client console.

### 🎯 Who is this for?

**For Plugin Developers:**
Easily discover which in-game interactions trigger which events, making the plugin development process much more convenient.

**For Server Owners:**
Quickly check what kind of events are available to understand what new custom features or plugins can be developed for your server.

## 🚀 Key Features

- **Automatic Event Subscription**: Automatically finds and subscribes to all events under the `Exiled.Events.Handlers` namespace. Very useful for plugin development to check "when an event is called" and "what values it contains".
- **Detailed Event Logs**: Reads all property values of the event object when an event occurs and outputs them to the console. Limits output to a configurable character limit for readability.
- **Handler Filtering**: Allows filtering specific handler events such as `Player`, `Server`, `Scp3114`, `Warhead`, etc. (Supports case-insensitive filtering).
- **Event Ignoring (Anti-Spam)**: Allows ignoring spammy events (e.g., `Player.MakingNoiseEventArgs`) from flooding the console, either via command or config.
- **Customizable Output**: Configure text size, colors, and property character length limits to keep logs clean and readable.
- **In-game Object Inspector**: Use `gpdebug print hit` to inspect objects you are looking at via Raycast. It automatically finds corresponding Exiled API objects (like `Door`, `Room`, `Pickup`) and prints their properties.
- **Class/Player Properties Printing**: Use `gpdebug print <class>` or `gpdebug print player` to dump all properties of an Exiled static feature class or a specific player directly to your console.

## 💻 Command Guide

You can use the following commands in the Remote Admin console:

| Command | Description |
|---|---|
| `gpdebug help` or `gpdebug` | Help is displayed. |
| `gpdebug start` | Activates debug mode and starts outputting all subscribed EXILED events to the console. |
| `gpdebug stop` | Deactivates debug mode (stops receiving events). |
| `gpdebug handler add <HandlerName>` | Adds a handler to monitor. (e.g., `gpdebug handler add Player`)<br>* If at least one handler is added, only events from those handlers will be filtered and output. (Case-insensitive) |
| `gpdebug handler remove <HandlerName>` | Removes the specified handler from the filter list. |
| `gpdebug ignore add <EventName>` | Adds a specific event to the ignore list so it won't be printed. (e.g., `gpdebug ignore add Player.MakingNoiseEventArgs`) |
| `gpdebug ignore remove <EventName>` | Removes a specific event from the ignore list. |
| `gpdebug print <class>` | Prints all public static properties of the specified Exiled UI class (e.g., `Server`, `Map`). |
| `gpdebug print player [name/id]` | Prints all properties of the target player (or yourself if no arguments given). |
| `gpdebug print hit` | Casts a ray where you are looking, gets the GameObject, and prints its properties along with any matched Exiled API features (like `BreakableDoor`). |
| `gpdebug list` | Shows a list of all Exiled.API.Features classes that are available for the `gpdebug print <class>` command. |

## ⚙️ Configuration

Below are the available configuration options for `GPDebugger`:

```yml
gp_debugger:
  is_enabled: true
  debug: false
  # The maximum length of a message to show in the console.
  console_message_length_limit: 100
  # The color of the console messages.
  console_message_color: 'white'
  # List of handlers to allow. If this list has at least one value, only these handlers will be logged. (ex. Player, Server)
  handler_whitelist: []
  # List of events to ignore from being printed. (ex. Player.MakingNoiseEventArgs)
  ignored_events:
  - 'Player.MakingNoiseEventArgs'
  - 'Player.UsingMicrophoneEventArgs'
  - 'Item.UsingRadioPickupBatteryEventArgs'
  - 'Item.UsingRadioBatteryEventArgs'
```

## 🛠 Requirements

- [EXILED Framework](https://github.com/Exiled-Team/EXILED)

## 👨‍💻 Author
- **GoldenPig1205**



https://github.com/user-attachments/assets/7fc2baca-8dc2-4f48-b367-fd879703e2e1








