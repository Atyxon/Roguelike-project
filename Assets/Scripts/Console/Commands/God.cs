using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/God")]
    public class God : ConsoleCommand
    {
        public override string Name => "god";
        public override string Description => "Toggle unkillable mode (except for kill command)";

        public override void Execute(string[] args)
        {
            DevConsole.Instance.GoodLog($"God mode {(DevConsole.Instance.player.status.ToggleGodMode() ? "enabled" : "disabled")}");
        }
    }
}