using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/ShowFps")]
    public class ShowFps : ConsoleCommand
    {
        public override string Name => "show_fps";
        public override string Description => "Toggle FPS counter";

        public override void Execute(string[] args)
        {
            DevConsole.Instance.ToggleFPS();
            DevConsole.Instance.GoodLog($"FPS counter {(DevConsole.Instance.GetFpsActive() ? "enabled" : "disabled")}");
        }
    }
}