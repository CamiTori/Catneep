using System;
using UnityEngine;

using UnityEngine.Rendering.PostProcessing;

namespace Catneep.Neeps.PostProcessing
{

    [Serializable]
    [PostProcess(typeof(ImageWavingRenderer), PostProcessEvent.AfterStack, "Catneep/Image Waving")]
    public class ImageWaving : PostProcessEffectSettings
    {

        [Min(0)]
        [SerializeField]
        public FloatParameter intensity = new FloatParameter { value = 0.1f };

        [SerializeField]
        public FloatParameter frequency = new FloatParameter { value = 4f };

        [SerializeField]
        public FloatParameter speed = new FloatParameter { value = 1f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value && intensity.value > 0f;
        }

    }

    public class ImageWavingRenderer : PostProcessEffectRenderer<ImageWaving>
    {

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/ImageWaving"));
            sheet.properties.SetFloat("_Intensity", settings.intensity);
            sheet.properties.SetFloat("_Frequency", settings.frequency);
            sheet.properties.SetFloat("_Speed", settings.speed);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }

    }

}