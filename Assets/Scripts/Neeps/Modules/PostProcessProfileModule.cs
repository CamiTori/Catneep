using UnityEngine;
using UnityEngine.Rendering.PostProcessing;


namespace Catneep.Neeps.Modules
{

    [CustomEffectModule(visualMenu + "Post Processing")]
    public class PostProcessProfileModule : EffectModule
    {

        [HideInInspector]
        [SerializeField]
        private PostProcessProfile profile;

        private PostProcessVolume volume;

        protected override void OnInitialize(NeepEffect owner)
        {
            if (profile == null) profile = CreateInstance<PostProcessProfile>();

            volume = owner.ContainerGameObject.AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            // Warm up the unloaded shaders to avoid "hiccups" with the fps when the effect starts
            // (Not really working)
            Shader.WarmupAllShaders();
            // TODO: create an instance of ShaderVariantCollection add the shaders and call WarmUp()
        }

        protected internal override void Show(bool show)
        {
            if (!show) volume.weight = 0;
        }

        protected internal override void Fade(float fade)
        {
            volume.weight = fade;
        }

        protected override void OnDestroy()
        {
            DestroyImmediate(profile, true);
            DestroyImmediate(volume);
        }

    }
}