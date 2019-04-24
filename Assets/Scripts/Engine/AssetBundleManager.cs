/** 
 * 
 * Manager de los asset bundles, que son un conjunto de assets que guarda externamente y
 * que además podemos cargar cuando los necesitemos.
 * 
 * Sin esto se necesitarían actualizar los bundles cada vez que le hagamos un cambio, pero con
 * esto podemos obtener los assets de forma directa en el editor.
 * 
 * Esto nos va a ayudar a tener todas las canciones para cargarlas y añadir más sin tener que actualizar
 * el juego completo. Además de poder cargar los efectos de sonidos y demás.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace Game.AssetBundles
{
    using AssetBundles.Operations;
    
    /// <summary>
    /// Clase estática que se encarga de la carga de los asset bundles para el juego.
    /// </summary>
    public static class AssetBundleManager
    {
        static Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();
        internal static void AddBundle(string name, AssetBundle bundle)
        {
            loadedAssetBundles.Add(name, bundle);
        }
        internal static bool TryToGetBundle(string name, out AssetBundle bundle)
        {
            return loadedAssetBundles.TryGetValue(name, out bundle);
        }

        #if UNITY_EDITOR
        const string simulateBundlesKey = "SimulateAssetBundles";

        static int simulationMode = -1;
        public static bool SimulateAssetBundleEditor
        {
            get
            {
                if (simulationMode < 0 || simulationMode > 1)
                    simulationMode = EditorPrefs.GetBool(simulateBundlesKey, true) ? 1 : 0;

                return simulationMode != 0;
            }
            set
            {
                int intValue = value ? 1 : 0;
                if (intValue != simulationMode)
                {
                    simulationMode = intValue;
                    EditorPrefs.SetBool(simulateBundlesKey, value);
                }
            }
        }
        #endif

        public static readonly string assetBundlesPath = Application.streamingAssetsPath;


        public static LoadAssetsOperation<ReturnT> LoadAllAssetsAsync<ReturnT>(string bundleName) where ReturnT : Object
        {
            return LoadAllAssetsAsync<ReturnT>(bundleName, bundleName);
        }
        public static LoadAssetsOperation<ReturnT> LoadAllAssetsAsync<ReturnT>(string bundleName, string loadingName) where ReturnT : Object
        {
            #if UNITY_EDITOR
            if (SimulateAssetBundleEditor) return new LoadAssetsSimulation<ReturnT>(bundleName, loadingName);
            #endif
            return new LoadAssetsFromBundleOperation<ReturnT>(bundleName, loadingName);
        }


        public static AssetBundleCreateRequest LoadBundleAsync (string bundleName)
        {
            #if UNITY_EDITOR
            if (SimulateAssetBundleEditor) return null;
            #endif

            return AssetBundle.LoadFromFileAsync(Path.Combine(assetBundlesPath, bundleName));
        }


    }
}

namespace Game.AssetBundles.Operations
{
    public abstract class LoadAssetsOperation<AssetsT> : CustomYieldInstruction where AssetsT : Object
    {
        protected bool isDone = false;
        public override bool keepWaiting { get { return !isDone; } }

        protected readonly bool typeIsComponent = typeof(AssetsT).IsSubclassOf(typeof(Component));

        public AssetsT[] Assets { get; protected set; }

        protected readonly string bundleName;
        protected readonly string loadingName;

        public LoadAssetsOperation(string bundleName, string loadingName)
        {
            this.bundleName = bundleName;
            this.loadingName = loadingName;
        }

        public virtual IEnumerator Execute()
        {
            yield return null;
        }
    }

    #if UNITY_EDITOR
    public class LoadAssetsSimulation<AssetsT> : LoadAssetsOperation<AssetsT> where AssetsT : Object
    {
        public LoadAssetsSimulation(string bundleName, string loadingName) : base(bundleName, loadingName) { }

        public override IEnumerator Execute()
        {
            yield return null;

            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            List<AssetsT> loadedAssets = new List<AssetsT>();
            foreach (string path in assetPaths)
            {
                Object obj = AssetDatabase.LoadMainAssetAtPath(path);

                AssetsT asset = null;
                try
                {
                    asset = typeIsComponent ? (obj as GameObject).GetComponent<AssetsT>() : obj as AssetsT;
                }
                catch
                {
                    //Debug.LogWarningFormat("Couldn't load asset \"{0}\" for \"{1}\".", path, typeof(AssetsT));
                }

                if (asset) loadedAssets.Add(asset);
            }
            Assets = loadedAssets.ToArray();
        }
    }
    #endif

    public class LoadAssetsFromBundleOperation<AssetsT> : LoadAssetsOperation<AssetsT> where AssetsT : Object
    {

        public LoadAssetsFromBundleOperation(string bundleName, string loadingName) : base(bundleName, loadingName) { }


        public AssetBundle Bundle { get; protected set; }


        public override IEnumerator Execute()
        {
            yield return GetBundle();

            if (Bundle == null) yield break;

            //LoadingUI.SetLoadingLabel(string.Format("Loading {0}.", loadingName));

            //Load all the assets (Get the GameObjects if the AssetsT is a component)
            AssetBundleRequest loadAssetsOperation = typeIsComponent ?
                Bundle.LoadAllAssetsAsync<GameObject>() :
                Bundle.LoadAllAssetsAsync<AssetsT>();

            while (!loadAssetsOperation.isDone)
            {
                // Mostrar el progreso de carga
                yield return null;
            }

            //If the AssetsT is a component try to get each GameObject, then the desired asset type and return as an array
            //If not, return it as it is
            if (typeIsComponent)
            {
                List<AssetsT> components = new List<AssetsT>();
                foreach (GameObject gameObject in loadAssetsOperation.allAssets)
                {
                    AssetsT component = gameObject.GetComponent<AssetsT>();
                    if (component) components.Add(component);
                }
                Assets = components.ToArray();
            }
            else Assets = Array.ConvertAll(loadAssetsOperation.allAssets, item => (AssetsT)item);
        }

        IEnumerator GetBundle()
        {
            /*
            var loadBundleOperation = AssetBundleManager.LoadBundleAsync(bundleName);
            if (loadBundleOperation == null) yield break;

            while (!loadBundleOperation.isDone)
            {
                yield return null;
                LoadingUI.SetLoadingProgress(loadBundleOperation.progress);
            }
            Bundle = loadBundleOperation.assetBundle;
            */

            var loadBundleOperation = new GetOrLoadAssetBundleOperation(bundleName);
            yield return loadBundleOperation.Execute();
            Bundle = loadBundleOperation.GetAssetBundle;
        }
    }

    internal class GetOrLoadAssetBundleOperation : CustomYieldInstruction
    {
        string bundleName;

        AssetBundle assetBundle = null;
        public AssetBundle GetAssetBundle { get { return assetBundle; } }

        bool isDone = false;
        public override bool keepWaiting { get { return !isDone; } }

        public GetOrLoadAssetBundleOperation (string bundleName)
        {
            this.bundleName = bundleName;
        }

        public IEnumerator Execute ()
        {
            if (AssetBundleManager.TryToGetBundle(bundleName, out assetBundle))
            {
                isDone = true;
                yield break;
            }

            var loadOperation = AssetBundle.LoadFromFileAsync(Path.Combine(AssetBundleManager.assetBundlesPath, 
                bundleName));
            yield return loadOperation;

            assetBundle = loadOperation.assetBundle;
            AssetBundleManager.AddBundle(bundleName, assetBundle);
            isDone = true;
        }
    }
}