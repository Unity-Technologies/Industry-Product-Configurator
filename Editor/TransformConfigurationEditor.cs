using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    [CustomEditor(typeof(TransformConfiguration))]
    public class TransformConfigurationEditor : UnityEditor.Editor
    {
        private TransformConfiguration transformConfiguration;
        private Slider optionSlider;
        
        private void OnEnable()
        {
            transformConfiguration = target as TransformConfiguration;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(myInspector, serializedObject, this);
            optionSlider = new Slider("Option Slider", 0, transformConfiguration.OptionDetails.Count - 1, SliderDirection.Horizontal, 1);
            var prop = serializedObject.FindProperty("optionDetails.Array.size");
            optionSlider.TrackPropertyValue(prop, OnOptionCountChanged);
            optionSlider.RegisterValueChangedCallback(OnSliderOptionChanged);
            myInspector.Add(optionSlider);
            // Return the finished inspector UI
            return myInspector;
        }
        
        private void OnDisable()
        {
            optionSlider.UnregisterValueChangedCallback(OnSliderOptionChanged);
            //optionSlider.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        }
        
        private void OnOptionCountChanged(SerializedProperty obj)
        {
            optionSlider.highValue = obj.intValue - 1;
            optionSlider.value = Mathf.Min(optionSlider.value, optionSlider.highValue);
        }
        
        private void OnSliderOptionChanged(ChangeEvent<float> evt)
        {
            transformConfiguration.SetOption((int)evt.newValue);
        }
    }
}
