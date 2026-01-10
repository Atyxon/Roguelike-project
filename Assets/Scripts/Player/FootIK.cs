using UnityEngine;

namespace Player
{
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
        private Animator animator;
        private PlayerController pc;
        private float pelvisOriginOffset;
        private float pelvisDrop;
        private float currentIKWeight = 1f;
        private float ikWeightVelocity;

        struct FootState
        {
            public AvatarIKGoal goal;
            public Vector3 smoothedPos;
            public Vector3 velocity;
            public Quaternion smoothedRot;

            public Vector3 lockedWorldPos;
            public Quaternion lockedWorldRot;

            public bool isLocked;
            public float nearTargetTime;

            public Vector3 lastCandidatePos;
            public Quaternion lastCandidateRot;
        }

        private FootState _left, _right;

        private void Start()
        {
            animator = GetComponent<Animator>();
            pc = GetComponent<PlayerController>();
        
            var leftFootT = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFootT = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (leftFootT != null && rightFootT != null && hips != null)
            {
                var avgFeetY = (leftFootT.position.y + rightFootT.position.y) * 0.5f;
                pelvisOriginOffset = hips.position.y - avgFeetY;
            }
            else
            {
                pelvisOriginOffset = 0.9f;
            }

            _left.goal = AvatarIKGoal.LeftFoot;
            _right.goal = AvatarIKGoal.RightFoot;

            _left.smoothedPos = animator.GetIKPosition(_left.goal);
            _right.smoothedPos = animator.GetIKPosition(_right.goal);

            _left.smoothedRot = animator.GetIKRotation(_left.goal);
            _right.smoothedRot = animator.GetIKRotation(_right.goal);

            _left.lockedWorldPos = _left.smoothedPos;
            _right.lockedWorldPos = _right.smoothedPos;

            _left.lockedWorldRot = _left.smoothedRot;
            _right.lockedWorldRot = _right.smoothedRot;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;

            var speedPercent = 0f;
            speedPercent = animator.HasParameterOfType(AnimDefines.PARAMETER_SPEED_PERCENT, AnimatorControllerParameterType.Float) ? animator.GetFloat(AnimDefines.PARAMETER_SPEED_PERCENT) : animator.velocity.magnitude;

            var ikTarget = (speedPercent > 0.1f || pc.isGrounded == false) ? 0f : 1f;
            currentIKWeight = Mathf.SmoothDamp(currentIKWeight, ikTarget, ref ikWeightVelocity, ikWeightSmoothTime);

            var effectiveCast = Mathf.Lerp(0.02f, raycastDistance, currentIKWeight);

            UpdateOneFoot(ref _left, effectiveCast);
            UpdateOneFoot(ref _right, effectiveCast);

            AdjustPelvisHeight();

            ApplyFootToAnimator(_left);
            ApplyFootToAnimator(_right);
        }

        private void UpdateOneFoot(ref FootState foot, float castDistance)
        {
            var animFootPos = animator.GetIKPosition(foot.goal);
            var animFootRot = animator.GetIKRotation(foot.goal);

            var rayOrigin = animFootPos + Vector3.up * raycastStartHeight;

            var hit = Physics.Raycast(rayOrigin, Vector3.down, out var hitInfo, castDistance + raycastStartHeight, groundLayer);
            var candidatePos = animFootPos;
            var candidateRot = animFootRot;
            if (hit)
            {
                candidatePos = hitInfo.point + Vector3.up * footOffset;
                var groundRot = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * animFootRot;

                var e = groundRot.eulerAngles;
                if (e.x > 180) e.x -= 360;
                if (e.z > 180) e.z -= 360;

                e.x = Mathf.Clamp(e.x, -maxFootPitch, maxFootPitch);
                e.z = Mathf.Clamp(e.z, -maxFootRoll, maxFootRoll);

                var animYaw = animFootRot.eulerAngles.y;
                candidateRot = Quaternion.Euler(e.x, animYaw, e.z);
            }

            foot.lastCandidatePos = candidatePos;
            foot.lastCandidateRot = candidateRot;

            var distToCandidate = Vector3.Distance(animFootPos, candidatePos);
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
                    var distFromLock = Vector3.Distance(animFootPos, foot.lockedWorldPos);
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
                    var slowOut = Mathf.Max(0.02f, positionSmoothTime * 1.6f);
                    foot.smoothedPos = Vector3.SmoothDamp(foot.smoothedPos, animFootPos, ref foot.velocity, slowOut);
                    foot.smoothedRot = Quaternion.Slerp(foot.smoothedRot, animFootRot, Time.deltaTime * (rotationSpeed * 0.5f));
                    foot.isLocked = false;
                }
            }

            var w = currentIKWeight;
            var blendedPos = Vector3.Lerp(animFootPos, foot.smoothedPos, w);
            var blendedRot = Quaternion.Slerp(animFootRot, foot.smoothedRot, w);

            foot.smoothedPos = blendedPos;
            foot.smoothedRot = blendedRot;
        }

        private void ApplyFootToAnimator(FootState foot)
        {
            animator.SetIKPositionWeight(foot.goal, currentIKWeight);
            animator.SetIKRotationWeight(foot.goal, currentIKWeight);

            animator.SetIKPosition(foot.goal, foot.smoothedPos);
            animator.SetIKRotation(foot.goal, foot.smoothedRot);
        }

        private void AdjustPelvisHeight()
        {
            var expectedFootY = animator.bodyPosition.y - pelvisOriginOffset;

            var leftY = _left.smoothedPos.y;
            var rightY = _right.smoothedPos.y;

            var leftGap = Mathf.Max(0f, expectedFootY - leftY);
            var rightGap = Mathf.Max(0f, expectedFootY - rightY);

            var neededDrop = Mathf.Clamp(Mathf.Max(leftGap, rightGap), 0f, maxPelvisDrop);

            pelvisDrop = Mathf.Lerp(pelvisDrop, neededDrop, Time.deltaTime * pelvisSmooth);

            var p = animator.bodyPosition;
            p.y = animator.bodyPosition.y - pelvisDrop * currentIKWeight;
            animator.bodyPosition = p;
        }
    }

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
}