using UnityEngine;

namespace TAO.Console
{
	[RequireComponent(typeof(GameConsole))]
	public class ConsoleSampleCommands : MonoBehaviour
	{
	    private GameConsole console = null;
	
	    private void Start()
	    {
	        console = GetComponent<GameConsole>();
	        console.AddCommand(Application.Quit);
	    }
	}
}