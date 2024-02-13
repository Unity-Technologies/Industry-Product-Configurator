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

                switch (configurationBase)
                {
                    case GameObjectConfiguration gameObjectConfiguration:
                        foreach (var gameObjectOptionDetail in gameObjectConfiguration.OptionDetails)
                        {
                            var newButton = new Button
                            {
                                text = gameObjectOptionDetail.configurationOption.name
                            };
                            newButton.clicked += () =>
                            {
                                OptionSelected(gameObjectOptionDetail);
                            };
                            configuration.Add(newButton);
                        }
                        break;
                    case TransformConfiguration transformConfiguration:
                        foreach (var transformOptionDetail in transformConfiguration.OptionDetails)
                        {
                            var newButton = new Button
                            {
                                text = transformOptionDetail.configurationOption.name
                            };
                            newButton.clicked += () =>
                            {
                                OptionSelected(transformOptionDetail);
                            };
                            configuration.Add(newButton);
                        }
                        break;
                    
                    case MaterialConfiguration materialConfiguration:
                        foreach (var materialOptionDetail in materialConfiguration.OptionDetails)
                        {
                            var newButton = new Button
                            {
                                text = materialOptionDetail.configurationOption.name
                            };
                            newButton.clicked += () =>
                            {
                                OptionSelected(materialOptionDetail);
                            };
                            configuration.Add(newButton);
                        }
                        break;
                    
                    case SetConfiguration setConfiguration:
                        foreach (var optionDetailBase in setConfiguration.OptionDetails)
                        {
                            var newButton = new Button
                            {
                                text = optionDetailBase.configurationOption.name
                            };
                            newButton.clicked += () =>
                            {
                                OptionSelected(optionDetailBase);
                            };
                            configuration.Add(newButton);
                        }
                        break;
                }
                
                uiDocument.rootVisualElement.Add(configuration);
            }
        }
    }
}