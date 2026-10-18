using System;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    /// <summary>Bounded entrance/confirmation/exit; no perpetual transform motion.</summary>
    public sealed class ModularRoomMenuMotion : MonoBehaviour
    {
        [Serializable]
        public sealed class Piece
        {
            public RectTransform rect;
            public CanvasGroup opacity;
            public float delay;
            [NonSerialized] public Vector3 rest;
        }
        public Piece[] pieces;
        public CanvasGroup realChoice, virtualChoice;
        [Header("Replace these Source Images with final icons")]
        public UnityEngine.UI.Image realRoomImage, virtualRoomImage;
        private float age;
        private int phase;
        private bool virtualSelected, initialized;
        public bool ExitComplete { get; private set; }
        private void Cache()
        {
            if (initialized) return;
            foreach (var piece in pieces) piece.rest = piece.rect.localPosition;
            initialized = true;
        }
        public void Reveal()
        {
            Cache(); age = 0; phase = 1; ExitComplete = false; enabled = true;
            foreach (var piece in pieces) { piece.opacity.alpha = 0; piece.rect.localPosition = piece.rest + Vector3.down * 10; }
        }
        public void Confirm(bool useVirtual)
        {
            Cache(); age = 0; phase = 2; virtualSelected = useVirtual; enabled = true;
        }
        // Called when the next screen is ready; until then room switching stays accessible.
        public void Exit()
        {
            Cache(); age = 0; phase = 3; ExitComplete = false; enabled = true;
        }
        private void Update()
        {
            age += Time.unscaledDeltaTime;
            if (phase == 1)
            {
                foreach (var piece in pieces)
                {
                    float t = Mathf.Clamp01((age - piece.delay) / .38f);
                    float eased = 1 - Mathf.Pow(1 - t, 3);
                    piece.rect.localPosition = piece.rest + Vector3.down * (10 * (1 - eased));
                    piece.opacity.alpha = eased;
                }
                if (age >= .65f) { phase = 0; enabled = false; }
            }
            else if (phase == 2)
            {
                var other = virtualSelected ? realChoice : virtualChoice;
                other.alpha = 1 - .55f * Mathf.Sin(Mathf.Clamp01(age / .65f) * Mathf.PI);
                if (age >= .65f) { other.alpha = 1; phase = 0; enabled = false; }
            }
            else if (phase == 3)
            {
                foreach (var piece in pieces)
                {
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - piece.delay * .4f) / .22f));
                    piece.opacity.alpha = 1 - t;
                    piece.rect.localPosition = piece.rest + Vector3.down * (6 * t);
                }
                if (age >= .4f) { ExitComplete = true; phase = 0; enabled = false; }
            }
            else enabled = false;
        }
    }
}

