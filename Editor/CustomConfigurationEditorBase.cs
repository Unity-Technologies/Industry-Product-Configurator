using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    public class CustomConfigurationEditorBase : UnityEditor.Editor
    {
        private Slider optionSlider;
        private DropdownField captureSizeDropdown;
        private Button captureImageButton;

        protected virtual void OnEnable() {}

        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = EditorCore.ReturnCommonInspectorElement(target, ref optionSlider, this, 
                OnOptionCountChanged, OnSliderOptionChanged, ref captureImageButton, OnCaptureImageButtonClicked,
                ref captureSizeDropdown);
            
            // Return the finished inspector UI
            return myInspector;
        }

        protected virtual void OnDisable()
        {
            optionSlider.UnregisterValueChangedCallback(OnSliderOptionChanged);
            captureImageButton.clicked -= OnCaptureImageButtonClicked;
        }
        
        private void OnCaptureImageButtonClicked()
        {
            var configurationBase = target as ConfigurationBase;
            var size = EditorCore.IconPresets[captureSizeDropdown.index];
            EditorCore.CaptureOptionImage(configurationBase, size.Width, size.Height);
        }

        private void OnOptionCountChanged(SerializedProperty obj)
        {
            optionSlider.highValue = obj.intValue - 1;
            optionSlider.value = Mathf.Min(optionSlider.value, optionSlider.highValue);
        }

        private void OnSliderOptionChanged(ChangeEvent<float> evt)
        {
            (target as ConfigurationBase).SetOption((int)evt.newValue);
        }
    }
}
