using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/PrintEntities")]
    public class PrintEntities : ConsoleCommand
    {
        public override string Name => "print_entities";
        public override string Description => "Print all entities available to spawn";
        public override CommandCategory commandCategory => CommandCategory.Console;

        public override void Execute(string[] args)
        {
            for (int i = 0; i < DevConsole.Instance.entitiesList.Length; i++)
            {
                DevConsole.Instance.NormalLog($"   ID: <color=Yellow>{i}</color> - {DevConsole.Instance.entitiesList[i].name}");  
            }
        }
    }
}