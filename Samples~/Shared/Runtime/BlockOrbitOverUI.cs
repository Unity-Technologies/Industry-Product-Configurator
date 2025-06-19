using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IndustryCSE.Tool.ProductConfigurator.Shared.Runtime
{
    public class BlockOrbitOverUI : MonoBehaviour
    {
        public CinemachineInputAxisController inputAxisController;
    
        // Update is called once per frame
        void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                if (inputAxisController.enabled)
                {
                    inputAxisController.enabled = false;
                }
            }
            else
            {
                if (!inputAxisController.enabled)
                {
                    inputAxisController.enabled = true;
                }
            }
        }
    }
}
