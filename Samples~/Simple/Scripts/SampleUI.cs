using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndustryCSE.Tool.ProductConfigurator.Sample.Simple
{
    public class SampleUI : MonoBehaviour
    {
        [SerializeField]
        private UIDocument uiDocument;
        
        private void Start()
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
                        VariantSetBase.VariantTriggered?.Invoke(variantSetBase.VariantSetAsset, variantBase.variantAsset, true);
                    };
                    variantSetContainer.Add(newButton);
                }
                
                uiDocument.rootVisualElement.Add(variantSetContainer);
            }
        }
    }
}