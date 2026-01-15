using UnityEngine;

namespace Console
{
    public abstract class ConsoleCommand : ScriptableObject, IConsoleCommand
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract void Execute(string[] args);
    }
}