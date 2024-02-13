using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace IndustryCSE.Tool.ProductConfigurator
{
    public class ConfigurationControllerBase : MonoBehaviour
    {
        public event Action<OptionDetailBase> OnOptionSelected;
        
        protected void OptionSelected(OptionDetailBase optionDetailBase)
        {
            OnOptionSelected?.Invoke(optionDetailBase);
        }
    }
}