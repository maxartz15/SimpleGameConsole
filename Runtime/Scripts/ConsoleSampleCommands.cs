using UnityEngine;

namespace TAO.Console
{
	[RequireComponent(typeof(ConsoleWindow))]
	public class ConsoleSampleCommands : MonoBehaviour
	{
	    private ConsoleWindow console = null;
	
	    private void Start()
	    {
	        console = GetComponent<ConsoleWindow>();
	        console.AddCommand(Application.Quit);

			//InvokeRepeating("Log", 1, 1);
	    }

		private void Update()
		{
			if (Input.GetKey(KeyCode.Space))
			{
				Log();
			}
		}

		private void Log()
		{
			Debug.Log("Log");
			Debug.LogWarning("Log");
			Debug.LogError("Log");
		}
	}
}