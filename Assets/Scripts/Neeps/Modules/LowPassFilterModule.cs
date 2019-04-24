using UnityEngine;


namespace Catneep.Neeps.Modules
{

    [CustomEffectModule(audioMenu + "Low Pass Filter")]
    public class LowPassFilterModule : EffectModulePassFilter<AudioLowPassFilter>
    {

        public override float LerpFrom { get { return maxFrequency; } }

        public override float CutoffSetProperty { set { AudioFilter.cutoffFrequency = value; } }

    }

}
