using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/Quit")]
    public class Quit : ConsoleCommand
    {
        public override string Name => "quit";
        public override string Description => "Quit the game";
        public override CommandCategory commandCategory => CommandCategory.Game;

        public override void Execute(string[] args)
        {
            DevConsole.Instance.ErrorLog("Quitting game...");
            Application.Quit();
        }
    }
}