using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Drop-in default rate dialog with animated card, star decoration, and three action buttons.
    ///
    /// <b>Setup:</b> Run <c>Rate Control → Create Default Prefab</c> to generate the
    /// prefab with all references pre-wired. Then assign it to <see cref="RateControlSetup"/>.
    ///
    /// <b>Customization:</b> All colors, text, and timing values are Inspector-exposed.
    /// Swap fonts, sprites, or override <c>Show</c>/<c>Hide</c> in a subclass for full control.
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

        [Header("Labels")]
        [Tooltip("TextMeshPro label for the dialog title (e.g. 'Enjoying the game?').")]
        [SerializeField] private TextMeshProUGUI _titleLabel;

        [Tooltip("TextMeshPro label for the dialog subtitle / call-to-action line.")]
        [SerializeField] private TextMeshProUGUI _subtitleLabel;

        [Header("Buttons")]
        [Tooltip("Button the player taps to open the store review page immediately.")]
        [SerializeField] private Button _rateNowButton;

        [Tooltip("Button the player taps to postpone the prompt (shown again after the configured reminder delay).")]
        [SerializeField] private Button _remindLaterButton;

        [Tooltip("Button the player taps to permanently decline the prompt.")]
        [SerializeField] private Button _noThanksButton;

        // ── Content ───────────────────────────────────────────────────────────────

        [Header("Default Text")]
        [Tooltip("Title shown at the top of the dialog card.")]
        [SerializeField] private string _title = "Enjoying the game?";

        [Tooltip("Subtitle shown below the title.")]
        [SerializeField] private string _subtitle = "A quick review means the world to us!";

        [Tooltip("Label text for the 'Rate Now' action button.")]
        [SerializeField] private string _rateLabel = "Rate Now  ★";

        [Tooltip("Label text for the 'Remind Me Later' button.")]
        [SerializeField] private string _remindLabel = "Remind Me Later";

        [Tooltip("Label text for the 'No Thanks' dismiss button.")]
        [SerializeField] private string _noThanksLabel = "No Thanks";

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
            ApplyText(_titleLabel,   _title);
            ApplyText(_subtitleLabel, _subtitle);
            ApplyButtonLabel(_rateNowButton,    _rateLabel);
            ApplyButtonLabel(_remindLaterButton, _remindLabel);
            ApplyButtonLabel(_noThanksButton,   _noThanksLabel);

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

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void ApplyText(TextMeshProUGUI label, string text)
        {
            if (label != null) label.text = text;
        }

        private static void ApplyButtonLabel(Button button, string text)
        {
            if (button == null) return;
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }
    }
}

