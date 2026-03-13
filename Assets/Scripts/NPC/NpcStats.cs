using UnityEngine;

public class NpcStats : MonoBehaviour
{
    public float MaxHP = 100f;
    public float CurrentHP;

    void Awake()
    {
        CurrentHP = MaxHP;
    }

    public void TakeDamage(float amount)
    {
        CurrentHP -= amount;
        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}