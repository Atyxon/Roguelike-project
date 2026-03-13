using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/ApplyHealth")]
    public class ApplyHealth : ConsoleCommand
    {
        public override string Name => "apply_health";
        public override string Usage => "apply_health <amount>";
        public override string Description => "Apply given value to player's health points";

        public override void Execute(string[] args)
        {
            if (!int.TryParse(args[1], out var value)) {
                DevConsole.Instance.ErrorLog($"Invalid input value ({args[1]} is not a number)");
                return;
            }

            DevConsole.Instance.player.status.ApplyHealth(value);
            DevConsole.Instance.GoodLog($"Applied {value} to player's health points");
        }
    }
}