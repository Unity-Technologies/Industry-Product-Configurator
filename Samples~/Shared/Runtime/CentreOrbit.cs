using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace IndustryCSE.Tool.ProductConfigurator.Shared.Runtime
{
    public class CentreOrbit : MonoBehaviour
    {
        public InputActionReference doubleClickAction;
        public InputActionReference panAction;
        public InputActionReference orbitAction; // New input for orbiting
        public InputActionReference zoomAction; // New input for zooming, if needed
        public CinemachineOrbitalFollow cinemachineOrbitalFollow; // Reference to the Cinemachine component
        
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
        private float maxAllowedDistance = 7.5f; // Threshold for touch pinch detection
        private float lastTouchPinchThreshold;
        private float zoomThreshold = 10f; // Threshold for zoom detection

        void Start()
        {
            doubleClickAction.action.Enable();
            panAction.action.Enable();
            orbitAction.action.Enable();
            zoomAction.action.Enable();

            startPosition = target.transform.position;
            doubleClickAction.action.performed += DoubleClick;
            panAction.action.performed += Pan;
            panAction.action.canceled += OnPanCanceled;
            panAction.action.started += OnPanStarted;
            orbitAction.action.performed += Orbit;
            zoomAction.action.performed += Zoom; // Assuming you have a zoom action
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
        
        private void Zoom(InputAction.CallbackContext obj)
        {
            var zoomValue = obj.ReadValue<float>();
            Zooming(zoomValue);
        }

        private void Zooming(float value)
        {
            var zoomAmount = value < 0 ? 0.1f : -0.1f;
            cinemachineOrbitalFollow.RadialAxis.Value = Mathf.Clamp(
                cinemachineOrbitalFollow.RadialAxis.Value + zoomAmount, 
                cinemachineOrbitalFollow.RadialAxis.Range.x, 
                cinemachineOrbitalFollow.RadialAxis.Range.y
            );
        }
        
        private void OnPanStarted(InputAction.CallbackContext obj)
        {
            orbitAction.action.Disable();
        }
        
        private void OnPanCanceled(InputAction.CallbackContext obj)
        {
            orbitAction.action.Enable();
            lastTouchPinchThreshold = 0f; // Reset pinch threshold on pan cancel
        }

        void Pan(InputAction.CallbackContext obj)
        {
            var device = obj.control.device;

            if (device is Touchscreen)
            {
                //Handle with touch input
                TouchControl first = null, second = null;
                int activeTouchCount = 0;
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.isPressed)
                    {
                        activeTouchCount++;
                        if (first == null)
                        {
                            first = touch;
                        } else if (second == null)
                        {
                            second = touch;
                        }
                    }
                        
                }
                if(activeTouchCount != 2)
                    return;

                var distance = 0f;
                
                if (first != null && second != null)
                {
                    Vector2 pos1 = first.position.ReadValue();
                    Vector2 pos2 = second.position.ReadValue();
                    distance = Vector2.Distance(pos1, pos2);
                }
                
                if (lastTouchPinchThreshold == 0f)
                {
                    lastTouchPinchThreshold = distance;
                    return;
                }
                
                var difference = distance - lastTouchPinchThreshold;
                lastTouchPinchThreshold = distance;
                if (Mathf.Abs(difference) > maxAllowedDistance)
                {
                    //Do Zoom instead of pan
                    Zooming(difference);
                    return;
                }
            }
            
            orbitAction.action.Disable();
            
            Vector2 delta = obj.ReadValue<Vector2>();

            float normalizedX = delta.x / Screen.width;
            float normalizedY = delta.y / Screen.height;

            //Debug.Log($"Delta: {delta}, Normalized: ({normalizedX}, {normalizedY})");

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
