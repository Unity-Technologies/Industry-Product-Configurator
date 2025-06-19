using UnityEngine;
using UnityEngine.InputSystem;

namespace IndustryCSE.Tool.ProductConfigurator.Shared.Runtime
{
    public class CentreOrbit : MonoBehaviour
    {
        public InputActionReference doubleClickAction;
        public InputActionReference panAction;
        public InputActionReference orbitAction; // New input for orbiting

        public GameObject target;
        private Vector3 startPosition;
        private Vector2 lastMousePosition;
        private bool isDragging = false;
        public float moveSpeed = 0;
        public float scale = -0.1f;
        public float distanceFromTarget = 0;

        public float orbitSensitivity = 180f; // Degrees per screen width
        private float yaw;
        private float pitch;

        void Start()
        {
            doubleClickAction.action.Enable();
            panAction.action.Enable();
            orbitAction.action.Enable();

            startPosition = target.transform.position;
            doubleClickAction.action.performed += DoubleClick;
            panAction.action.performed += Pan;
            orbitAction.action.performed += Orbit;
        }

        private void DoubleClick(InputAction.CallbackContext obj)
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.value);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    target.transform.position = hit.point;
                }
                else
                {
                    target.transform.position = startPosition;
                }
            }
        }

        void Pan(InputAction.CallbackContext obj)
        {
            Vector2 delta = obj.ReadValue<Vector2>();

            float normalizedX = delta.x / Screen.width;
            float normalizedY = delta.y / Screen.height;

            Debug.Log($"Delta: {delta}, Normalized: ({normalizedX}, {normalizedY})");

            if (Camera.main != null)
            {
                Vector3 right = Camera.main.transform.right;
                Vector3 up = Camera.main.transform.up;

                Vector3 movement = (right * normalizedX + up * normalizedY) * (moveSpeed * Time.deltaTime * 1000f);
                target.transform.position += movement;
            }
        }

        void Orbit(InputAction.CallbackContext context)
        {
            Vector2 delta = context.ReadValue<Vector2>();

            float normalizedX = delta.x / Screen.width;
            float normalizedY = delta.y / Screen.height;

            yaw += normalizedX * orbitSensitivity;
            pitch -= normalizedY * orbitSensitivity;
            pitch = Mathf.Clamp(pitch, -85f, 85f);

            if (target != null && Camera.main != null)
            {
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
                Vector3 direction = rotation * Vector3.back;

                Camera.main.transform.position = target.transform.position + direction * distanceFromTarget;
                Camera.main.transform.rotation = rotation;
            }
        }

        void Update()
        {
            if (Camera.main != null)
                distanceFromTarget = Vector3.Distance(Camera.main.transform.position, target.transform.position);
            moveSpeed = distanceFromTarget * scale;
        }
    }
}
