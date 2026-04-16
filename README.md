# GPDebugger

A comprehensive debugging plugin for the **EXILED framework** in SCP: Secret Laboratory.
It dynamically tracks all events occurring in-game using Reflection and outputs the property values of those events directly to the in-game client console.

https://github.com/user-attachments/assets/9e3a9faa-1f22-44f8-881e-cc120dbbad31

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

## 💻 Command Guide

You can use the following commands in the Remote Admin console:

| Command | Description |
|---|---|
| `gpdebug start` | Activates debug mode and starts outputting all subscribed EXILED events to the console. |
| `gpdebug stop` | Deactivates debug mode (stops receiving events). |
| `gpdebug handler add <HandlerName>` | Adds a handler to monitor. (e.g., `gpdebug handler add Player`)<br>* If at least one handler is added, only events from those handlers will be filtered and output. (Case-insensitive) |
| `gpdebug handler remove <HandlerName>` | Removes the specified handler from the filter list. |
| `gpdebug ignore add <EventName>` | Adds a specific event to the ignore list so it won't be printed. (e.g., `gpdebug ignore add Player.MakingNoiseEventArgs`) |
| `gpdebug ignore remove <EventName>` | Removes a specific event from the ignore list. |

## ⚙️ Configuration

Below are the available configuration options for `GPDebug`:

```yml
gp_debug:
  is_enabled: true
  debug: false
  # The maximum length of a message to show in the console.
  console_message_length_limit: 100
  # The color of the console messages.
  console_message_color: 'white'
  # List of events to ignore from being printed. (ex. Player.MakingNoiseEventArgs)
  ignored_events:
  - 'Player.MakingNoiseEventArgs'
  - 'Player.UsingMicrophoneEventArgs'
```

## 🛠 Requirements

- [EXILED Framework](https://github.com/Exiled-Team/EXILED)

## 👨‍💻 Author
- **GoldenPig1205**
