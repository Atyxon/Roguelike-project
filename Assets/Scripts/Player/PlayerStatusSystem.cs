using System;
using Player;
using UnityEngine;

public class PlayerStatusSystem : MonoBehaviour
{
    public UiController uiController;
    public Renderer rend;
    public float bloodMaxLevel = .5f;
    
    public float healthCurrent;
    public float healthMax;
    public float staminaCurrent;
    public float staminaMax;
    public float staminaRegenerationRate;

    private float _staminaRegenerationTimer;
    private float _staminaRegenerationThreshold = 3f;
    private bool _godMode;
    private bool _infiniteStamina;
    private void Start()
    {
        healthCurrent = healthMax;
        staminaCurrent = staminaMax;
        
        var original = rend.sharedMaterial;
        var instance = Instantiate(original);
        rend.material = instance;
    }

    private void Update()
    {
        if (_staminaRegenerationTimer < _staminaRegenerationThreshold)
        {
            _staminaRegenerationTimer += Time.deltaTime;
        }
        else
        {
            if (staminaCurrent < staminaMax)
            {
                ApplyStamina(staminaRegenerationRate * Time.deltaTime);
            }
        }
    }

    public void ApplyHealth(float h)
    {
        if(_godMode == true && h < 0)
            return;
        
        healthCurrent = Mathf.Clamp(healthCurrent + h, 0f, healthMax);
        if (healthCurrent == 0)
            Kill();
        
        uiController.UpdateHealthBar(healthCurrent, healthMax);
        rend.material.SetFloat("_Visibility", (1-(healthCurrent/healthMax)) * bloodMaxLevel);
    }

    public void ApplyStamina(float s)
    {
        if (s < 0)
        {
            _staminaRegenerationTimer = 0;
            if(_infiniteStamina == true)
                return;
        }
        
        staminaCurrent = Mathf.Clamp(staminaCurrent + s, 0f, staminaMax);
        
        uiController.UpdateStaminaBar(staminaCurrent, staminaMax);
    }

    public void Kill()
    {
        print("player killed");
    }
    public bool ToggleGodMode() => _godMode = !_godMode;
    public bool ToggleInfiniteStamina() => _infiniteStamina = !_infiniteStamina;
}
