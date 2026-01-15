using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Console
{
    public class DevConsole : MonoBehaviour
    {
        public static DevConsole Instance { get; private set; }
        public Dictionary<string, IConsoleCommand> commands = new();

        public PlayerController player;
        
        [Header("Dev Console UI")]
        public GameObject consolePanel;
        public TMP_InputField inputField;
        public TextMeshProUGUI consoleLog;

        private string _consoleLogBuffer;
        private bool _isActive;
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RegisterAllCommands();
            consolePanel.SetActive(false);
            player = FindFirstObjectByType<PlayerController>();
            
            GoodLog("Dev console 1.0");
            GoodLog("Type <color=Yellow>help</color> to see commands");
        }

        public void Execute(string input)
        {
            var args = input.Split(' ');
            var commandName = args[0].ToLower();
            UserLog(input);
            
            if (commands.TryGetValue(commandName, out var command))
            {
                command.Execute(args);
            }
            else
            {
                WarningLog($"Unknown command: {commandName}");
            }
        }

        public void RegisterCommand(IConsoleCommand command)
        {
            commands[command.Name.ToLower()] = command;
        }

        private void RegisterAllCommands()
        {
            var loadedCommands = Resources.LoadAll<ConsoleCommand>("ConsoleCommands");

            foreach (var command in loadedCommands)
            {
                RegisterCommand(command);
            }
        }
        private void Log(string message)
        {
            _consoleLogBuffer += $"\n{message}";
            consoleLog.text = _consoleLogBuffer;
        }
        public bool IsConsoleActive() => _isActive;
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F1) || consolePanel == null) return;

            _isActive = !consolePanel.activeSelf;
            consolePanel.SetActive(!consolePanel.activeSelf);
            StartCoroutine(FocusInputNextFrame());
        }
        public void OnSubmitCommand()
        {
            if (string.IsNullOrWhiteSpace(inputField.text))
            {
                UserLog("");
                StartCoroutine(FocusInputNextFrame());
                return;
            }

            Execute(inputField.text);

            inputField.text = string.Empty;
            StartCoroutine(FocusInputNextFrame());
        }

        public void NormalLog(string str) { Log($"   {str}</color>"); }
        public void UserLog(string str) { Log($"> {str}</color>"); }
        public void GoodLog(string str) { Log($"<color=#00FF00>   {str}</color>"); }
        public void WarningLog(string str) { Log($"<color=#FFCC00>   {str}</color>"); }
        public void ErrorLog(string str) { Log($"<color=#CC3300>   {str}</color>"); }

        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            EventSystem.current.SetSelectedGameObject(null);
            inputField.Select();
        }
    }
}
