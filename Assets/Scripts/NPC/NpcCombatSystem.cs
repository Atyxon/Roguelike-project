using System;
using UnityEngine;
using UnityEngine.AI;

public class NpcCombatSystem : MonoBehaviour
{
    public WeaponCollider currentWeapon;
    private NavMeshAgent _agent;

    private void Start()
    {
        currentWeapon.gameObject.SetActive(false);
        _agent = GetComponent<NavMeshAgent>();
    }

    public void HitPlayer(GameObject player)
    {
        player.SendMessage("ApplyHealth", -15);
    }

    public void EnableCollider()
    {
        currentWeapon.gameObject.SetActive(true);
    }
    
    public void DisableCollider()
    {
        currentWeapon.gameObject.SetActive(false);
    }
    
    public void StartAttack()
    {
        _agent.enabled = false;
        print("start");
    }

    public void EndAttack()
    {
        _agent.enabled = true;
        print("end");
    }
}
