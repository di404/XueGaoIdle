using UnityEngine;

namespace XueGao
{
    public class GameAudio : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip bgmLoop;
        [SerializeField] private AudioClip buyClip;
        [SerializeField] private AudioClip focusClip;
        [SerializeField] private AudioClip biteClip;
        [SerializeField] private AudioClip completeClip;
        [SerializeField] private AudioClip prizeWinClip;
        [SerializeField] private AudioClip prizeLoseClip;
        [SerializeField] private AudioClip upgradeClip;
        [SerializeField] private AudioClip continueClip;
        [SerializeField] private AudioClip invalidClip;

        [Header("Volumes")]
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 0.32f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.8f;

        private void Awake()
        {
            ResolveSources();
            ConfigureSources();
        }

        public void PlayBgm()
        {
            if (bgmSource == null || bgmLoop == null)
            {
                return;
            }

            bgmSource.clip = bgmLoop;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }

        public void PlayBuy()
        {
            PlayOneShot(buyClip);
        }

        public void PlayFocus()
        {
            PlayOneShot(focusClip);
        }

        public void PlayBite()
        {
            PlayOneShot(biteClip);
        }

        public void PlayComplete()
        {
            PlayOneShot(completeClip);
        }

        public void PlayPrize(bool isWin)
        {
            PlayOneShot(isWin ? prizeWinClip : prizeLoseClip);
        }

        public void PlayUpgrade()
        {
            PlayOneShot(upgradeClip);
        }

        public void PlayContinue()
        {
            PlayOneShot(continueClip);
        }

        public void PlayInvalid()
        {
            PlayOneShot(invalidClip);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (sfxSource == null || clip == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        private void ResolveSources()
        {
            if (bgmSource != null && sfxSource != null)
            {
                return;
            }

            AudioSource[] sources = GetComponents<AudioSource>();
            if (bgmSource == null && sources.Length > 0)
            {
                bgmSource = sources[0];
            }

            if (sfxSource == null)
            {
                sfxSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
            }
        }

        private void ConfigureSources()
        {
            ConfigureSource(bgmSource, bgmVolume, true);
            ConfigureSource(sfxSource, sfxVolume, false);
        }

        private static void ConfigureSource(AudioSource source, float volume, bool loop)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = 0f;
        }
    }
}
