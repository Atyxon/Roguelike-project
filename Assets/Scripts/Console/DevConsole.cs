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
        private const string ConsoleVer = "1.1";

        public PlayerController player;
        
        [Header("Dev Console UI")]
        public GameObject consolePanel;
        public TMP_InputField inputField;
        public TextMeshProUGUI consoleLog;
        public TextMeshProUGUI hintText;

        [Header("Stats UI")]
        public TextMeshProUGUI statsText;
        public StatsManager statsManager;

        private bool _showFps = false;
        private bool _showStats = false;
        
        private string _consoleLogBuffer = string.Empty;
        private bool _isActive;
        private string _currentHint;
        private readonly List<string> _commandHistory = new();
        private int _historyIndex = -1;

        [Header("Lists")]
        public GameObject[] entitiesList;
        
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
            hintText.gameObject.SetActive(false);

            player = FindFirstObjectByType<PlayerController>();

            inputField.onValueChanged.AddListener(OnInputChanged);
            WelcomeLog();
        }

        private void WelcomeLog()
        {
            GoodLog($"--------------------------------------------------");
            GoodLog($"");
            GoodLog($"               Welcome to Console {ConsoleVer}");
            GoodLog($"                Type 'help' to start");
            GoodLog($"");
            GoodLog($"--------------------------------------------------");
        }

        private void Update()
        {
            // Stats display
            if (_showFps || _showStats)
            {
                var statsTextValue = (_showFps ? $"{statsManager.GetFps()}\n" : string.Empty) +
                                     (_showStats ? statsManager.GetStats() : string.Empty);
                statsText.text = statsTextValue;
            }
            else
                statsText.text = string.Empty;

                // Toggle console
            if (Input.GetKeyDown(KeyCode.F1) && consolePanel != null)
            {
                _isActive = !consolePanel.activeSelf;
                consolePanel.SetActive(_isActive);

                ClearHint();
                StartCoroutine(FocusInputNextFrame());
                return;
            }

            if (!_isActive || !inputField.isFocused)
                return;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                ShowPreviousCommand();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ShowNextCommand();
            }
            
            // TAB autocomplete
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (!string.IsNullOrEmpty(_currentHint))
                {
                    inputField.text = _currentHint;
                    //inputField.ActivateInputField();
                    inputField.caretPosition = inputField.text.Length;
                    ClearHint();
                }
            }
        }

        // ------------------------
        // Command execution
        // ------------------------
        public void OnSubmitCommand()
        {
            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
                return;

            ClearHint();

            if (string.IsNullOrWhiteSpace(inputField.text))
            {
                UserLog("");
                StartCoroutine(FocusInputNextFrame());
                return;
            }

            Execute(inputField.text);

            var submittedCommand = inputField.text;
            if (_commandHistory.Count == 0 || _commandHistory[^1] != submittedCommand)
            {
                _commandHistory.Add(submittedCommand);
            }
            _historyIndex = _commandHistory.Count;
            
            inputField.text = string.Empty;
            StartCoroutine(FocusInputNextFrame());
        }

        public void Execute(string input)
        {
            var args = input.Split(' ');
            var commandName = args[0].ToLower();

            UserLog(input);

            if (commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    command.Execute(args);
                }
                catch (System.Exception ex)
                {
                    ErrorLog($"Command '{command.Name}' failed: {ex.Message}");
                }
            }
            else
            {
                WarningLog($"Unknown command: {commandName}");
            }
        }

        // ------------------------
        // Command registration
        // ------------------------
        public void RegisterCommand(IConsoleCommand command)
        {
            commands[command.Name.ToLower()] = command;
        }

        private void RegisterAllCommands()
        {
            var loadedCommands = Resources.LoadAll<ConsoleCommand>("ConsoleCommands");

            foreach (var command in loadedCommands)
                RegisterCommand(command);
        }

        public void ResetConsoleBuffer()
        {
            _consoleLogBuffer = string.Empty;
            consoleLog.text = string.Empty;
        }

        // ------------------------
        // Hinting / autocomplete
        // ------------------------
        private void OnInputChanged(string input)
        {
            if (!_isActive)
                return;

            _currentHint = FindCommandHint(input);

            if (!string.IsNullOrEmpty(_currentHint) && _currentHint != input)
            {
                hintText.text = _currentHint;
                hintText.gameObject.SetActive(true);
            }
            else
            {
                ClearHint();
            }
        }

        private string FindCommandHint(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            input = input.ToLower();

            foreach (var cmd in commands.Values)
            {
                if (cmd.Name.StartsWith(input))
                    return cmd.Name;
            }

            return null;
        }

        private void ClearHint()
        {
            _currentHint = null;
            hintText.text = string.Empty;
            hintText.gameObject.SetActive(false);
        }

        private void ShowPreviousCommand()
        {
            if (_commandHistory.Count == 0)
                return;

            _historyIndex--;
            if (_historyIndex < 0)
                _historyIndex = 0;

            SetInputFromHistory();
        }

        private void ShowNextCommand()
        {
            if (_commandHistory.Count == 0)
                return;

            _historyIndex++;
            if (_historyIndex >= _commandHistory.Count)
            {
                _historyIndex = _commandHistory.Count;
                inputField.text = string.Empty;
                return;
            }

            SetInputFromHistory();
        }

        private void SetInputFromHistory()
        {
            inputField.text = _commandHistory[_historyIndex];
            inputField.caretPosition = inputField.text.Length;
            ClearHint();
        }
        
        // ------------------------
        // Logging
        // ------------------------
        private void Log(string message)
        {
            _consoleLogBuffer += $"\n{message}";
            consoleLog.text = _consoleLogBuffer;
        }

        public bool IsConsoleActive() => _isActive;
        public bool GetFpsActive() => _showFps;
        public bool GetStatsActive() => _showStats;

        public void NormalLog(string str)  => Log($"  {str}");
        public void UserLog(string str)    => Log($"> {str}");
        public void GoodLog(string str)    => Log($"<color=#54eb54>  {str}</color>");
        public void WarningLog(string str) => Log($"<color=#FFCC00>  {str}</color>");
        public void ErrorLog(string str)   => Log($"<color=#CC3300>  {str}</color>");

        public void ToggleFPS() { _showFps = !_showFps; }
        public void ToggleStats() { _showStats = !_showStats; }

        // ------------------------
        // Utilities
        // ------------------------
        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            EventSystem.current.SetSelectedGameObject(null);
            inputField.Select();
            inputField.ActivateInputField();
        }
    }
}
