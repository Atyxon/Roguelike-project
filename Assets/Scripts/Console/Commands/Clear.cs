using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/Clear")]
    public class Clear : ConsoleCommand
    {
        public override string Name => "clear";
        public override string Description => "Clears dev console log buffer";
        public override CommandCategory commandCategory => CommandCategory.Console;

        public override void Execute(string[] args)
        {
            DevConsole.Instance.ResetConsoleBuffer();
            DevConsole.Instance.GoodLog("Console cleared");
        }
    }
}