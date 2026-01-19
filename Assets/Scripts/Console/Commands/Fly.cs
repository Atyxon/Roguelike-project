using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/Fly")]
    public class Fly : ConsoleCommand
    {
        public override string Name => "fly";
        public override string Description => "Allows player to fly";

        public override void Execute(string[] args)
        {
            DevConsole.Instance.GoodLog($"Fly mode {(DevConsole.Instance.player.ToggleFlyMode() ? "enabled" : "disabled")}");
        }
    }
}