/**
 * 
 * Script de editor que nos permite agregar las opciones del juego como el modo debug o simular assetbundles.
 * 
 * Nos permite hacer una "simulación de carga de assets" con AssetBundlesManager
 * en lugar de tener que construirlos de nuevo cada vez que los actualicemos.
 * 
 * Simplemente activando o desactivando la opción Assets>Simulate Asset Bundles
 * 
 */

using UnityEditor;
using Game.AssetBundles;

public static class CatneepMenu
{

    const string rootMenu = "Catneep"; 

    // Debug Mode
    const string debugMenuName = rootMenu + "/Debug Mode";

    [MenuItem(debugMenuName, false, 200)]
    private static void ToggleDebug()
    {
        GameManager.EditorDebugMode = !GameManager.EditorDebugMode;
    }
    [MenuItem(debugMenuName, true)]
    private static bool ToggleDebugValidate()
    {
        Menu.SetChecked(debugMenuName, GameManager.EditorDebugMode);
        return true;
    }


    // Asset Bundles
    const string simulationMenuName = rootMenu + "/Simulate Asset Bundles";

    [MenuItem(simulationMenuName, false, 200)]
    private static void ToggleSimulation()
    {
        AssetBundleManager.SimulateAssetBundleEditor = !AssetBundleManager.SimulateAssetBundleEditor;
    }
    [MenuItem(simulationMenuName, true)]
    private static bool ToggleSimulationValidate()
    {
        Menu.SetChecked(simulationMenuName, AssetBundleManager.SimulateAssetBundleEditor);
        return true;
    }

}
