using System.Collections.Generic;
using UnityEngine;

public class SongSelector : MonoBehaviour
{

    public SelectorButton selectorPrefab;
    // El transform del objeto "Content" tiene que estar asignado aca,
    // que está dentro del objeto scrollview
    public Transform buttonsParent;

    List<SelectorButton> selectorButtons = new List<SelectorButton>();

    public void Start()
    {
        // Obetenemos del GameManager una lista con las canciones.
        // Con el array de canciones que obtuvimos, agregamos instanciamos un botón por cada una
        foreach(var song in GameManager.GetSongs)
        {
            AddSongButton(song);
        }
    }
    void AddSongButton(Song song)
    {
        // Creamos una nueva variable newButton, instanciamos el botón a partir del prefab y
        // le asignamos el transform parent como parent del mismo
        SelectorButton newButton = Instantiate(selectorPrefab, buttonsParent);
        // Agregamos el botón a la lista, es probable que no haga falta
        selectorButtons.Add(newButton);

        // Llamamos el método del Selector para asignarle una canción
        newButton.SetSong(song);
    }

}
