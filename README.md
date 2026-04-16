# GPDebug

A comprehensive debugging plugin for the **EXILED framework** in SCP: Secret Laboratory.
It dynamically tracks all events occurring in-game using Reflection and outputs the property values of those events directly to the in-game client console.

## 🚀 Key Features

- **Automatic Event Subscription**: Automatically finds and subscribes to all events under the `Exiled.Events.Handlers` namespace. Very useful for plugin development to check "when an event is called" and "what values it contains".
- **Detailed Event Logs**: Reads all property values of the event object when an event occurs and outputs them to the console. Limits output to 100 characters per property for readability if it's too long.
- **Handler Filtering**: Allows filtering specific handler events such as `Player`, `Server`, `Scp3114`, `Warhead`, etc. (Supports case-insensitive filtering).

## 💻 Command Guide

You can use the following commands in the Remote Admin console:

| Command | Description |
|---|---|
| `gpdebug start` | Activates debug mode and starts outputting all subscribed EXILED events to the console. |
| `gpdebug stop` | Deactivates debug mode (stops receiving events). |
| `gpdebug handler add <HandlerName>` | Adds a handler to monitor. (e.g., `gpdebug handler add Player`)<br>* If at least one handler is added, only events from those handlers will be filtered and output. (Case-insensitive) |
| `gpdebug handler remove <HandlerName>` | Removes the specified handler from the filter list. |

## 🛠 Requirements

- [EXILED Framework](https://github.com/Exiled-Team/EXILED)

## 👨‍💻 Author
- **GoldenPig1205**
