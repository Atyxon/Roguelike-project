using UnityEngine;

namespace Console
{
    public abstract class ConsoleCommand : ScriptableObject, IConsoleCommand
    {
        public enum CommandCategory
        {
            Game,
            Console
        }
        public virtual CommandCategory commandCategory
        {
            get => CommandCategory.Game;
            set => throw new System.NotImplementedException();
        }
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract void Execute(string[] args);
    }
}