using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/ApplyStamina")]
    public class ApplyStamina : ConsoleCommand
    {
        public override string Name => "apply_stamina";
        public override string Usage => "apply_stamina <amount>";
        public override string Description => "Apply given value to player's stamina points";

        public override void Execute(string[] args)
        {
            if (!int.TryParse(args[1], out var value)) {
                DevConsole.Instance.ErrorLog($"Invalid input value ({args[1]} is not a number)");
                return;
            }

            DevConsole.Instance.player.status.ApplyStamina(value);
            DevConsole.Instance.GoodLog($"Applied {value} to player's stamina points");
        }
    }
}