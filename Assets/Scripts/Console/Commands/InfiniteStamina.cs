using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/InfiniteStamina")]
    public class InfiniteStamina : ConsoleCommand
    {
        public override string Name => "infinite_stamina";
        public override string Description => "Toggle infinite stamina mode";

        public override void Execute(string[] args)
        {
            DevConsole.Instance.GoodLog($"Infinite stamina mode {(DevConsole.Instance.player.status.ToggleInfiniteStamina() ? "enabled" : "disabled")}");
        }
    }
}