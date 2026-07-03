using System;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Inspector'da tanımlı sesleri yönetir.
    ///
    /// Kullanım:
    ///   SoundManager.instance.Play("Click");
    ///   SoundManager.instance.PlayOneShot("Coin");
    ///   SoundManager.instance.Stop("Music");
    ///   SoundManager.instance.SetEnabled(true);
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public Sound[] sounds;

        #region Singleton
        public static SoundManager instance = null;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
                Construct();
            }
            else
            {
                DestroyImmediate(this);
            }
        }
        #endregion

        // ── Setup ─────────────────────────────────────────────────────────────────────

        public void Construct()
        {
            if (sounds == null) return;

            foreach (Sound s in sounds)
            {
                if (s == null || s.clip == null) continue;

                s.source = gameObject.AddComponent<AudioSource>();
                s.source.clip        = s.clip;
                s.source.volume      = s.volume;
                s.source.pitch       = s.pitch;
                s.source.loop        = s.loop;
                s.source.playOnAwake = s.playOnAwake;

                if (s.playOnAwake)
                    s.source.Play();
            }

            // İlk açılışta DataManager'dan ayarları uygula (yoksa default = açık).
            bool soundOn = DataManager.instance == null || DataManager.instance.sound;
            SetEnabled(soundOn);
        }

        // ── Play / Stop ───────────────────────────────────────────────────────────────

        public void Play(string name)
        {
            Sound s = FindSound(name);
            if (s == null || s.source == null) return;
            s.source.Play();
        }

        /// <summary>Üst üste çalınabilen SFX için (Click, Coin gibi).</summary>
        public void PlayOneShot(string name, float volumeScale = 1f)
        {
            Sound s = FindSound(name);
            if (s == null || s.source == null || s.clip == null) return;
            s.source.PlayOneShot(s.clip, volumeScale);
        }

        public void Stop(string name)
        {
            Sound s = FindSound(name);
            if (s == null || s.source == null) return;
            s.source.Stop();
        }

        public bool IsPlaying(string name)
        {
            Sound s = FindSound(name);
            return s != null && s.source != null && s.source.isPlaying;
        }

        // ── Enable / Disable ──────────────────────────────────────────────────────────

        /// <summary>Tüm sesleri aç/kapat.</summary>
        public void SetEnabled(bool enabled)
        {
            if (sounds == null) return;
            foreach (Sound s in sounds)
            {
                if (s == null || s.source == null) continue;
                s.source.volume = enabled ? s.volume : 0f;
            }
        }

        /// <summary>Eski API uyumluluğu — parametre artık "açık mı" anlamında.</summary>
        public void AllSound(bool enabled) => SetEnabled(enabled);

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private Sound FindSound(string name)
        {
            if (sounds == null || string.IsNullOrEmpty(name)) return null;
            Sound s = Array.Find(sounds, c => c != null && c.name == name);
            if (s == null)
                Debug.LogWarning($"[SoundManager] Sound \"{name}\" not found.", this);
            return s;
        }
    }

    [Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;

        [Range(0f, 1f)]  public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;

        public bool loop;
        public bool playOnAwake;

        [HideInInspector] public AudioSource source;
    }
}
