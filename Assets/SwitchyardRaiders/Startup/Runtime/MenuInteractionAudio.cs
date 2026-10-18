using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // Lives above the panels, so hiding a button cannot cut off its sound.
    public sealed class MenuInteractionAudio : MonoBehaviour
    {
        public AudioSource hoverSource, clickSource, transitionSource;
        public AudioClip hoverClip, confirmClip, backClip, beginClip, menuTransitionClip;
        [Tooltip("Imported for the future game countdown; not played by room selection.")]
        public AudioClip countdownClip;
        private float nextHover, nextClick, nextTransition;

        public void Hover()
        {
            if (Time.unscaledTime < nextHover) return;
            nextHover = Time.unscaledTime + .09f;
            Play(hoverSource, hoverClip);
        }
        public void Click(AudioClip clip)
        {
            if (Time.unscaledTime < nextClick) return;
            nextClick = Time.unscaledTime + .05f;
            if (hoverSource) hoverSource.Stop();
            Play(clickSource, clip);
        }
        public void MenuOpened()
        {
            if (Time.unscaledTime < nextTransition) return;
            nextTransition = Time.unscaledTime + .2f;
            Play(transitionSource, menuTransitionClip);
        }
        private static void Play(AudioSource source, AudioClip clip)
        {
            if (!source || !clip || !source.isActiveAndEnabled) return;
            // One voice per channel bounds overlap, including rapid two-hand hovering.
            source.clip = clip;
            source.Play();
        }
    }
}
