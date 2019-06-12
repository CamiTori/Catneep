using UnityEngine;


public class SongBackground : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer.sprite = SongManager.CurrentSong.BackgroundImage;
    }
}
