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
            var scenesToReload = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(i => SceneManager.GetSceneAt(i))
                .Where(s => s.isLoaded && s.name != "DevConsoleScene")
                .ToList();

            foreach (var scene in scenesToReload)
            {
                SceneManager.UnloadSceneAsync(scene);
            }

            foreach (var scene in scenesToReload)
            {
                SceneManager.LoadScene(scene.name, LoadSceneMode.Additive);
            }

            if (scenesToReload.Count > 0)
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(scenesToReload[0].name));
            }
        }
    }
}