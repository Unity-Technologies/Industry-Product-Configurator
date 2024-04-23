using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace IndustryCSE.Tool.ProductConfigurator.Sample.StandardConfigurator
{
    public class CustomCinemachineInputProvider : MonoBehaviour
    {
        [SerializeField]
        private CinemachineInputProvider cinemachineInputProvider;

        private void Update()
        {
            cinemachineInputProvider.enabled = !IsPointerOverUIElement();
        }

        public static bool IsPointerOverUIElement()
        {
            return GetEventSystemRaycastResults() != null && GetEventSystemRaycastResults().Count > 0;
        }

        static List<RaycastResult> GetEventSystemRaycastResults()
        {
            if (EventSystem.current == null) return null;
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            if (Pointer.current == null) return null;
            eventData.position = Pointer.current.position.ReadValue();
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            raycastResults.RemoveAll(x => x.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"));
            return raycastResults;
        }
    }
}

