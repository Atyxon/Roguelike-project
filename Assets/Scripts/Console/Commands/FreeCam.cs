using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/FreeCam")]
    public class FreeCam : ConsoleCommand
    {
        public override string Name => "free_cam";
        public override string Description => "Toggle free cam to fly around";

        public override void Execute(string[] args)
        {
            DevConsole.Instance.player.ToggleFreeCam();
            DevConsole.Instance.GoodLog($"Free camera {(DevConsole.Instance.player.IsFreeCamActive() ? "enabled" : "disabled")}");
        }
    }
}