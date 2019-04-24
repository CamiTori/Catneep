using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;


namespace Catneep.Neeps
{
    public class NeepEffectManager : MonoBehaviour
    {

        public const int maxConsumableNeeps = 6;

        public const int neepsForHardDifficulty = 4;


        private static NeepEffectManager singleton;
        public static NeepEffectManager Singleton { get { return singleton; } }


        [Header("References")]

        [SerializeField]
        private NotesUI notesUI;
        public NotesUI UI { get { return notesUI; } }

        private SongManagerNoteInput manager;
        public SongManagerNoteInput SongManager { get { return manager; } }

        private AudioListener listener;
        public AudioListener AudioListener { get { return listener; } }

        private AudioMixer mixer;
        public AudioMixer EffectMixer { get { return mixer; } }


        [Header("Effects")]

        [Range(0, maxConsumableNeeps)]
        [SerializeField]
        private int consumedNeeps = 0;
        public int GetConsumedNeeps { get { return consumedNeeps; } }
        public Difficulty GetDifficulty
        {
            get
            {
                return consumedNeeps < neepsForHardDifficulty ? Difficulty.Easy : Difficulty.Hard;
            }
        }

        [SerializeField]
        private float neepScoreMultiplierBonus = 0.5f;


        private enum EffectCycleMode { RoundRobin, Random, RandomCycle }
        [Space]
        [SerializeField]
        private EffectCycleMode effectCycleMode;

        [SerializeField]
        private NeepEffect preEffect;

        [SerializeField]
        private NeepEffect[] effects = new NeepEffect[0];

        private int activeEffectIndex = 0;
        public NeepEffect CurrentEffect { get { return activeEffectIndex >= 0 ? effects[activeEffectIndex] : null; } }

        public IEnumerable<NeepEffect> AllEffects
        {
            get
            {
                yield return preEffect;

                foreach (NeepEffect effect in effects)
                {
                    yield return effect;
                }
            }
        }

        [Space]

        [SerializeField]
        private float baseEffectInterval = 20f;
        [SerializeField]
        private float intervalDecreaseRate = 0.1f;

        private float effectInterval;

        [Space]

        [SerializeField]
        private float baseEffectDuration = 10f;
        [SerializeField]
        private float durationGrowthRate = 0.5f;

        private float effectDuration;

        private bool DoEffects { get { return consumedNeeps > 0; } }


        private bool postInitialized = false;
        private float currentTime;

        private readonly Queue<int> effectIndexQueue = new Queue<int>();


        private enum EffectState
        {
            NoEffect, PreEffect, OnEffect
        }
        private EffectState currentState = EffectState.NoEffect;

        private float lastEffectTime = 0f;
        private bool TimeHasPassed(float currentTime, float timeDifference)
        {
            return currentTime > lastEffectTime + timeDifference;
        }

        private void Awake()
        {
            if (singleton == null)
            {
                singleton = this;
            }
            else if (singleton != this)
            {
                DestroyImmediate(this);
                return;
            }

            DOTween.Init();
        }

        private void Start()
        {
            // Subscribe to the manager update event
            manager = notesUI.SongManager;
            manager.OnTimeUpdateEvent += OnTimeUpdate;
            manager.AdditionalDebugInfo += DebugInfo;

            manager.SetDifficulty(GetDifficulty, consumedNeeps * neepScoreMultiplierBonus + 1);

            // Get the audio listener and the audio mixer
            listener = FindObjectOfType<AudioListener>();
            mixer = manager.GetAudioSource.outputAudioMixerGroup.audioMixer;

            // Initialize all the effects
            foreach (NeepEffect effect in AllEffects)
            {
                effect.Initialize(this);
            }

            // Set the effect times
            effectInterval = baseEffectInterval / Mathf.Exp((consumedNeeps - 1) * intervalDecreaseRate);
            effectDuration = baseEffectDuration * Mathf.Exp((consumedNeeps - 1) * durationGrowthRate);
        }

        private void LateUpdate()
        {
            if (!postInitialized) PostInitialize();
        }

        private void PostInitialize()
        {
            foreach (NeepEffect effect in AllEffects)
            {
                effect.PostInitialize();
            }

            postInitialized = true;
        }


        private void OnTimeUpdate()
        {
            if (!DoEffects) return;

            currentTime = manager.CurrentSongPosition;

            switch (currentState)
            {
                case EffectState.NoEffect:
                    if (TimeHasPassed(currentTime, effectInterval))
                    {
                        currentState = EffectState.OnEffect;
                        lastEffectTime = currentTime;

                        ActivateNextEffect();
                    }
                    break;
                case EffectState.PreEffect:
                    break;
                case EffectState.OnEffect:
                    if (TimeHasPassed(currentTime, effectDuration))
                    {
                        activeEffectIndex = -1;
                        lastEffectTime = currentTime;
                        currentState = EffectState.NoEffect;
                    }
                    break;
            }
        }

        private void ActivateNextEffect()
        {
            switch (effectCycleMode)
            {
                case EffectCycleMode.RoundRobin:
                    ActivateEffect((activeEffectIndex + 1) % effects.Length);
                    break;
                case EffectCycleMode.Random:
                    ActivateEffect(Random.Range(0, effects.Length));
                    break;
                case EffectCycleMode.RandomCycle:
                    if (effectIndexQueue.Count <= 0)
                    {
                        foreach (int i in Enumerable.Range(0, effects.Length)
                            .OrderBy(i => Random.Range(int.MinValue, int.MaxValue)))
                        {
                            effectIndexQueue.Enqueue(i);
                        }
                        //Debug.Log(string.Join(" -> ", effectIndexQueue.Select(i => i.ToString()).ToArray()));
                    }
                    ActivateEffect(effectIndexQueue.Dequeue());
                    break;
            }
        }

        private void ActivateEffect(int index)
        {
            if (index < 0 || index >= effects.Length) return;

            activeEffectIndex = index;
            effects[index].StartEffect(effectDuration, currentTime, () => manager.CurrentSongPosition);
        }


        private IEnumerable<string> DebugInfo()
        {
            yield return null;

            yield return "Consumed Neeps: " + consumedNeeps;
            if (DoEffects)
            {
                yield return string.Format("Effect interval: {0:0.##} s", effectInterval);
                yield return string.Format("Effect duration: {0:0.##} s", effectDuration);
            }
        }

    }
}
