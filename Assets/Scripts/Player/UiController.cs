using System;
using UnityEngine;

namespace Player
{
    public class UiController : MonoBehaviour
    {
        [Header("Stamina")]
        public RectTransform hpBarMain;
        public RectTransform hpBarChase;
        private float _originalHpBarWidth;
        
        [Header("Stamina")]
        public RectTransform staminaBarMain;
        public RectTransform staminaBarChase;
        private float _originalStaminaBarWidth;
        void Start()
        {
            _originalHpBarWidth = hpBarMain.sizeDelta.x;
            _originalStaminaBarWidth = staminaBarMain.sizeDelta.x;
        }
        private void Update()
        {
            hpBarChase.sizeDelta = new Vector2(
                Mathf.Lerp(
                    hpBarChase.sizeDelta.x,
                    hpBarMain.sizeDelta.x,
                    2 * Time.deltaTime
                ),
                hpBarChase.sizeDelta.y
            );
            
            staminaBarChase.sizeDelta = new Vector2(
                Mathf.Lerp(
                    staminaBarChase.sizeDelta.x,
                    staminaBarMain.sizeDelta.x,
                    2 * Time.deltaTime
                ),
                staminaBarChase.sizeDelta.y
            );
        }
        public void UpdateHealthBar(float current, float max)
        {
            var ratio = Mathf.Clamp01(current / max);
            var newWidth = _originalHpBarWidth * ratio;

            hpBarMain.sizeDelta = new Vector2(newWidth, hpBarMain.sizeDelta.y);
        }
        
        public void UpdateStaminaBar(float current, float max)
        {
            var ratio = Mathf.Clamp01(current / max);
            var newWidth = _originalStaminaBarWidth * ratio;

            staminaBarMain.sizeDelta = new Vector2(newWidth, staminaBarMain.sizeDelta.y);
        }
    }
}
