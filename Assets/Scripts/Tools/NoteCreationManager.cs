using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Catneep.NoteCreation
{

    [RequireComponent(typeof(AudioSource))]
    public class NoteCreationManager : MonoBehaviour
    {
        #region Teclas
        [Header("Teclas")]
        [SerializeField]
        private float margenNotaLarga = .5f;

        [SerializeField]
        KeyCode[] noteKeys = new KeyCode[] { KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P };

        private float[] timersNotas = new float[] { 0, 0, 0, 0, 0, 0 };
        private bool[] longFlags = new bool[] { false, false, false, false, false, false };
        #endregion

        #region Configuration
        [Header("Config")]
        [SerializeField]
        private GameObject noteGroupPrefab = null;
        [SerializeField]
        private Transform spawnNotesUnder = null;

        [SerializeField]
        private Sprite simpleSprite = null;
        [SerializeField]
        private Sprite longStartSprite = null;
        [SerializeField]
        private Sprite longMiddleSprite = null;
        [SerializeField]
        private Sprite longEndSprite = null;

        [SerializeField]
        private Button startButton = null;
        [SerializeField]
        private Text startButtonText = null;

        [SerializeField]
        private Text clipText = null;

        [SerializeField]
        private Text timerText = null;

        [SerializeField]
        private AudioSource audioSource = null;
        #endregion

        private NoteCreationGroup currentGroup = null;

        private List<NoteCreationGroup> notes = new List<NoteCreationGroup>();

        bool loseGroup = false;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (null == audioSource)
                audioSource = GetComponent<AudioSource>();
            else if (null != startButton)
                startButton.interactable = null != audioSource.clip;
            else
            {
                startButton = GameObject.Find("StartButton").GetComponent<Button>();
                startButtonText = startButton.GetComponentInChildren<Text>();
            }
        }
#endif

        private void Start()
        {
            clipText.text = audioSource.clip.name;
            timerText.text = "";
        }

        public void StartPlaying()
        {
            StartCoroutine(RecordRoutine());
            startButton.interactable = false;
        }

        private IEnumerator RecordRoutine()
        {
            float timer = 5;

            while (timer > 0)
            {
                startButtonText.text = "Mueve el bote en " + (int)timer;
                yield return new WaitForEndOfFrame();
                timer -= Time.deltaTime;
            }


            startButtonText.text =
                noteKeys[0].ToString() + " | " +
                noteKeys[1].ToString() + " | " +
                noteKeys[2].ToString() + " | " +
                noteKeys[3].ToString() + " | " +
                noteKeys[4].ToString() + " | " +
                noteKeys[5].ToString() + " | ";

            timer = 0;
            audioSource.Play();

            while (audioSource.isPlaying)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    audioSource.Stop();
                    break;
                }

                timer += Time.deltaTime;
                timerText.text = String.Format("{0:00}:{1:00}:{2:000}", timer / 60, timer % 60, (timer * 1000) % 1000);

                for (int i = 0; i < noteKeys.Length; i++)
                    CheckNote(noteKeys[i], timerText.text);

                if (loseGroup)
                {
                    loseGroup = false;
                    currentGroup = null;
                }

                yield return null;
            }

            // TODO: Aca podes agarrar la data guardada en "notes" y guardarla en un scriptable o algo.

        }

        private void CheckNote(KeyCode key, string timer)
        {
            int keyIndex = Array.IndexOf(noteKeys, key);

            if (Input.GetKeyDown(key))
            {
                GetNoteGroup(timer);
                currentGroup.UpdateNote(keyIndex, NoteType.Simple, simpleSprite);
            }

            if (Input.GetKey(key))
            {
                if (!longFlags[keyIndex])
                {
                    timersNotas[keyIndex] += Time.deltaTime;
                    if (timersNotas[keyIndex] >= margenNotaLarga)
                    {
                        currentGroup.UpdateNote(keyIndex, NoteType.LongStart, longStartSprite);
                        CheckForLongMiddles();
                        longFlags[keyIndex] = true;
                        timersNotas[keyIndex] = 0;
                        loseGroup = true;
                    }
                }
            }

            if (Input.GetKeyUp(key))
            {
                GetNoteGroup(timer);

                if (longFlags[keyIndex])
                {
                    currentGroup.UpdateNote(keyIndex, NoteType.LongEnd, longEndSprite);
                    longFlags[keyIndex] = false;
                }
                else
                    CheckForLongMiddles();

                loseGroup = true;
            }
        }

        private void CheckForLongMiddles()
        {
            for (int i = 0; i < longFlags.Length; i++)
            {
                if (longFlags[i])
                    currentGroup.UpdateNote(i, NoteType.LongMiddle, longMiddleSprite);
            }
        }

        private void GetNoteGroup(string timer)
        {
            if (null == currentGroup)
            {
                currentGroup = Instantiate(noteGroupPrefab, spawnNotesUnder).GetComponent<NoteCreationGroup>();
                currentGroup.UpdateNoteTime(timer);

                notes.Add(currentGroup);
            }
        }
    }
}
