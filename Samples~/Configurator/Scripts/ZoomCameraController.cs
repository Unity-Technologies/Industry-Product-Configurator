using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Unity.IndustryCSE.ProductConfigurator.Sample.StandardConfigurator
{
    public class ZoomCameraController : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 5f)] private float zoomSpeed = 1f;

        [SerializeField] private float minCameraDistance = 0.25f;
        
        [SerializeField] private float maxCameraDistance = 7f;
        
        [SerializeField] private InputActionReference zoomActionReference;
        
        CinemachineBrain _cinemachineBrain;

        private InputAction zoomAction;
        
        CinemachineBrainEvents _cinemachineBrainEvents;
        
        private CinemachinePositionComposer _PositionTransposer;
        
        private CinemachineVirtualCameraBase _virtualCamera;
        
        private void Awake()
        {
            _virtualCamera = GetComponent<CinemachineVirtualCameraBase>();
            _cinemachineBrain = FindAnyObjectByType<CinemachineBrain>();
            _cinemachineBrainEvents = FindAnyObjectByType<CinemachineBrainEvents>();
            _cinemachineBrainEvents.CameraActivatedEvent.AddListener(OnCameraActivatedEvent);
            _PositionTransposer = gameObject.GetComponent<CinemachinePositionComposer>();
        }

        private void Start()
        {
            zoomAction = zoomActionReference.action;
            zoomAction.performed += OnZoomAction;
            
            if ((CinemachineVirtualCameraBase)_cinemachineBrain.ActiveVirtualCamera == _virtualCamera)
            {
                zoomAction.Enable();
            }
        }

        private void OnDestroy()
        {
            _cinemachineBrainEvents.CameraActivatedEvent.RemoveListener(OnCameraActivatedEvent);
            zoomAction.performed -= OnZoomAction;
            zoomAction.Disable();
        }

        private void OnZoomAction(InputAction.CallbackContext obj)
        {
            if(obj.phase != InputActionPhase.Performed) return;
            if(_PositionTransposer == null)return;
            var distance = _PositionTransposer.CameraDistance;
            distance -= obj.ReadValue<float>() * zoomSpeed;
            distance = Mathf.Clamp(distance, minCameraDistance, maxCameraDistance);
            _PositionTransposer.CameraDistance = distance;
        }
        
        private void OnCameraActivatedEvent(ICinemachineMixer arg0, ICinemachineCamera arg1)
        {
            if ((CinemachineVirtualCameraBase)arg1 == _virtualCamera)
            {
                //Activate the zoom action
                zoomAction?.Enable();
            }
            else
            {
                //Deactivate the zoom action
                zoomAction?.Disable();
            }
        }
    }
}
