using System;
using System.Collections.Generic;
using UnityEngine;


namespace Catneep.Neeps.Modules
{

    [CustomEffectModule(audioMenu + "Audio Mixer Parameter Setter")]
    public class AudioMixerParameterSetterModule : EffectModule
    {

        [Serializable]
        public class ParameterSetter
        {
            public string name;

            [NonSerialized]
            public float fromValue;
            public float toValue;
        }

        [SerializeField]
        private ParameterSetter[] showSetters = new ParameterSetter[0];

        [SerializeField]
        private ParameterSetter[] fadeSetters = new ParameterSetter[0];

        private IEnumerable<ParameterSetter> AllSetters
        {
            get
            {
                foreach (ParameterSetter setter in showSetters) yield return setter;
                foreach (ParameterSetter setter in fadeSetters) yield return setter;
            }
        }

        protected internal override void Show(bool show)
        {
            if (show)
            {
                foreach (ParameterSetter setter in showSetters)
                {
                    SetFloat(setter.name, setter.toValue);
                }
                foreach (ParameterSetter setter in fadeSetters)
                {
                    setter.fromValue = GetFloat(setter.name);
                }
            }
            else
            {
                foreach (ParameterSetter setter in AllSetters)
                {
                    ClearFloat(setter.name);
                }
            }
        }

        protected internal override void Fade(float fade)
        {
            foreach (ParameterSetter setter in fadeSetters)
            {
                SetFloat(setter.name, Mathf.LerpUnclamped(setter.fromValue, setter.toValue, fade));
            }
        }

        private float GetFloat(string parameter)
        {
            float value = 0f;
            Owner.Manager.EffectMixer.GetFloat(parameter, out value);
            return value;
        }
        private void SetFloat(string parameter, float value)
        {
            Owner.Manager.EffectMixer.SetFloat(parameter, value);
        }
        private bool ClearFloat(string parameter)
        {
            return Owner.Manager.EffectMixer.ClearFloat(parameter);
        }

    }
}