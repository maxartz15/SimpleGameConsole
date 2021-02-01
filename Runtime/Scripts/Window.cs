using UnityEngine;

namespace TAO.Console
{
    public class Window : MonoBehaviour
    {
		[SerializeField]
		protected GUISkin skin = null;

#pragma warning disable CS0414
		[SerializeField]
		protected bool showInEditor = false;
		[SerializeField]
		protected bool showInDebug = true;
		[SerializeField]
		protected bool showInRelease = true;
#pragma warning restore CS0414

		protected Rect rect = new Rect(0, 0, 0, 0);
		public bool isOpen = false;
		protected string windowName = "";
		protected int windowId = 0;

		protected bool windowExpandWidth = false;
		protected bool windowExpandHeight = false;

		protected virtual void Awake()
		{
			windowId = Random.Range(int.MinValue, int.MaxValue);
		}

		protected virtual void OnGUI()
		{
			if (IsEnabled() && isOpen)
			{
				var prevSkin = GUI.skin;
				if (skin != null)
				{
					GUI.skin = skin;
				}

				Event e = Event.current;
				if (e.type == EventType.Layout)
				{
					UpdateWindowRect();
				}

				rect = GUILayout.Window(windowId, rect, OnWindow, windowName, GUILayout.ExpandWidth(windowExpandWidth), GUILayout.ExpandHeight(windowExpandHeight));

				GUI.skin = prevSkin;
			}
		}

		protected virtual void OnWindow(int id)
		{

		}

		protected virtual void UpdateWindowRect()
		{

		}

		// Can the console be used based on editor/development/release status.
		public bool IsEnabled()
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

		public void Toggle()
		{
			isOpen = !isOpen;
		}
	}
}