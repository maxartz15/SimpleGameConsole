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

		private bool isOpen = false;
		private int logHeight = 100;
		private string command = "";
		private int suggestionIndex = -1;
		private Vector2 logScrollPosition = Vector2.zero;
		private const string inputControlName = "inputControl";

		// Data.
		private List<LogMessage> log = new List<LogMessage>();
		private Dictionary<string, Action> commands = new Dictionary<string, Action>();
		private List<string> suggestions = new List<string>();

		private void Awake()
		{
			if (IsEnabled())
			{
				// Add base commands.
				AddCommand(Help);
				AddCommand(Clear);
				AddCommand(Close);
				AddCommand(SysInfo);
				AddCommand(GameInfo);

				// Subscribe to Debug.Log.
				Application.logMessageReceived += OnLog;
			}
			else
			{
				enabled = false;
			}
		}

		public void ToggleConsole()
		{
			isOpen = !isOpen;
			logHeight = Screen.height / 3;
			GUI.FocusControl(inputControlName);
		}

		public void Log(LogMessage logEntry)
		{
			log.Add(logEntry);
		}
	
		public void Log(string message, LogType logType)
		{
			Log(new LogMessage(message, logType));
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

		public bool AddCommand(string name, Action action)
		{
			if (!commands.ContainsKey(name))
			{
				commands.Add(name, action);
				return true;
			}

			return false;
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
			if (commands.TryGetValue(name, out Action action))
			{
				command = "";
				// Execute command.
				action.Invoke();
				logScrollPosition.y = 99999f;
				GUI.FocusControl(inputControlName);
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
				using (new GUILayout.VerticalScope(GUILayout.Width(Screen.width)))
				{
					var prevSkin = GUI.skin;
					if (skin != null)
					{
						GUI.skin = skin;
					}

					GUIHeader(e);
					GUILog(e);
					GUICommand(e);

					GUI.skin = prevSkin;
				}
			}
		}

		private void GUIHeader(Event e)
		{
			using (new GUILayout.HorizontalScope(skin.box))
			{
				GUILayout.Label(string.Format("Console - {0} v{1}", Application.productName, Application.version));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Clear"))
				{
					log.Clear();
				}
			}
		}

		private void GUILog(Event e)
		{
			using (new GUILayout.VerticalScope(skin.box))
			{
				using (var scope = new GUILayout.ScrollViewScope(logScrollPosition, false, true, GUILayout.ExpandWidth(true), GUILayout.Height(logHeight)))
				{
					logScrollPosition = scope.scrollPosition;

					// Display the log messages.
					foreach (LogMessage entry in log)
					{
						if (entry.logType == LogType.Error || entry.logType == LogType.Exception)
						{
							GUI.color = Color.red;
						}
						else if (entry.logType == LogType.Warning)
						{
							GUI.color = Color.yellow;
						}

						string msg;
						if (entry.displayStrackTrace)
						{
							msg = string.Format("↓ {0}\n{1}", entry.message, entry.stackTrace);
						}
						else
						{
							msg = string.Format("→ {0}", entry.message);
						}

						if (GUILayout.Button(msg, skin.label))
						{
							entry.displayStrackTrace = !entry.displayStrackTrace;
						}

						GUI.color = Color.white;
					}
				}
			}
		}

		private void GUICommand(Event e)
		{
			// Handle input.
			if (e.type == EventType.KeyDown && e.isKey && e.keyCode == enterKey)
			{
				ExcuteCommand(command);
				e.Use();
			}

			if (suggestions.Count != 0 && suggestionIndex != -1)
			{
				if (e.type == EventType.KeyDown && e.isKey && e.keyCode == suggestionCompleteKey)
				{
					command = suggestions[suggestionIndex];
					e.Use();
				}

				if (e.type == EventType.KeyDown && e.isKey && e.keyCode == suggestionUpKey)
				{
					suggestionIndex = Mathf.Clamp(suggestionIndex--, 0, suggestions.Count - 1);
					e.Use();
				}

				if (e.type == EventType.KeyDown && e.isKey && e.keyCode == suggestionDownKey)
				{
					suggestionIndex = Mathf.Clamp(suggestionIndex++, 0, suggestions.Count - 1);
					e.Use();
				}
			}

			// Input field.
			using (new GUILayout.HorizontalScope(skin.box))
			{
				GUILayout.Label("Command", GUILayout.ExpandWidth(false));
				GUI.SetNextControlName(inputControlName);
				command = GUILayout.TextField(command, GUILayout.ExpandWidth(true));
			}

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
			}

			// Draw suggestions.
			if (suggestions.Count != 0)
			{
				// Select first if we didn't have a suggestion already.
				if (suggestionIndex == -1)
				{
					suggestionIndex = 0;
				}

				// Draw suggestions.
				using (new GUILayout.VerticalScope(skin.box))
				{
					for (int i = 0; i < suggestions.Count; i++)
					{
						GUI.color = Color.gray;
						if (i == suggestionIndex)
						{
							GUI.color = Color.white;
							GUILayout.Label(string.Format("→ {0}", suggestions[i]));
						}
						else
						{
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
		#endregion

		#region BaseCommands
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
				foreach (var p in c.Value.Method.GetParameters())
				{
					methodString += string.Format("({0}){1}", p.ParameterType, p.Name);
				}

				msg += string.Format("\n{0}{1}", c.Key, methodString);
			}

			log.Add(new LogMessage(msg, LogType.Log));
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
		public string message = "";
		public string stackTrace = "";
		public LogType logType = LogType.Log;
		public bool displayStrackTrace = false;

		public LogMessage(string message, string stackTrace, LogType logType)
		{
			this.message = message;
			this.stackTrace = stackTrace;
			this.logType = logType;
		}

		public LogMessage(string message, LogType logType)
		{
			this.message = message;
			this.logType = logType;
		}

		public LogMessage(string message)
		{
			this.message = message;
		}
	}
}