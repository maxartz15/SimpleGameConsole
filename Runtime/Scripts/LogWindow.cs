using System.Collections.Generic;
using UnityEngine;

namespace TAO.Console
{
	[RequireComponent(typeof(ConsoleWindow))]
	public class LogWindow : Window
	{
		[Space]

		[SerializeField]
		private bool displayLogs = true;
		[SerializeField]
		private bool displayWarnings = true;
		[SerializeField]
		private bool displayErrors = true;

		[Space]

		[SerializeField]
		private bool autoScroll = true;
		[SerializeField]
		private int maxLogEntries = 10000;

		private readonly Queue<LogMessage> log = new Queue<LogMessage>();
		private LogMessage selectedLog = null;
		private Vector2 logScrollPosition = Vector2.zero;
		private Vector2 selectedLogScrollPosition = Vector2.zero;
		private ConsoleWindow consoleWindow = null;

		protected override void Awake()
		{
			base.Awake();

			if (IsEnabled())
			{
				windowName = "Log";

				if (consoleWindow == null)
				{
					consoleWindow = GetComponent<ConsoleWindow>();
				}

				// Add commands.
				consoleWindow.AddCommand(Help);
				consoleWindow.AddCommand("Log.Toggle", Toggle);
				consoleWindow.AddCommand("Log.Clear", Clear);
				consoleWindow.AddCommand("Log.SysInfo", SysInfo);
				consoleWindow.AddCommand("Log.GameInfo", GameInfo);

				// Subscribe to Debug.Log.
				Application.logMessageReceived += OnLog;
			}
			else
			{
				enabled = false;
			}
		}

		protected override void OnGUI()
		{
			base.OnGUI();
		}

		protected override void OnWindow(int id)
		{
			base.OnWindow(id);

			Event e = Event.current;

			LogMenu(e);

			// Log.
			using (new GUILayout.VerticalScope())
			{
				using var scope = new GUILayout.ScrollViewScope(logScrollPosition, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
				logScrollPosition = scope.scrollPosition;

				// Display the log messages.
				foreach (LogMessage entry in log)
				{
					if (entry.logType == LogType.Error || entry.logType == LogType.Exception)
					{
						if (!displayErrors)
						{
							continue;
						}

						GUI.color = Color.red;
					}
					else if (entry.logType == LogType.Warning)
					{
						if (!displayWarnings)
						{
							continue;
						}

						GUI.color = Color.yellow;
					}
					else
					{
						if (!displayLogs)
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

						if (e.button == 1)
						{
							selectedLog = entry;
						}
					}

					GUI.color = Color.white;
				}
			}

			// Selection.
			if (selectedLog != null)
			{
				GUILayout.Box("", GUILayout.Height(2));

				using (new GUILayout.VerticalScope(GUILayout.Height(rect.height * 0.5f)))
				{
					GUILayout.Box("", GUILayout.Height(2));

					using (new GUILayout.HorizontalScope())
					{
						GUILayout.FlexibleSpace();

						if (GUILayout.Button("Close"))
						{
							selectedLog = null;
						}
					}

					if (selectedLog != null)
					{
						using var scope = new GUILayout.ScrollViewScope(selectedLogScrollPosition, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
						selectedLogScrollPosition = scope.scrollPosition;

						string msg = string.Format("{0}\n{1}", selectedLog.message, selectedLog.stackTrace);
						GUILayout.Label(msg, skin.label);
					}
				}
			}
		}

		protected override void UpdateWindowRect()
		{
			rect.width = Screen.width;
			rect.height = Screen.height / 3;
			rect.y = Screen.height - rect.height;
		}

		private void LogMenu(Event e)
		{
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				GUI.color = Color.white;
				displayLogs = GUILayout.Toggle(displayLogs, "");
				GUI.color = Color.yellow;
				displayWarnings = GUILayout.Toggle(displayWarnings, "");
				GUI.color = Color.red;
				displayErrors = GUILayout.Toggle(displayErrors, "");

				GUI.color = Color.white;

				autoScroll = GUILayout.Toggle(autoScroll, "AutoScroll");

				if (GUILayout.Button("Clear"))
				{
					log.Clear();
				}
				if (GUILayout.Button("Close"))
				{
					isOpen = false;
				}
			}
		}

		public void Log(LogMessage logEntry)
		{
			while (log.Count > maxLogEntries)
			{
				log.Dequeue();
			}

			log.Enqueue(logEntry);
			if (autoScroll)
			{
				logScrollPosition.y = float.MaxValue;
			}
		}

		public void Log(string message, LogType logType)
		{
			Log(new LogMessage(message, "", logType));
		}

		private void OnLog(string message, string stackTrace, LogType type)
		{
			Log(new LogMessage(message, stackTrace, type));
		}

		#region Commands
		private void Help()
		{
			// TODO: Add help pages.
			string msg = "Help";
			foreach (var c in consoleWindow.Commands)
			{
				string methodString = "";
				foreach (var p in c.Value.action.Method.GetParameters())
				{
					methodString += string.Format("({0}){1}", p.ParameterType, p.Name);
				}

				msg += string.Format("\n{0}{1}", c.Key, methodString);
			}

			Log(new LogMessage(msg, "", LogType.Log));

			isOpen = true;
		}

		private void Clear()
		{
			log.Clear();
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
}