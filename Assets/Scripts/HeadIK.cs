using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HeadIK : MonoBehaviour
{
    public Transform target;

    [Header("Detection")]
    public float checkRadius = 5f;
    public LayerMask detectionLayer;
    [Range(-1f, 1f)]
    public float dotThreshold = 0.3f; // higher = narrower front cone

    [Header("Rig")]
    public Rig headRig;
    public float weightLerpSpeed = 5f;

    private float desiredWeight;

    void Update()
    {
        DetectLookTarget();
        headRig.weight = Mathf.Lerp(
            headRig.weight,
            desiredWeight,
            Time.deltaTime * weightLerpSpeed
        );
    }

    void DetectLookTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius, detectionLayer);

        Transform validTarget = null;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("lookTarget"))
                continue;

            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, directionToTarget);

            // Target must be in front
            if (dot >= dotThreshold)
            {
                validTarget = hit.transform;
                break;
            }
        }

        if (validTarget != null)
        {
            target.position = validTarget.position;
            desiredWeight = 1f;
        }
        else
        {
            desiredWeight = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}