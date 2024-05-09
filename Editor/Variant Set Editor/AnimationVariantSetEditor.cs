using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    [CustomEditor(typeof(AnimationVariantSet))]
    public class AnimationVariantSetEditor : CustomConfigurationEditorBase
    {
        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = base.CreateInspectorGUI();
            var sliderContainer = myInspector.Q<VisualElement>("Variant Slider Container");
            if (sliderContainer != null)
            {
                myInspector.Remove(sliderContainer);
            }
            
            var captureImageContainer = myInspector.Q<VisualElement>("Capture Image Container");
            if (captureImageContainer != null)
            {
                myInspector.Remove(captureImageContainer);
            }
            return myInspector;
        }
    }
}
