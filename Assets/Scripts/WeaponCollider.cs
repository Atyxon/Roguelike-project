using UnityEngine;

public class WeaponCollider : MonoBehaviour
{
    public NpcCombatSystem cs;
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            cs.HitPlayer(other.gameObject);
        }
    }
}
