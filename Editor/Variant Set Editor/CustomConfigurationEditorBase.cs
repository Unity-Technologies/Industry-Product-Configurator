using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    public abstract class CustomConfigurationEditorBase : UnityEditor.Editor
    {
        public VisualElement DefaultInspectorContainer;
        
        public VisualElement VariantSetContainer;
        public TextField VariantSetNameTextField;
        public Button CreateVariantSetButton;
        
        public VisualElement VariantContainer;
        public TextField VariantNameTextField;
        public Button CreateVariantButton;
        
        public VisualElement VariantSliderContainer;
        public Slider VariantSlider;
        
        public VisualElement CaptureImageContainer;
        public DropdownField CaptureSizeDropdown;
        public Button CaptureImageButton;
        public Button UsePreviousLocationButton;

        protected virtual void OnEnable() {}

        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = EditorCore.ReturnCommonInspectorElement(target, this);
            // Return the finished inspector UI
            return myInspector;
        }

        protected virtual void OnDestroy()
        {
            if (Application.isPlaying) return;
            var variantSetBase = target as VariantSetBase;
            SceneTracker.CheckVariantSet(variantSetBase);
        }

        protected virtual void OnDisable()
        {
            if (VariantSlider != null)
            {
                VariantSlider.UnregisterValueChangedCallback(OnVariantSliderChanged);
            }

            if (CaptureImageButton != null)
            {
                CaptureImageButton.clicked -= OnCaptureImageButtonClicked;
            }
            
            if (CreateVariantSetButton != null)
            {
                CreateVariantSetButton.clicked -= OnCreateNewVariantSetAsset;
            }

            if (VariantSetNameTextField != null)
            {
                VariantSetNameTextField.UnregisterValueChangedCallback(OnVariantSetTextFieldChange);
            }

            if (CreateVariantButton != null)
            {
                CreateVariantButton.clicked -= OnCreateVariant;
            }

            if (UsePreviousLocationButton != null)
            {
                UsePreviousLocationButton.clicked -= OnUsePreviousLocationButtonClicked;
            }
        }

        public void OnUsePreviousLocationButtonClicked()
        {
            SceneView view = SceneView.lastActiveSceneView;
            Camera cam = view.camera;
            var variantSetBase = target as VariantSetBase;
            if(!variantSetBase.VariantSetAsset.hasStoreCameraPositionAndRotation) return;
            // Set the SceneView camera position and rotation
            view.LookAt(variantSetBase.VariantSetAsset.storeCameraPosition, variantSetBase.VariantSetAsset.storeCameraRotation);
        }

        public void ShowVariantSetAssetCreationBtn(bool show)
        {
            CreateVariantSetButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public void ShowVariantContainer(bool show)
        {
            VariantContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            var variantSetBase = target as VariantSetBase;
            if (variantSetBase.VariantBase.Count <= 1)
            {
                VariantSliderContainer.style.display = DisplayStyle.None;
                CaptureImageContainer.style.display = DisplayStyle.None;
                return;
            }
            CaptureImageContainer.style.display = DisplayStyle.Flex;
            VariantSliderContainer.style.display = DisplayStyle.Flex;
        }
        
        public void TrackVariantSetAssetProperty(SerializedProperty obj)
        {
            ShowVariantSetAssetCreationBtn(obj.objectReferenceValue == null);
            ShowVariantContainer(obj.objectReferenceValue != null);
            var variantSetBase = target as VariantSetBase;
            VariantSetNameTextField.SetValueWithoutNotify(variantSetBase.VariantSetAsset == null?
                "Name your variant set here" : variantSetBase.VariantSetAsset.VariantSetName);
        }
        
        public void OnCaptureImageButtonClicked()
        {
            var variantSetBase = target as VariantSetBase;
            var size = EditorCore.IconPresets[CaptureSizeDropdown.index];
            UsePreviousLocationButton.style.display = DisplayStyle.Flex;
            EditorCore.CaptureOptionImage(variantSetBase, size.Width, size.Height);
        }

        public void OnVariantCountChanged(SerializedProperty obj)
        {
            SceneTracker.CheckVariantSet(target as VariantSetBase);
            VariantSlider.highValue = obj.intValue - 1;
            VariantSlider.value = Mathf.Min(VariantSlider.value, VariantSlider.highValue);
            
            if (VariantSlider.highValue < 1)
            {
                VariantSliderContainer.style.display = DisplayStyle.None;
                CaptureImageContainer.style.display = DisplayStyle.None;
                return;
            }
            CaptureImageContainer.style.display = DisplayStyle.Flex;
            VariantSliderContainer.style.display = DisplayStyle.Flex;
        }

        public void OnVariantSliderChanged(ChangeEvent<float> evt)
        {
            (target as VariantSetBase).SetVariant((int)evt.newValue, false);
        }
        
        public void OnVariantSetTextFieldChange(ChangeEvent<string> evt)
        {
            var variantSetBase = target as VariantSetBase;
            if(variantSetBase.VariantSetAsset == null) return;
            variantSetBase.VariantSetAsset.SetName(evt.newValue);
            EditorUtility.SetDirty(variantSetBase.VariantSetAsset);
        }

        public void OnCreateNewVariantSetAsset()
        {
            // Create a new VariantSetAsset
            var variantSetBase = target as VariantSetBase;
            variantSetBase.CreateNewVariantSetAsset(VariantSetNameTextField.value);
        }
        
        public void OnCreateVariant()
        {
            var variantSetBase = target as VariantSetBase;
            variantSetBase.CreateVariantAsset(VariantNameTextField.value);
            VariantNameTextField.SetValueWithoutNotify("Name your variant here");
        }
    }
}
