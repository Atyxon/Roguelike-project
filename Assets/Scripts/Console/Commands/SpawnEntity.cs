using UnityEngine;

namespace Console.Commands
{
    [CreateAssetMenu(menuName = "DevConsole/Commands/SpawnEntity")]
    public class SpawnEntity : ConsoleCommand
    {
        public override string Name => "spawn_entity";
        public override string Usage => "spawn_entity <entity_id>";
        public override string Description => "Spawn entity in front of player";

        public override void Execute(string[] args)
        {
            if (DevConsole.Instance.player == null) {
                DevConsole.Instance.ErrorLog("Player is null");
                return;
            }
            if (args.Length < 2) {
                DevConsole.Instance.ErrorLog("Not enough arguments!");
                DevConsole.Instance.ErrorLog("Command usage: spawn_entity <entity_id>");
                return;
            }
            if (!int.TryParse(args[1], out var index)) {
                DevConsole.Instance.ErrorLog($"Invalid entity index ({args[1]} is not a number)");
                return;
            }
            if (index < 0 || index >= DevConsole.Instance.entitiesList.Length) {
                DevConsole.Instance.ErrorLog("Entity ID out of range");
                return;
            }
            
            var playerTransform = DevConsole.Instance.player.transform;
            var entityPrefab = DevConsole.Instance.entitiesList[index];
            var spawnPosition = playerTransform.position + playerTransform.forward * 2f;
            
            Instantiate(entityPrefab, spawnPosition, Quaternion.identity);
            DevConsole.Instance.GoodLog($"Spawned {entityPrefab.name}");
        }
    }
}