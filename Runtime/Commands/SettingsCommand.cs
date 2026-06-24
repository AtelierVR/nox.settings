using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Settings;
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
			if (context == null || string.IsNullOrWhiteSpace(input))
				return Array.Empty<string>();

			var inputLower = input.ToLower().Trim();
			var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (!inputLower.StartsWith(CommandWithPrefix.ToLower()))
				return CommandWithPrefix.StartsWith(inputLower)
					? new[] { CommandWithPrefix + " " }
					: Array.Empty<string>();

			// Suggest subcommands
			if (parts.Length == 1 || (parts.Length == 2 && !input.EndsWith(' '))) {
				var partial = parts.Length == 2 ? parts[1].ToLower() : "";
				return _subCommands
					.Where(sc => sc.StartsWith(partial))
					.Select(sc => $"{CommandWithPrefix} {sc} ")
					.ToArray();
			}

			// For "get" and "set", suggest handler paths
			if (parts.Length >= 2) {
				var subCommand = parts[1].ToLower();
				if (subCommand is "get" or "set" or "trigger") {
					var partial = parts.Length >= 3 && !input.EndsWith(' ') ? parts[2] : "";
					var handlers = Main.Handlers;
					return handlers
						.Select(h => string.Join(".", h.GetPath()))
						.Where(p => p.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
						.Select(p => $"{CommandWithPrefix} {subCommand} {p}")
						.ToArray();
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
			var handlers = Main.Handlers;

			if (handlers.Count == 0) {
				if (printing)
					context.PrintLn(LanguageManager.Get("terminal.command.settings.list.empty"));
				context.SetResult(handlers);
				return true;
			}

			if (printing) {
				context.PrintLn(LanguageManager.Get("terminal.command.settings.list.header", handlers.Count));
				foreach (var handler in handlers) {
					var path = string.Join(".", handler.GetPath());
					if (handler.IsTriggerable)
						context.PrintLn($"  {path} [trigger]");
					else {
						var value = handler.Value;
						context.PrintLn($"  {path} = {value.ToVisualString()}");
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

			if (printing)
				context.PrintLn($"Value: {value.ToVisualString()}");

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

			// Try to parse and set the value via Config
			var config = Config.Load();
			var configPath = new[] { "settings" }.Concat(path).ToArray();

			try {
				object parsedValue;

				// Try parsing as number first, then bool, then string
				if (float.TryParse(valueStr, out var floatValue))
					parsedValue = floatValue;
				else if (bool.TryParse(valueStr, out var boolValue))
					parsedValue = boolValue;
				else
					parsedValue = valueStr;

				config.Set(configPath, parsedValue);
				config.Save();

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
