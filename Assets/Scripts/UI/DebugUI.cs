using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class DebugUI : MonoBehaviour
{

    public TextMeshProUGUI debugText;
    public Slider timeSlider;

    SongManager manager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SubscribeToManagerDebug()
    {
        SongManager.OnDebugStart += CreateDebugUI;
    }
    static void CreateDebugUI(SongManager manager)
    {
        Instantiate(Resources.Load<DebugUI>("Debug UI")).manager = manager;
    }

    private void LateUpdate()
    {
        debugText.text = manager.GetDebugInfo();
    }

}
