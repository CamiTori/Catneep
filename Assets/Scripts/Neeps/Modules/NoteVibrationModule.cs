using UnityEngine;

using Catneep.Utils;


namespace Catneep.Neeps.Modules
{

    [CustomEffectModule(visualMenu + "Note Vibration")]
    public class NoteVibrationModule : EffectModule
    {

        [SerializeField]
        private Wave vibration;


        private NotesUI ui;


        protected override void OnInitialize(NeepEffect owner)
        {
            ui = owner.Manager.UI;
        }

        protected internal override void Show(bool show)
        {
            if (show)
            {
                ui.NoteShakeFrequency = vibration.Frequency;
            }
            else ui.ResetShake();
        }

        protected internal override void Fade(float fade)
        {
            ui.NoteShakeIntensity = vibration.intensity * fade;
        }

    }
}
