using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using IndustryCSE.Tool.ProductConfigurator.Runtime;
using IndustryCSE.Tool.ProductConfigurator.Settings.Editor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace IndustryCSE.Tool.ProductConfigurator.Editor
{
    [CustomEditor(typeof(VariantSelect))]
    public class VariantSelectEditor : UnityEditor.Editor
    {
        private DropdownField variantDropDown;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();
            var so = new SerializedObject(target);
            InspectorElement.FillDefaultInspector(myInspector, so, this);

            var variantSetPropertyField = myInspector.Q<PropertyField>("PropertyField:VariantSet");
            variantSetPropertyField.style.display = DisplayStyle.None;
            var index = myInspector.IndexOf(variantSetPropertyField);
            
            var variantBases = Object.FindObjectsByType<VariantSetBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            DropdownField variantSetDropdown = new DropdownField("Selected Variant Set", variantBases.Select(x => x.VariantSetAsset.VariantSetName).ToList(), 0)
                {
                    userData = variantBases,
                };
            variantSetDropdown.RegisterValueChangedCallback(OnVariantSetDropdownChanged);
            myInspector.Insert(index, variantSetDropdown);

            var variantSelect = target as VariantSelect;

            variantSetDropdown.SetValueWithoutNotify(variantSelect.VariantSet == null
                ? string.Empty
                : variantSelect.VariantSet.VariantSetAsset.VariantSetName);

            variantDropDown = new DropdownField("Selected Variant");
            
            var variantAssetPropertyField = myInspector.Q<PropertyField>("PropertyField:VariantAsset");
            variantAssetPropertyField.style.display = DisplayStyle.None;
            index = myInspector.IndexOf(variantAssetPropertyField);
            if (variantSelect.VariantSet == null)
            {
                variantDropDown.style.display = DisplayStyle.None;
            }
            else
            {
                AssignVariantOption();
                variantDropDown.SetValueWithoutNotify(variantSelect.VariantAsset != null
                    ? variantSelect.VariantAsset.VariantName
                    : string.Empty);
            }
            variantDropDown.RegisterValueChangedCallback(OnVariantDropDownChanged);
            myInspector.Insert(index, variantDropDown);
            
            var triggerConditionalVariantsPropertyField = myInspector.Q<PropertyField>("PropertyField:triggerConditionalVariants");
            triggerConditionalVariantsPropertyField.style.display = PackageSettingsController.Settings.UseAdvancedSettings? DisplayStyle.Flex : DisplayStyle.None;
            
            return myInspector;
        }

        private void AssignVariantOption()
        {
            var variantSelect = target as VariantSelect;
            var allVariants = variantSelect.VariantSet.VariantBase;
            var allOptions = allVariants.Select(x => x.variantAsset.VariantName).ToList();
            variantDropDown.choices = allOptions;
            variantDropDown.userData = allVariants;
        }
        
        private void OnVariantSetDropdownChanged(ChangeEvent<string> evt)
        {
            var dropdown = (DropdownField)evt.target;
            if(dropdown.index < 0) return;
            var variantSet = (dropdown.userData as VariantSetBase[])[dropdown.index];
            var variantSelect = target as VariantSelect;
            if (variantSelect.VariantSet != null && variantSelect.VariantSet == variantSet) return;
            variantSelect.VariantSet = variantSet;
            if(variantSelect.VariantSet == null) return;
            AssignVariantOption();
            variantDropDown.style.display = DisplayStyle.Flex;
            variantDropDown.SetValueWithoutNotify(string.Empty);
            variantSelect.VariantAsset = null;
        }

        private void OnVariantDropDownChanged(ChangeEvent<string> evt)
        {
            var dropdown = (DropdownField)evt.target;
            if (dropdown.index < 0)  return;
            var variantAsset = (dropdown.userData as List<VariantBase>)[dropdown.index];
            var variantSelect = target as VariantSelect;
            variantSelect.VariantAsset = variantAsset.variantAsset;
        }
    }
}
