using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HeadIK : MonoBehaviour
{
    public Transform target;

    [Header("Detection")]
    public float checkRadius = 5f;
    public LayerMask detectionLayer;
    [Range(-1f, 1f)]
    public float dotThreshold = 0.3f;

    [Header("Rig")]
    public Rig headRig;
    public float weightLerpSpeed = 5f;

    private float _desiredWeight;

    private void Update()
    {
        DetectLookTarget();
        headRig.weight = Mathf.Lerp(headRig.weight, _desiredWeight, Time.deltaTime * weightLerpSpeed);
    }

    private void DetectLookTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius, detectionLayer);
        Transform validTarget = null;

        foreach (var hit in hits)
        {
            if (false == hit.CompareTag(GameobjectTags.TAG_LOOK_TARGET))
            {
                continue;
            }

            var directionToTarget = (hit.transform.position - transform.position).normalized;
            var dot = Vector3.Dot(transform.forward, directionToTarget);

            if (dot >= dotThreshold)
            {
                validTarget = hit.transform;
                break;
            }
        }

        if (validTarget != null)
        {
            target.position = validTarget.position;
            _desiredWeight = 1f;
        }
        else
        {
            _desiredWeight = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}