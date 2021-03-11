using System;
using System.Collections.Generic;
using UnityEngine;

namespace TAO.Console
{
	public class ConsoleWindow : Window
	{
		[Space]

		[SerializeField]
		private KeyCode toggleKey = KeyCode.Backslash;
		[SerializeField]
		private KeyCode enterKey = KeyCode.Return;
		[SerializeField]
		private KeyCode suggestionCompleteKey = KeyCode.Tab;
		[SerializeField]
		private KeyCode suggestionUpKey = KeyCode.UpArrow;
		[SerializeField]
		private KeyCode suggestionDownKey = KeyCode.DownArrow;

		private string command = "";
		private int suggestionIndex = -1;

		private const string inputControlName = "inputControl";

		public Dictionary<string, Command> Commands { get; } = new Dictionary<string, Command>();
		private readonly List<string> suggestions = new List<string>();

		protected override void Awake()
		{
			base.Awake();

			if (IsEnabled())
			{
				windowExpandHeight = true;
				windowName = string.Format("Console - {0} v{1}", Application.productName, Application.version);

				// Add commands.
				AddCommand("Console.Toggle", Toggle);
			}
			else
			{
				enabled = false;
			}
		}

		protected override void OnGUI()
		{
			if (IsEnabled())
			{
				Event e = Event.current;

				if (e.type == EventType.KeyDown && e.isKey && e.keyCode == toggleKey)
				{
					Toggle();
					e.Use();
				}

			}

			base.OnGUI();
		}

		protected override void OnWindow(int id)
		{
			base.OnWindow(id);

			using (new GUILayout.VerticalScope())
			{
				Event e = Event.current;

				ConsoleMenu(e);

				using (new GUILayout.VerticalScope())
				{
					// Handle input.
					if (e.isKey && e.type == EventType.KeyDown && e.keyCode == enterKey)
					{
						ExcuteCommand(command);
					}

					if (e.isKey && e.type == EventType.KeyDown && e.keyCode == suggestionUpKey)
					{
						suggestionIndex = Mathf.Clamp(suggestionIndex - 1, 0, suggestions.Count - 1);
						GUI.FocusControl(inputControlName);
						e.Use();
					}

					if (e.isKey && e.type == EventType.KeyDown && e.keyCode == suggestionDownKey)
					{
						suggestionIndex = Mathf.Clamp(suggestionIndex + 1, 0, suggestions.Count - 1);
						GUI.FocusControl(inputControlName);
						e.Use();
					}

					// Input field.
					GUI.SetNextControlName(inputControlName);
					command = GUILayout.TextField(command, GUILayout.ExpandWidth(true));
				}

				ConsoleSuggestions(e);
			}
		}
		
		protected override void UpdateWindowRect()
		{
			rect.width = Screen.width;
			rect.height = 0;
		}

		private void ConsoleMenu(Event e)
		{
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Close"))
				{
					isOpen = false;
				}
			}
		}

		private void ConsoleSuggestions(Event e)
		{
			// Get suggestions.
			suggestions.Clear();
			if (!string.IsNullOrWhiteSpace(command))
			{
				foreach (string k in Commands.Keys)
				{
					if (k.StartsWith(command, true, null))
					{
						suggestions.Add(k);
					}
				}

				// Select first if we didn't have a suggestion already.
				if (suggestions.Count != 0 && suggestionIndex == -1)
				{
					suggestionIndex = 0;
				}
			}

			if (suggestions.Count != 0 && suggestionIndex != -1)
			{
				if (e.type == EventType.KeyDown && e.isKey && e.keyCode == suggestionCompleteKey)
				{
					command = suggestions[suggestionIndex];
					e.Use();
				}
			}

			// Draw suggestions.
			if (suggestions.Count != 0)
			{
				// Draw suggestions.
				using (new GUILayout.VerticalScope())
				{
					for (int i = 0; i < suggestions.Count; i++)
					{
						if (i == suggestionIndex)
						{
							GUI.color = Color.white;
							GUILayout.Label(string.Format("→ {0}", suggestions[i]));
						}
						else
						{
							GUI.color = Color.gray;
							GUILayout.Label(suggestions[i]);
						}
					}
				}
			}
			else
			{
				suggestionIndex = -1;
			}
		}

		public bool AddCommand(Command command)
		{
			if (!Commands.ContainsKey(command.name))
			{
				Commands.Add(command.name, command);
				return true;
			}

			return false;
		}

		public bool AddCommand(string name, Action action)
		{
			return AddCommand(new Command(name, action));
		}

		public bool AddCommand(Action action)
		{
			return AddCommand(action.Method.Name, action);
		}

		public void RemoveCommand(string name)
		{
			Commands.Remove(name);
		}

		public void ExcuteCommand(string name)
		{
			if (Commands.TryGetValue(name, out Command c))
			{
				this.command = "";

				// Execute command.
				c.action.Invoke();
			}
		}
	}

	public struct Command
	{
		public string name;
		public Action action;
		public string description;

		public Command(string name, Action action, string description = "")
		{
			this.name = name;
			this.action = action;
			this.description = description;
		}
	}
}