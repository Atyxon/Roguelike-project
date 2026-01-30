using UnityEngine;
using UnityEngine.AI;

namespace StateMachines
{
    public class EnableRootMotion : StateMachineBehaviour
    {
        private NavMeshAgent _agent;
        private Animator _animator;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_agent)
                _agent = animator.GetComponent<NavMeshAgent>();

            animator.applyRootMotion = true;
            _agent.updatePosition = false;
            _agent.updateRotation = false;
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.applyRootMotion = false;
            _agent.updatePosition = true;
            _agent.updateRotation = true;
        }
    }
}