using Console;
using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Cursor")]
        public bool lockCursor = true;

        [Header("Target")]
        public Transform target;

        [Header("Rotation")]
        public float mouseSensitivity = 10f;
        public Vector2 pitchMinMax = new Vector2(-40f, 85f);
        public float rotationSmoothTime = 0.12f;

        [Header("Distance")]
        public float baseDistanceTarget = 6f;
        public float dstFromTarget = 6f;

        [Header("Collision")]
        public LayerMask collisionMask;
        public float cameraRadius = 0.35f;

        private Vector3 _rotationSmoothVelocity;
        private Vector3 _currentRotation;

        private float _yaw;
        private float _pitch;

        private void Start()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            dstFromTarget = baseDistanceTarget;
        }

        private void LateUpdate()
        {
            HandleRotation();
            HandleCameraCollision();
            HandlePosition();
        }

        private void HandleRotation()
        {
            if (DevConsole.Instance != null && DevConsole.Instance.IsConsoleActive()) return;
            
            _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, pitchMinMax.x, pitchMinMax.y);

            _currentRotation = Vector3.SmoothDamp(_currentRotation, new Vector3(_pitch, _yaw), ref _rotationSmoothVelocity, rotationSmoothTime);
            transform.eulerAngles = _currentRotation;
        }

        private void HandlePosition()
        {
            transform.position = target.position - transform.forward * dstFromTarget;
        }

        private void HandleCameraCollision()
        {
            var origin = target.position;
            var direction = (transform.position - target.position).normalized;

            if (Physics.SphereCast(origin, cameraRadius, direction, out var hit, baseDistanceTarget, collisionMask, QueryTriggerInteraction.Ignore))
            {
                var distance = Vector3.Distance(origin, hit.point);
                dstFromTarget = Mathf.Clamp(distance - cameraRadius,0.5f, baseDistanceTarget);
            }
            else
            {
                dstFromTarget = Mathf.Lerp(dstFromTarget, baseDistanceTarget, Time.deltaTime * 5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!target) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                target.position + (transform.position - target.position).normalized * dstFromTarget,
                cameraRadius
            );
        }
    }
}
