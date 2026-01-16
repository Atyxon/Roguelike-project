using UnityEngine;

namespace Console
{
    public class StatsManager : MonoBehaviour
    {
        private float _timer;
        private int _frames;
        private float _fps;

        public string GetFps()
        {
            string color;
            if (_fps < 30)
            {
                color = "<color=#CC3300>";
            }
            else if (_fps < 45)
            {
                color = "<color=#FFCC00>";
            }
            else
            {
                color = string.Empty;
            }

            return $"{color}FPS {(int)_fps}</color>";
        }

        void Update()
        {
            _frames++;
            _timer += Time.unscaledDeltaTime;

            if (_timer >= 0.5f)
            {
                _fps = _frames / _timer;
                _frames = 0;
                _timer = 0f;
            }
        }
        public string GetStats()
        {
            var playerPos = DevConsole.Instance.player.gameObject.transform.position;
            return $"Player Position: [X:{playerPos.x:F1}, Y:{playerPos.y:F1}, Z:{playerPos.z:F1}]";
        }
    }
}