using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndustryCSE.Tool.ProductConfigurator.Sample
{
    public class SampleUI : ConfigurationControllerBase
    {
        [SerializeField]
        private UIDocument uiDocument;
        
        private void Start()
        {
            var allConfigurations = FindObjectsOfType<ConfigurationBase>();

            foreach (var configurationBase in allConfigurations)
            {
                if(configurationBase.Hide) continue;
                var configuration = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexWrap = Wrap.Wrap,
                        color = new StyleColor(Color.white)

                    }
                };
                var configurationName = new Label()
                {
                    text = configurationBase.Configuration.ConfigurationName
                    
                };
                configurationName.style.backgroundColor = new StyleColor(Color.black);
                configuration.Add(configurationName);

                foreach (var option in configurationBase.Options)
                {
                    var newButton = new Button
                    {
                        text = option.configurationOption.name
                    };
                    newButton.clicked += () =>
                    {
                        OptionSelected(option);
                    };
                    configuration.Add(newButton);
                }
                
                uiDocument.rootVisualElement.Add(configuration);
            }
        }
    }
}