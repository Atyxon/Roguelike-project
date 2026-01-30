using System;
using System.Collections;
using Console;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class NpcActions : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;

    [Header("Combat")]
    public float attackRange = 2.2f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 10;

    [Header("Movement")]
    public float keepDistanceRange = 6f;
    public float flankRadius = 4f;

    [Header("Cover")]
    public LayerMask coverLayer;

    private float lastAttackTime;
    private bool isBusy;

    private NpcStats stats;

    void Awake()
    {
        stats = GetComponent<NpcStats>();
        player = DevConsole.Instance.player.transform;
    }

    private void Start()
    {
        agent.updatePosition = true;
        agent.updateRotation = true;
        animator.applyRootMotion = false;
    }

    void Update()
    {
        float speedPercent = agent.velocity.magnitude / agent.speed;
        animator.SetFloat(
            AnimDefines.PARAMETER_SPEED_PERCENT,
            speedPercent,
            0.15f,
            Time.deltaTime
        );
        if (Input.GetKeyDown(KeyCode.Alpha1)) Attack();
        if (Input.GetKeyDown(KeyCode.Alpha2)) Flank();
        if (Input.GetKeyDown(KeyCode.Alpha3)) KeepDistance();
        if (Input.GetKeyDown(KeyCode.Alpha4)) Hide();
    }
    void LateUpdate()
    {
        if (!agent.updatePosition)
            agent.nextPosition = transform.position;
    }

    public void Attack()
    {
        if (isBusy) return;
        if (Time.time < lastAttackTime + attackCooldown) return;
        
        StartCoroutine(AttackRoutine());
    }

    public void Flank()
    {
        if (isBusy) return;

        Vector3 flankPos = GetFlankPosition();
        MoveTo(flankPos);
    }

    public void KeepDistance()
    {
        if (isBusy) return;

        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 targetPos = player.position + dir * keepDistanceRange;

        MoveTo(targetPos);
    }

    public void Hide()
    {
        if (isBusy) return;

        Transform cover = FindNearestCover();
        if (cover == null) return;

        Vector3 hidePos = GetCoverPosition(cover);
        MoveTo(hidePos);
    }
    private Vector3 GetCoverPosition(Transform cover)
    {
        // Direction from player to cover
        Vector3 dirFromPlayer = (cover.position - player.position).normalized;

        // Try to get collider size
        Collider col = cover.GetComponent<Collider>();

        float offset = 1.0f; // fallback offset

        if (col != null)
        {
            // approximate distance from center to surface
            offset = col.bounds.extents.magnitude * 0.5f;
        }

        Vector3 targetPos = cover.position + dirFromPlayer * offset;

        // Snap to NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    private void MoveTo(Vector3 destination)
    {
        Vector3 randomOffset =
            Random.insideUnitSphere * 0.5f;

        randomOffset.y = 0;

        agent.isStopped = false;
        agent.SetDestination(destination + randomOffset);
    }

    private IEnumerator AttackRoutine()
    {
        isBusy = true;

        agent.stoppingDistance = attackRange;
        agent.isStopped = false;

        float chaseDuration = 30f;
        float chaseStartTime = Time.time;

        // Chase player for up to 30 seconds
        while (Time.time < chaseStartTime + chaseDuration)
        {
            // Continuously update destination
            agent.SetDestination(player.position);

            // If close enough → attack
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                break;
            }

            yield return null;
        }

        if (Time.time >= chaseStartTime + chaseDuration)
        {
            Debug.Log("NPC gave up chasing player.");

            agent.ResetPath();
            isBusy = false;
            yield break;
        }

        agent.ResetPath();
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0;

        Quaternion rot = Quaternion.LookRotation(lookDir);

        while (Quaternion.Angle(transform.rotation, rot) > 2f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rot,
                Time.deltaTime * 8f
            );
            yield return null;
        }

        animator.SetTrigger("Attack");

        lastAttackTime = Time.time;

        yield return new WaitForSeconds(.2f);

        isBusy = false;
    }

    /* ==========================
     * POSITION CALCULATIONS
     * ========================== */

    private Vector3 GetFlankPosition()
    {
        Vector3 toEnemy = transform.position - player.position;
        Vector3 flankDir = Vector3.Cross(Vector3.up, toEnemy).normalized;

        if (Random.value > 0.5f)
            flankDir *= -1f;

        Vector3 flankPos = player.position + flankDir * flankRadius;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(flankPos, out hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    private Transform FindNearestCover()
    {
        Collider[] covers = Physics.OverlapSphere(transform.position, 10f, coverLayer);
        Transform best = null;
        float bestDist = Mathf.Infinity;

        foreach (var c in covers)
        {
            print(c);
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = c.transform;
            }
        }
        print(best);
        return best;
    }
}
