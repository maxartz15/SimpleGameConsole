using System;
using UnityEngine;
using System.Collections.Generic;

namespace TAO.Console
{
	public class GameConsole : MonoBehaviour
	{
		[SerializeField]
		private bool displayLogs = true;
		[SerializeField]
		private bool displayWarnings = true;
		[SerializeField]
		private bool displayErrors = true;

		[Space]

	#pragma warning disable CS0414
		[SerializeField]
		private bool showInEditor = false;
		[SerializeField]
		private bool showInDebug = true;
		[SerializeField]
		private bool showInRelease = true;
	#pragma warning restore CS0414

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
		[SerializeField]
		private GUISkin skin = null;

		// GUI.
		private bool isOpen = false;
		private bool isLogOpen = false;
		private bool showLogs = true;
		private bool showWarnings = true;
		private bool showErrors = true;
		private string command = "";
		private int suggestionIndex = -1;
		private Vector2 logScrollPosition = Vector2.zero;

		Rect consoleRect = new Rect(0, 0, 0, 0);
		Rect logRect = new Rect(0, 0, 0, 0);

		private const string inputControlName = "inputControl";
		private string consoleWindowName = "Console";

		// Data.
		private List<LogMessage> log = new List<LogMessage>();
		private Dictionary<string, Command> commands = new Dictionary<string, Command>();
		private List<string> suggestions = new List<string>();

		private void Awake()
		{
			if (IsEnabled())
			{
				consoleWindowName = string.Format("Console - {0} v{1}", Application.productName, Application.version);

				// Add base commands.
				AddCommand(Help);
				AddCommand(Clear);
				AddCommand(Close);
				AddCommand(SysInfo);
				AddCommand(GameInfo);
				AddCommand(ToggleConsole);
				AddCommand(ToggleLog);

				// Subscribe to Debug.Log.
				Application.logMessageReceived += OnLog;
			}
			else
			{
				enabled = false;
			}
		}

		public void Log(LogMessage logEntry)
		{
			log.Add(logEntry);
		}
	
		public void Log(string message, LogType logType)
		{
			Log(new LogMessage(message, "",logType));
		}

		private void OnLog(string message, string stackTrace, LogType type)
		{
			switch (type)
			{
				case LogType.Error:
					if (displayErrors)
					{
						log.Add(new LogMessage(message, stackTrace, type));
					}
					break;
				case LogType.Assert:
					if (displayErrors)
					{
						log.Add(new LogMessage(message, stackTrace, type));
					}
					break;
				case LogType.Warning:
					if (displayWarnings)
					{
						log.Add(new LogMessage(message, stackTrace, type));
					}
					break;
				case LogType.Log:
					if (displayLogs)
					{
						log.Add(new LogMessage(message, stackTrace, type));
					}
					break;
				case LogType.Exception:
					if (displayErrors)
					{
						log.Add(new LogMessage(message, stackTrace, type));
					}
					break;
				default:
					break;
			}

			logScrollPosition.y = 99999f;
		}

		public bool AddCommand(Command command)
		{
			if (!commands.ContainsKey(command.name))
			{
				commands.Add(command.name, command);
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
			commands.Remove(name);
		}

		public void ExcuteCommand(string name)
		{
			if (commands.TryGetValue(name, out Command c))
			{
				this.command = "";

				// Execute command.
				c.action.Invoke();
				logScrollPosition.y = 99999f;
			}
		}

		// Can the console be used based on editor/development/release status.
		private bool IsEnabled()
		{
	#if UNITY_EDITOR
			if (showInEditor)
			{
				return true;
			}
	#else
			if (Debug.isDebugBuild && showInDebug)
			{
				return true;
			}
			else if(!Debug.isDebugBuild && showInRelease)
			{
				return true;
			}
	#endif

			return false;
		}

		#region GUI
		private void OnGUI()
		{
			if (!IsEnabled())
			{
				return;
			}

			Event e = Event.current;

			if (e.type == EventType.KeyDown && e.isKey && e.keyCode == toggleKey)
			{
				ToggleConsole();
				e.Use();
			}

			if (isOpen)
			{
				var prevSkin = GUI.skin;
				if (skin != null)
				{
					GUI.skin = skin;
				}

				consoleRect.width = Screen.width;
				consoleRect.height = 0;
				consoleRect = GUILayout.Window(0, consoleRect, ConsoleWindow, consoleWindowName, GUILayout.ExpandHeight(true));

				if (isLogOpen)
				{
					logRect.width = consoleRect.width;
					logRect.height = Screen.height / 3;
					logRect.y = Screen.height - logRect.height;
					logRect = GUILayout.Window(1, logRect, LogWindow, "Log");
				}

				GUI.skin = prevSkin;
			}
		}

		private void ConsoleWindow(int id)
		{
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
		
		private void ConsoleMenu(Event e)
		{
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Log"))
				{
					ToggleLog();
				}
				if (GUILayout.Button("Close"))
				{
					Close();
				}
			}
		}

		private void ConsoleSuggestions(Event e)
		{
			// Get suggestions.
			suggestions.Clear();
			if (!string.IsNullOrWhiteSpace(command))
			{
				foreach (string k in commands.Keys)
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

		private void LogWindow(int id)
		{
			Event e = Event.current;

			LogMenu(e);

			using (new GUILayout.VerticalScope())
			{
				using var scope = new GUILayout.ScrollViewScope(logScrollPosition, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
				logScrollPosition = scope.scrollPosition;

				// Display the log messages.
				foreach (LogMessage entry in log)
				{
					if (entry.logType == LogType.Error || entry.logType == LogType.Exception)
					{
						if (!showErrors)
						{
							continue;
						}

						GUI.color = Color.red;
					}
					else if (entry.logType == LogType.Warning)
					{
						if (!showWarnings)
						{
							continue;
						}

						GUI.color = Color.yellow;
					}
					else
					{
						if (!showLogs)
						{
							continue;
						}
					}

					string msg;
					if (entry.displayStrackTrace)
					{
						msg = string.Format("- {0}\n{1}", entry.message, entry.stackTrace);
					}
					else
					{
						msg = string.Format("+ {0}", entry.message);
					}

					if (GUILayout.Button(msg, skin.label))
					{
						entry.displayStrackTrace = !entry.displayStrackTrace;
					}

					GUI.color = Color.white;
				}
			}
		}

		private void LogMenu(Event e)
		{
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				GUI.color = Color.white;
				showLogs = GUILayout.Toggle(showLogs, "");
				GUI.color = Color.yellow;
				showWarnings = GUILayout.Toggle(showWarnings, "");
				GUI.color = Color.red;
				showErrors = GUILayout.Toggle(showErrors, "");

				GUI.color = Color.white;
				if (GUILayout.Button("Clear"))
				{
					log.Clear();
				}
			}
		}

		#endregion

		#region BaseCommands
		public void ToggleConsole()
		{
			isOpen = !isOpen;

			if (isOpen)
			{
				GUI.FocusWindow(0);
				GUI.FocusControl(inputControlName);
			}
		}

		private void ToggleLog()
		{
			isLogOpen = !isLogOpen;
		}

		private void Clear()
		{
			log.Clear();
		}

		private void Help()
		{
			// TODO: Add help pages.
			string msg = "Help";
			foreach (var c in commands)
			{
				string methodString = "";
				foreach (var p in c.Value.action.Method.GetParameters())
				{
					methodString += string.Format("({0}){1}", p.ParameterType, p.Name);
				}

				msg += string.Format("\n{0}{1}", c.Key, methodString);
			}

			log.Add(new LogMessage(msg, "", LogType.Log));

			isLogOpen = true;
		}

		private void Close()
		{
			isOpen = false;
		}

		private void SysInfo()
		{
			Log("SysInfo\n" +
				"deviceUniqueIdentifier: " + SystemInfo.deviceUniqueIdentifier + "\n" +
				"deviceModel: " + SystemInfo.deviceModel + "\n" +
				"deviceName: " + SystemInfo.deviceName + "\n" +
				"deviceType: " + SystemInfo.deviceType + "\n" +
				"graphicsDeviceType: " + SystemInfo.graphicsDeviceType + "\n" +
				"operatingSystemFamily: " + SystemInfo.operatingSystemFamily + "\n" +
				"supportsComputeShaders: " + SystemInfo.supportsComputeShaders + "\n" +
				"copyTextureSupport: " + SystemInfo.copyTextureSupport,
				LogType.Log
			);
		}
	
		private void GameInfo()
		{
			Log("GameInfo\n" + 
				"productName: " + Application.productName + "\n" +
				"version: " + Application.version + "\n" +
				"unityVersion: " + Application.unityVersion + "\n" +
				"platform: " + Application.platform + "\n" +
				"dataPath: " + Application.dataPath + "\n" +
				"identifier: " + Application.identifier,
				LogType.Log
			);
		}
		#endregion
	}

	public class LogMessage
	{
		public string message;
		public string stackTrace;
		public LogType logType;
		public bool displayStrackTrace;

		public LogMessage(string message, string stackTrace = "", LogType logType = LogType.Log)
		{
			this.message = message;
			this.stackTrace = stackTrace;
			this.logType = logType;
			this.displayStrackTrace = false;
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