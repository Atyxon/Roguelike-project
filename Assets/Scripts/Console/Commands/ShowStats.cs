using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/ShowStats")]
    public class ShowStats : ConsoleCommand
    {
        public override string Name => "show_stats";
        public override string Description => "Toggle stats overview on HUD";

        public override void Execute(string[] args)
        {
            DevConsole.Instance.ToggleStats();
            DevConsole.Instance.GoodLog($"Stats overview {(DevConsole.Instance.GetStatsActive() ? "enabled" : "disabled")}");
        }
    }
}