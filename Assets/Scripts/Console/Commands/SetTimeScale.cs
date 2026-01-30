using System.Globalization;
using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/SetTimeScale")]
    public class SetTimeScale : ConsoleCommand
    {
        public override string Name => "set_time_scale";
        public override string Usage => "set_time_scale <scale>";
        public override string Description => "Allows user to slow down time, or accelerate if needed; Default is time scale is 1";

        public override void Execute(string[] args)
        {
            if (args.Length < 2) {
                DevConsole.Instance.ErrorLog("Not enough arguments!");
                DevConsole.Instance.ErrorLog($"Command usage: {Usage}");
                return;
            }
            if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float timeScale))
            {
                DevConsole.Instance.ErrorLog($"Invalid time scale ({args[1]} is not a number)");
                return;
            }

            Time.timeScale = timeScale;
            DevConsole.Instance.GoodLog($"Set time scale to {timeScale}");
        }
    }
}