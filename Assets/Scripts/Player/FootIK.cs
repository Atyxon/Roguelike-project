using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FootIK : MonoBehaviour
{
    [Header("Ground / Raycast")]
    public LayerMask groundLayer = ~0;
    public float raycastStartHeight = 0.25f;
    public float raycastDistance = 1.0f;
    public float footOffset = 0.08f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.06f;
    public float rotationSpeed = 15f;
    public float ikWeightSmoothTime = 0.12f;

    [Header("Planted / Locking")]
    public float minPlantDistance = 0.03f;
    public float minPlantTime = 0.08f;
    public float maxUnplantDistance = 0.15f;
    public float lockedPositionBlendOutSpeed = 4f;

    [Header("Foot rotation limits (degrees)")]
    public float maxFootPitch = 35f;
    public float maxFootRoll = 25f;

    [Header("Pelvis")]
    public float pelvisSmooth = 8f;
    public float maxPelvisDrop = 0.45f;

    // runtime
    Animator animator;
    float pelvisOriginOffset;
    float pelvisDrop;
    float currentIKWeight = 1f;
    float ikWeightVelocity;

    struct FootState
    {
        public AvatarIKGoal goal;
        public Vector3 smoothedPos;     // what we are telling IK this frame
        public Vector3 velocity;        // for SmoothDamp
        public Quaternion smoothedRot;

        public Vector3 lockedWorldPos;  // locked world-space position while planted
        public Quaternion lockedWorldRot;

        public bool isLocked;           // planted flag
        public float nearTargetTime;    // how long we've been near candidate target

        // cached last candidate
        public Vector3 lastCandidatePos;
        public Quaternion lastCandidateRot;
    }

    FootState left, right;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        Transform leftFootT = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFootT = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);

        if (leftFootT != null && rightFootT != null && hips != null)
        {
            float avgFeetY = (leftFootT.position.y + rightFootT.position.y) * 0.5f;
            pelvisOriginOffset = hips.position.y - avgFeetY;
        }
        else
        {
            pelvisOriginOffset = 0.9f;
        }

        // initialize foot states
        left.goal = AvatarIKGoal.LeftFoot;
        right.goal = AvatarIKGoal.RightFoot;

        left.smoothedPos = animator.GetIKPosition(left.goal);
        right.smoothedPos = animator.GetIKPosition(right.goal);

        left.smoothedRot = animator.GetIKRotation(left.goal);
        right.smoothedRot = animator.GetIKRotation(right.goal);

        left.lockedWorldPos = left.smoothedPos;
        right.lockedWorldPos = right.smoothedPos;

        left.lockedWorldRot = left.smoothedRot;
        right.lockedWorldRot = right.smoothedRot;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        float speedPercent = 0f;
        if (animator.HasParameterOfType("speedPercent", AnimatorControllerParameterType.Float))
            speedPercent = animator.GetFloat("speedPercent");
        else
            speedPercent = animator.velocity.magnitude;

        float ikTarget = (speedPercent > 0.1f) ? 0f : 1f;
        currentIKWeight = Mathf.SmoothDamp(currentIKWeight, ikTarget, ref ikWeightVelocity, ikWeightSmoothTime);

        float effectiveCast = Mathf.Lerp(0.02f, raycastDistance, currentIKWeight);

        UpdateOneFoot(ref left, effectiveCast);
        UpdateOneFoot(ref right, effectiveCast);

        AdjustPelvisHeight();

        // Apply to animator
        ApplyFootToAnimator(left);
        ApplyFootToAnimator(right);
    }

    void UpdateOneFoot(ref FootState foot, float castDistance)
    {
        Vector3 animFootPos = animator.GetIKPosition(foot.goal);
        Quaternion animFootRot = animator.GetIKRotation(foot.goal);

        Vector3 rayOrigin = animFootPos + Vector3.up * raycastStartHeight;

        bool hit = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hitInfo, castDistance + raycastStartHeight, groundLayer);
        Vector3 candidatePos = animFootPos;
        Quaternion candidateRot = animFootRot;
        if (hit)
        {
            candidatePos = hitInfo.point + Vector3.up * footOffset;
            Quaternion groundRot = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * animFootRot;

            Vector3 e = groundRot.eulerAngles;
            if (e.x > 180) e.x -= 360;
            if (e.z > 180) e.z -= 360;

            e.x = Mathf.Clamp(e.x, -maxFootPitch, maxFootPitch);
            e.z = Mathf.Clamp(e.z, -maxFootRoll, maxFootRoll);

            float animYaw = animFootRot.eulerAngles.y;
            candidateRot = Quaternion.Euler(e.x, animYaw, e.z);
        }

        foot.lastCandidatePos = candidatePos;
        foot.lastCandidateRot = candidateRot;

        float distToCandidate = Vector3.Distance(animFootPos, candidatePos);
        if (hit && distToCandidate <= minPlantDistance)
        {
            foot.nearTargetTime += Time.deltaTime;
            if (foot.nearTargetTime >= minPlantTime && !foot.isLocked)
            {
                foot.isLocked = true;
                foot.lockedWorldPos = candidatePos;
                foot.lockedWorldRot = candidateRot;
            }
        }
        else
        {
            foot.nearTargetTime = 0f;
            if (foot.isLocked)
            {
                float distFromLock = Vector3.Distance(animFootPos, foot.lockedWorldPos);
                if (distFromLock > maxUnplantDistance)
                {
                    foot.isLocked = false;
                }
            }
        }

        if (foot.isLocked)
        {
            foot.lockedWorldPos = Vector3.Lerp(foot.lockedWorldPos, candidatePos, Time.deltaTime * (lockedPositionBlendOutSpeed * currentIKWeight));
            foot.lockedWorldRot = Quaternion.Slerp(foot.lockedWorldRot, candidateRot, Time.deltaTime * (lockedPositionBlendOutSpeed * currentIKWeight));

            foot.smoothedPos = Vector3.SmoothDamp(foot.smoothedPos, foot.lockedWorldPos, ref foot.velocity, positionSmoothTime);
            foot.smoothedRot = Quaternion.Slerp(foot.smoothedRot, foot.lockedWorldRot, Time.deltaTime * rotationSpeed);
        }
        else
        {
            if (hit)
            {
                foot.smoothedPos = Vector3.SmoothDamp(foot.smoothedPos, candidatePos, ref foot.velocity, positionSmoothTime);
                foot.smoothedRot = Quaternion.Slerp(foot.smoothedRot, candidateRot, Time.deltaTime * rotationSpeed);
            }
            else
            {
                float slowOut = Mathf.Max(0.02f, positionSmoothTime * 1.6f);
                foot.smoothedPos = Vector3.SmoothDamp(foot.smoothedPos, animFootPos, ref foot.velocity, slowOut);
                foot.smoothedRot = Quaternion.Slerp(foot.smoothedRot, animFootRot, Time.deltaTime * (rotationSpeed * 0.5f));
                foot.isLocked = false;
            }
        }

        float w = currentIKWeight;
        Vector3 blendedPos = Vector3.Lerp(animFootPos, foot.smoothedPos, w);
        Quaternion blendedRot = Quaternion.Slerp(animFootRot, foot.smoothedRot, w);

        foot.smoothedPos = blendedPos;
        foot.smoothedRot = blendedRot;
    }

    void ApplyFootToAnimator(FootState foot)
    {
        animator.SetIKPositionWeight(foot.goal, currentIKWeight);
        animator.SetIKRotationWeight(foot.goal, currentIKWeight);

        animator.SetIKPosition(foot.goal, foot.smoothedPos);
        animator.SetIKRotation(foot.goal, foot.smoothedRot);
    }

    void AdjustPelvisHeight()
    {
        float expectedFootY = animator.bodyPosition.y - pelvisOriginOffset;

        float leftY = left.smoothedPos.y;
        float rightY = right.smoothedPos.y;

        float leftGap = Mathf.Max(0f, expectedFootY - leftY);
        float rightGap = Mathf.Max(0f, expectedFootY - rightY);

        float neededDrop = Mathf.Clamp(Mathf.Max(leftGap, rightGap), 0f, maxPelvisDrop);

        pelvisDrop = Mathf.Lerp(pelvisDrop, neededDrop, Time.deltaTime * pelvisSmooth);

        Vector3 p = animator.bodyPosition;
        p.y = animator.bodyPosition.y - pelvisDrop * currentIKWeight;
        animator.bodyPosition = p;
    }
}

// Small helper extension: optional (detects parameter)
public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, string name, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }
}
