using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings.Runtime;
using Nox.Terminal;

namespace Nox.Settings.Commands {
	public class SettingsCommand : ICommand, IHelper {
		public string GetName()
			=> "settings";

		public string GetDescription()
			=> LanguageManager.Get($"terminal.command.{GetName()}.description");

		public string GetShort()
			=> LanguageManager.Get($"terminal.command.{GetName()}.short");

		public string GetUsage()
			=> $"{CommandWithPrefix} <list|get|set|trigger> [path] [value]";

		private string CommandWithPrefix
			=> GetName();

		private readonly string[] _subCommands = { "list", "get", "set", "trigger" };

		public string[] AutoComplete(string input, Terminal.IContext context = null) {
			// The context is not required: the suggestions only depend on the registered handlers.
			if (string.IsNullOrWhiteSpace(input))
				return Array.Empty<string>();

			var endsWithSpace = input.EndsWith(' ');
			var parts         = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

			// Still typing the command name ("set" must not be completed to "settings" once a space was
			// typed: "set " is another command).
			if (parts.Length == 1 && !endsWithSpace)
				return CommandWithPrefix.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase)
					? new[] { CommandWithPrefix + " " }
					: Array.Empty<string>();

			// Not our command.
			if (!parts[0].Equals(CommandWithPrefix, StringComparison.OrdinalIgnoreCase))
				return Array.Empty<string>();

			// "settings " → every subcommand.
			if (parts.Length == 1)
				return _subCommands
					.Select(sc => $"{CommandWithPrefix} {sc} ")
					.ToArray();

			// "settings se" → the matching subcommands.
			if (parts.Length == 2 && !endsWithSpace)
				return _subCommands
					.Where(sc => sc.StartsWith(parts[1], StringComparison.OrdinalIgnoreCase))
					.Select(sc => $"{CommandWithPrefix} {sc} ")
					.ToArray();

			// For "get" and "set", suggest handler paths
			if (parts.Length >= 2) {
				var subCommand = parts[1].ToLower();

				if (subCommand is "get" or "set" or "trigger") {
					// "settings get accessibility.interface.language " : the path is complete, there is
					// nothing more to suggest (a "set" expects a value that cannot be guessed).
					if (input.EndsWith(' ') && parts.Length >= 3)
						return Array.Empty<string>();

					var partial = parts.Length >= 3 ? parts[2] : string.Empty;
					var paths   = new List<string>();

					foreach (var handler in Main.Handlers) {
						// Only the handler path is read here: IsActive/IsTriggerable depend on running systems
						// (XR, controllers…) that do not exist out of Play mode, and used to throw.
						try {
							if (subCommand == "trigger" && !handler.IsTriggerable)
								continue;

							var path = string.Join(".", handler.Path);

							if (string.IsNullOrEmpty(path) || !path.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
								continue;

							// The path alone: the panel appends it to the words already typed.
							paths.Add(path);
						} catch (Exception) {
							// A handler that cannot even expose its path is simply not suggested.
						}
					}

					return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
				}
			}

			return Array.Empty<string>();
		}

		public UniTask<bool> Execute(string input, Terminal.IContext context = null)
			=> UniTask.FromResult(ExecuteInternal(input, context));

		private bool ExecuteInternal(string input, Terminal.IContext context = null) {
			if (string.IsNullOrWhiteSpace(input) || context == null)
				return false;

			var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (!parts[0].Equals(CommandWithPrefix, StringComparison.OrdinalIgnoreCase))
				return false;

			var printing = context.CanPrinting();

			if (parts.Length < 2) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.usage"));
				return true;
			}

			var subCommand = parts[1].ToLower();

			switch (subCommand) {
				case "list":
					return HandleList(context, printing);
				case "get":
					return HandleGet(parts, context, printing);
				case "set":
					return HandleSet(parts, context, printing);
				case "trigger":
					return HandleTrigger(parts, context, printing);
				default:
					if (printing)
						context.PrintLn(LanguageManager.Get("terminal.command.settings.invalid_subcommand", new object[] { subCommand }));
					return true;
			}
		}

		private static bool HandleList(Terminal.IContext context, bool printing) {
			var handlers = new List<IHandler>();

			foreach (var handler in Main.Handlers) {
				// IsActive can depend on a running system (XR, controllers…) missing out of Play mode.
				try {
					if (handler.IsActive)
						handlers.Add(handler);
				} catch (Exception) {
					// Not listed, but it does not break the command.
				}
			}

			if (handlers.Count == 0) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.list.empty"));
				context.SetResult(handlers);
				return true;
			}

			if (printing) {
				context.PrintLn(LanguageManager.Get("terminal.command.settings.list.header", handlers.Count));
				foreach (var handler in handlers) {
					var path = string.Join(".", handler.Path);

					try {
						if (handler.IsTriggerable)
							context.PrintLn(LanguageManager.Get("terminal.command.settings.list.item_trigger", new object[] { path }));
						else
							context.PrintLn(LanguageManager.Get("terminal.command.settings.list.item", new object[] { path, handler.Value.ToVisualString() }));
					} catch (Exception) {
						// Value unavailable (system not running): the path is still listed.
						context.PrintLn(LanguageManager.Get("terminal.command.settings.list.item_trigger", new object[] { path }));
					}
				}
			}

			context.SetResult(handlers);
			return true;
		}

		private static bool HandleGet(string[] parts, Terminal.IContext context, bool printing) {
			if (parts.Length < 3) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.get.usage"));
				return true;
			}

			var pathStr = parts[2];
			var path = pathStr.Split('.');

			var handler = Main.Instance.Get(path);

			if (handler == null) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.get.not_found", new object[] { pathStr }));
				context.SetResult(null);
				return true;
			}

			var value = handler.Value;

			// The stored value wins: it is what the setting will be on the next launch, and handlers
			// whose UI is not built yet (editor) only have a default one.
			var stored = Config.Load().Get(new[] { "settings" }.Concat(path).ToArray());

			if (stored != null)
				value = stored.ToObject<object>();

			if (printing)
				context.PrintLn(LanguageManager.Get("terminal.command.settings.get.value", new object[] { value.ToVisualString() }));

			context.SetResult(value);
			return true;
		}

		private static bool HandleSet(string[] parts, Terminal.IContext context, bool printing) {
			if (parts.Length < 4) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.set.usage"));
				return true;
			}

			var pathStr = parts[2];
			var path = pathStr.Split('.');
			var valueStr = parts[3];

			var handler = Main.Instance.Get(path);

			if (handler == null) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.set.not_found", new object[] { pathStr }));
				context.SetResult(false);
				return true;
			}

			// The handler applies AND persists the value (its OnValueChanged does both): the command
			// only reports the outcome.
			try {
				object parsedValue;

				// Try parsing as number first, then bool, then string
				if (float.TryParse(valueStr, out var floatValue))
					parsedValue = floatValue;
				else if (bool.TryParse(valueStr, out var boolValue))
					parsedValue = boolValue;
				else
					parsedValue = valueStr;

				if (!handler.Apply(parsedValue)) {
					if (printing)
						context.PrintLn(LanguageManager.Get("terminal.command.settings.set.rejected", new object[] { pathStr }));

					context.SetResult(false);
					return true;
				}

				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.set.success", new object[] { pathStr, valueStr }));

				context.SetResult(parsedValue);
			} catch (Exception ex) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.set.error", new object[] { ex.Message }));
				context.SetResult(false);
			}

			return true;
		}

		private static bool HandleTrigger(string[] parts, Terminal.IContext context, bool printing) {
			if (parts.Length < 3) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.trigger.usage"));
				return true;
			}

			var pathStr = parts[2];
			var path = pathStr.Split('.');

			var handler = Main.Instance.Get(path);

			if (handler == null) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.trigger.not_found", new object[] { pathStr }));
				context.SetResult(false);
				return true;
			}

			if (!handler.IsTriggerable) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.trigger.not_triggerable", new object[] { pathStr }));
				context.SetResult(false);
				return true;
			}

			handler.OnClick(new Nox.CCK.Settings.Context());

			if (printing)
				context.PrintLn(LanguageManager.Get("terminal.command.settings.trigger.success", new object[] { pathStr }));

			context.SetResult(true);
			return true;
		}
	}
}
