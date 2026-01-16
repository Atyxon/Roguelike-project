using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/Help")]
    public class Help : ConsoleCommand
    {
        public override string Name => "help";
        public override string Description => "Display available commands";
        public override CommandCategory commandCategory => CommandCategory.Console;

        public override void Execute(string[] args)
        {
            DevConsole.Instance.NormalLog("Console commands");
            foreach (var commandPair in DevConsole.Instance.commands)
            {
                var command = commandPair.Value;

                if (command.commandCategory == CommandCategory.Console)
                {
                    DevConsole.Instance.NormalLog(
                        $"   <color=Yellow>{command.Name}</color> - {command.Description}"
                    );
                }
            }
            DevConsole.Instance.NormalLog(string.Empty);
            DevConsole.Instance.NormalLog("Game commands");
            foreach (var commandPair in DevConsole.Instance.commands)
            {
                var command = commandPair.Value;

                if (command.commandCategory == CommandCategory.Game)
                {
                    DevConsole.Instance.NormalLog(
                        $"   <color=Yellow>{command.Name}</color> - {command.Description}"
                    );
                }
            }
        }
    }
}