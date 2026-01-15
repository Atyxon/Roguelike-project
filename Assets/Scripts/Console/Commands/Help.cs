using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/Help")]
    public class Help : ConsoleCommand
    {
        public override string Name => "help";
        public override string Description => "Display available commands";

        public override void Execute(string[] args)
        {
            foreach (var commandPair in DevConsole.Instance.commands)
            {
                var command = commandPair.Value;
                DevConsole.Instance.NormalLog($"<color=Yellow>{command.Name}</color> - {command.Description}");
            }
        }
    }
}