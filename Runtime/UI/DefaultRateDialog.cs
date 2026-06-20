using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Drop-in default rate dialog with animated card and three action buttons.
    ///
    /// <b>Setup:</b> Run <c>Rate Control → Create Default Prefab</c> to generate the
    /// prefab with all references pre-wired. Then assign it to your <c>RateConfig</c> asset.
    ///
    /// <b>Text / Localization:</b> Labels are <b>not</b> set by this script — edit the
    /// <c>TextMeshProUGUI</c> components directly in the prefab, or attach your localization
    /// component (e.g. I2 Localization's <c>Localize</c>) to each label. This script will
    /// never overwrite what the prefab or localization system provides.
    ///
    /// <b>Animations:</b> Uses Unity Coroutines — no external animation library required.
    /// </summary>
    [AddComponentMenu("Rate Control/Rate Dialog (Default)")]
    public sealed class DefaultRateDialog : RateDialog
    {
        // ── Inspector refs ────────────────────────────────────────────────────────

        [Header("Layout References")]
        [Tooltip("Full-screen semi-transparent overlay placed behind the dialog card. Fades in/out during show and hide animations.")]
        [SerializeField] private CanvasGroup _backdrop;

        [Tooltip("RectTransform of the dialog card panel. Scales from 80% → 100% when showing and back when hiding.")]
        [SerializeField] private RectTransform _card;

        [Tooltip("CanvasGroup on the dialog card panel. Controls the card's alpha during fade-in/out. Must be on the same GameObject as the Card RectTransform.")]
        [FormerlySerializedAs("_cardGroup")]
        [SerializeField] private CanvasGroup _dialogGroup;

        [Header("Buttons")]
        [Tooltip("Button the player taps to open the store review page immediately.")]
        [SerializeField] private Button _rateNowButton;

        [Tooltip("Button the player taps to postpone the prompt (shown again after the configured reminder delay).")]
        [SerializeField] private Button _remindLaterButton;

        [Tooltip("Button the player taps to permanently decline the prompt.")]
        [SerializeField] private Button _noThanksButton;

        // ── Animation ─────────────────────────────────────────────────────────────

        [Header("Animation")]
        [Tooltip("Duration in seconds of the show (fade-in + scale-up) animation.")]
        [SerializeField] [Range(0.1f, 0.6f)] private float _showDuration = 0.28f;

        [Tooltip("Duration in seconds of the hide (fade-out + scale-down) animation.")]
        [SerializeField] [Range(0.1f, 0.4f)] private float _hideDuration = 0.18f;

        [Tooltip("Easing curve applied to the show animation (horizontal = time 0–1, vertical = value 0–1).")]
        [SerializeField] private AnimationCurve _showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("Easing curve applied to the hide animation.")]
        [SerializeField] private AnimationCurve _hideCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private Coroutine _anim;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rateNowButton?.onClick.AddListener(OnRateNow);
            _remindLaterButton?.onClick.AddListener(OnRemindLater);
            _noThanksButton?.onClick.AddListener(OnNoThanks);
        }

        // ── RateDialog ────────────────────────────────────────────────────────────

        public override void Show()
        {
            if (_anim != null) StopCoroutine(_anim);
            gameObject.SetActive(true);
            _anim = StartCoroutine(AnimateIn());
        }

        public override void Hide()
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateOut());
        }

        // ── Animation coroutines ──────────────────────────────────────────────────

        private IEnumerator AnimateIn()
        {
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.unscaledDeltaTime / _showDuration, 1f);
                float v = _showCurve.Evaluate(t);

                if (_backdrop     != null) _backdrop.alpha     = v;
                if (_dialogGroup  != null) _dialogGroup.alpha  = v;
                if (_card         != null) _card.localScale    = Vector3.LerpUnclamped(Vector3.one * 0.80f, Vector3.one, v);

                yield return null;
            }
            _anim = null;
        }

        private IEnumerator AnimateOut()
        {
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.unscaledDeltaTime / _hideDuration, 1f);
                float v = _hideCurve.Evaluate(t);

                if (_backdrop     != null) _backdrop.alpha     = v;
                if (_dialogGroup  != null) _dialogGroup.alpha  = v;
                if (_card         != null) _card.localScale    = Vector3.LerpUnclamped(Vector3.one, Vector3.one * 0.80f, t);

                yield return null;
            }
            gameObject.SetActive(false);
            _anim = null;
        }

    }
}

