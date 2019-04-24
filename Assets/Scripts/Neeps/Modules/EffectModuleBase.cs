using System;
using UnityEngine;

namespace Catneep.Neeps.Modules
{



    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CustomEffectModuleAttribute : Attribute
    {

        public CustomEffectModuleAttribute(string menuName)
        {
            this.menuName = menuName;
        }

        private readonly string menuName;
        public string MenuName { get { return menuName; } }

    }

    public abstract class EffectModule : ScriptableObject
    {

        public const string audioMenu = "Audio/";
        public const string visualMenu = "Visual/";


        [HideInInspector]
        [SerializeField]
        private NeepEffect owner;
        public NeepEffect Owner { get { return owner; } }

        private void OnEnable()
        {
#if UNITY_EDITOR
            //string title = UnityEditor.ObjectNames.GetInspectorTitle(this);
            //name = title.Substring(2, title.Length - 3);
            name = UnityEditor.ObjectNames.NicifyVariableName(this.GetType().Name);

            this.hideFlags = HideFlags.HideInHierarchy;
#endif
        }

#if UNITY_EDITOR

        public NeepEffect SetOwner { set { owner = value; } }

        [ContextMenu("Remove Module", false)]
        private void OnRemoveModule()
        {
            DestroyImmediate(this, true);
        }
#endif


        protected virtual void OnDestroy()
        {

        }


        internal void Initialize(NeepEffect owner)
        {
            this.owner = owner;
            OnInitialize(owner);
        }
        protected virtual void OnInitialize(NeepEffect owner)
        {

        }

        protected internal virtual void Show(bool show)
        {

        }

        protected internal virtual void Fade(float fade)
        {

        }

    }

}