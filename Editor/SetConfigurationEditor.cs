using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    [CustomEditor(typeof(SetConfiguration))]
    public class SetConfigurationEditor : UnityEditor.Editor
    {
        private SetConfiguration setConfiguration;
        private Slider optionSlider;
        
        private void OnEnable()
        {
            setConfiguration = target as SetConfiguration;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(myInspector, serializedObject, this);
            optionSlider = new Slider("Option Slider", 0, setConfiguration.OptionDetails.Length - 1, SliderDirection.Horizontal, 1);
            var prop = serializedObject.FindProperty("optionDetails.Array.size");
            optionSlider.TrackPropertyValue(prop, OnOptionCountChanged);
            optionSlider.RegisterValueChangedCallback(OnSliderOptionChanged);
            myInspector.Add(optionSlider);
            // Return the finished inspector UI
            return myInspector;
        }

        private void OnOptionCountChanged(SerializedProperty obj)
        {
            optionSlider.highValue = obj.intValue - 1;
            optionSlider.value = Mathf.Min(optionSlider.value, optionSlider.highValue);
        }

        private void OnDisable()
        {
            optionSlider.UnregisterValueChangedCallback(OnSliderOptionChanged);
        }
        
        private void OnSliderOptionChanged(ChangeEvent<float> evt)
        {
            setConfiguration.SetOption((int)evt.newValue);
        }
    }
}
