using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Catneep.Songs
{

    [CreateAssetMenu(fileName = defaultName, menuName = Song.menuPath + "Song List", order = -1)]
    public class SongList : ScriptableObject, IEnumerable<Song>
    {

        public const string defaultName = "Song List";

        private static SongList singleton;
        public static SongList Singleton { get { return singleton; } }

        public static IEnumerator LoadAsyncSingleton()
        {
            if (singleton != null) yield break;

            ResourceRequest loadRequest = Resources.LoadAsync<SongList>(defaultName);
            yield return loadRequest;

            singleton = (SongList)loadRequest.asset;
        }

        [SerializeField]
        private Song[] songs;
        public Song[] SongListCopy { get { return (Song[])songs.Clone(); } }

        public Song this[int index] { get { return songs[index]; } }
        public int Length { get { return songs.Length; } }


        IEnumerator<Song> IEnumerable<Song>.GetEnumerator()
        {
            return (IEnumerator<Song>)songs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return songs.GetEnumerator();
        }

    }
}