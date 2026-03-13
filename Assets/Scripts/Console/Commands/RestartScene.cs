using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/RestartScene")]
    public class RestartScene : ConsoleCommand
    {
        public override string Name => "reload_scene";
        public override string Description => "Reload current scene";
        public override CommandCategory commandCategory => CommandCategory.Game;

        public override void Execute(string[] args)
        {
            var currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}