using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IndustryCSE.Tool.ProductConfigurator.ScriptableObjects;

namespace IndustryCSE.Tool.ProductConfigurator
{
    public abstract class ConfigurationBase : MonoBehaviour
    {
        private ConfigurationControllerBase configurationController;
        
        public Configuration Configuration => configuration;
        
        [SerializeField]
        private Configuration configuration;
        [SerializeField]
        protected int defaultOptionIndex = 0;
    
        public int DefaultOptionIndex => defaultOptionIndex;
        
        public abstract List<OptionDetailBase> Options { get; }

        public bool Hide => hide;
        [SerializeField] protected bool hide = false;
        
        protected virtual void Start()
        {
            configurationController = FindFirstObjectByType<ConfigurationControllerBase>();
            if(configurationController == null) return;
            configurationController.OnOptionSelected += OnOptionChanged;
        }

        protected virtual void OnDestroy()
        {
            if(configurationController == null) return;
            configurationController.OnOptionSelected -= OnOptionChanged;
        }

        protected abstract void OnOptionChanged(OptionDetailBase obj);

        public abstract void SetOption(int value);

        public virtual void SetDefaultIndex(int index)
        {
            defaultOptionIndex = index;
        }
    }

}