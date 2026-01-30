namespace Console
{
    public interface IConsoleCommand
    {
        string Name { get; }
        string Usage { get; }
        string Description { get; }
        ConsoleCommand.CommandCategory commandCategory { get; set; }
        void Execute(string[] args);
    }
}