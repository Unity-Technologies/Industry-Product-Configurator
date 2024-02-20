using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Cinemachine;

namespace IndustryCSE.Tool.ProductConfigurator.Sample.CameraControl
{
    public class SimpleCameraControl : MonoBehaviour
    {
        [SerializeField]
        private UIDocument uiDocument;
        private DropdownField _cameraDropdownField;
        
        [SerializeField] private CinemachineVirtualCamera defaultCamera;
        private CinemachineVirtualCamera[] _cameraOptions;
        
        private void Start()
        {
            DistributeVariantSetControl();

            SwitchCamera(defaultCamera);

            MakeCameraUIControl();
        }

        private void OnDestroy()
        {
            _cameraDropdownField.UnregisterValueChangedCallback(OnCameraDropDownValueChange);
        }

        private void MakeCameraUIControl()
        {
            List<string> cameraChoices = _cameraOptions.Select(x => x.Name).ToList();
            _cameraDropdownField =
                new DropdownField(cameraChoices, Array.FindIndex(_cameraOptions, x => x == defaultCamera))
                {
                    style =
                    {
                        position = Position.Absolute,
                        right = new Length(0f, LengthUnit.Pixel),
                        top = new Length(0f, LengthUnit.Pixel)
                    }
                };

            _cameraDropdownField.RegisterValueChangedCallback(OnCameraDropDownValueChange);
            
            uiDocument.rootVisualElement.Add(_cameraDropdownField);
        }

        private void OnCameraDropDownValueChange(ChangeEvent<string> evt)
        {
            if ((CinemachineVirtualCamera) Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera ==
                _cameraOptions[_cameraDropdownField.index]) return;
            SwitchCamera(_cameraOptions[_cameraDropdownField.index]);
        }

        private void SwitchCamera(CinemachineVirtualCamera toCamera)
        {
            _cameraOptions ??=
                FindObjectsByType<CinemachineVirtualCamera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var optionalCamera in _cameraOptions.Where(x => x != toCamera))
            {
                optionalCamera.Priority = 0;
            }
            toCamera.Priority = 10;
            
            //Update the dropdown field
            if(_cameraDropdownField == null) return;
            var index = Array.FindIndex(_cameraOptions, x => x == toCamera);
            if (_cameraDropdownField.index != index)
            {
                _cameraDropdownField.SetValueWithoutNotify(_cameraOptions[index].Name);
            }
        }

        private void DistributeVariantSetControl()
        {
            var allVariantSets = FindObjectsByType<VariantSetBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var variantSetBase in allVariantSets)
            {
                if(variantSetBase.Hide) continue;
                var variantSetContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexWrap = Wrap.Wrap,
                        color = new StyleColor(Color.white)
                    }
                };
                
                var variantSetLabel = new Label
                {
                    text = variantSetBase.VariantSetAsset.VariantSetName,
                    style =
                    {
                        backgroundColor = new StyleColor(Color.black)
                    }
                };
                variantSetContainer.Add(variantSetLabel);

                foreach (var variantBase in variantSetBase.VariantBase)
                {
                    var newButton = new Button
                    {
                        text = variantBase.variantAsset.VariantName
                    };
                    newButton.clicked += () =>
                    {
                        //Switch camera if the variant set has a focus camera
                        if (variantSetBase.FocusCamera != null)
                        {
                            SwitchCamera(variantSetBase.FocusCamera);
                        }
                        VariantSetBase.VariantTriggered?.Invoke(variantSetBase.VariantSetAsset, variantBase.variantAsset, true);
                    };
                    variantSetContainer.Add(newButton);
                }
                
                uiDocument.rootVisualElement.Add(variantSetContainer);
            }
        }
    }
}
