using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/Kill")]
    public class Kill : ConsoleCommand
    {
        public override string Name => "kill";
        public override string Description => "Kills player";

        public override void Execute(string[] args)
        {
            DevConsole.Instance.player.status.Kill();
            DevConsole.Instance.GoodLog($"Killed player");
        }
    }
}