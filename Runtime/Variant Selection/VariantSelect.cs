using System.Linq;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

namespace IndustryCSE.Tool.ProductConfigurator.Runtime
{
    public class VariantSelect : MonoBehaviour
    {
        public VariantSetBase VariantSet;
        
        public VariantAsset VariantAsset;
        
        [SerializeField, Tooltip("Will add listener if detected button or toggle component")]
        private bool autoInitialise = true;

        [SerializeField]
        private bool triggerVariantSetCinemachineCamera;
        
        [SerializeField]
        private bool triggerConditionalVariants = true;

        private void Start()
        {
            if(!autoInitialise) return;
            
            if (transform.TryGetComponent(out Button button))
            {
                button.onClick.AddListener(SelectVariant);
            }
            if (transform.TryGetComponent(out Toggle toggle))
            {
                toggle.onValueChanged.AddListener(SelectVariant);
            }
        }

        private void OnDestroy()
        {
            if(!autoInitialise) return;
            if (transform.TryGetComponent(out Button button))
            {
                button.onClick.RemoveListener(SelectVariant);
            }
            if (transform.TryGetComponent(out Toggle toggle))
            {
                toggle.onValueChanged.RemoveListener(SelectVariant);
            }
        }

        public virtual void SelectVariant()
        {
            if (VariantSet == null || VariantAsset == null) return;
            VariantSetBase.VariantTriggered?.Invoke(VariantSet.VariantSetAsset, VariantAsset, triggerConditionalVariants);
            if (triggerVariantSetCinemachineCamera)
            {
                SwitchCamera();
            }
        }

        public virtual void SelectVariant(bool selected)
        {
            if(!selected) return;
            SelectVariant();
        }

        public virtual void SwitchCamera()
        {
            if (VariantSet.FocusCamera != null)
            {
                var allCameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(x => x != VariantSet.FocusCamera);
                foreach (var cinemachineCamera in allCameras)
                {
                    cinemachineCamera.Priority = 0;
                }

                VariantSet.FocusCamera.Priority = 1;
            }
        }
    }
}