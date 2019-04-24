using UnityEngine;
using UnityEngine.UI;


public class SelectorButton : MonoBehaviour
{

    public Text title;

    Song song;

    public void SetSong(Song song)
    {
        this.song = song;
        title.text = song.Title;
    }

    public void SelectSong()
    {
        // Le decimos al GameManager que comience con la canción que le asignamos a este botón
        GameManager.StartSong(song);
    }


}
