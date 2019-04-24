using UnityEngine;


namespace Catneep.Neeps.Modules
{

    [CustomEffectModule(audioMenu + "Reverb Filter")]
    public class ReverbFilterModule : EffectModuleAudio<AudioReverbFilter>
    {

        [SerializeField]
        private AudioReverbPreset reverbPreset = AudioReverbPreset.Off;

        protected internal override void Show(bool show)
        {
            base.Show(show);

            if (show) AudioFilter.reverbPreset = reverbPreset;
        }

    }

}
