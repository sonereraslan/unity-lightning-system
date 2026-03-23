using System;
using System.Collections;
using UnityEngine;

namespace Lightning
{
    public enum LightningPhase { Idle, Progression, Peak, Regression }

    [RequireComponent(typeof(LightningRenderer))]
    public class LightningAnimator : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _progressionDuration = 0.15f;
        [SerializeField] private float _peakDuration = 0.05f;
        [SerializeField] private float _regressionDuration = 0.6f;

        [Header("Regression")]
        [SerializeField] private float _dampingCoefficient = 4f;
        [SerializeField] private float _flickerFrequency = 12f;

        public LightningPhase CurrentPhase { get; private set; } = LightningPhase.Idle;
        public event Action OnStrikeComplete;

        private LightningRenderer _renderer;
        private LightningStrike _currentStrike;
        private Coroutine _animationCoroutine;

        private void Awake()
        {
            _renderer = GetComponent<LightningRenderer>();
        }

        public void PlayStrike(LightningStrike strike)
        {
            _currentStrike = strike;
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateStrike());
        }

        public void Stop()
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            _renderer.ClearAll();
            CurrentPhase = LightningPhase.Idle;
        }

        private IEnumerator AnimateStrike()
        {
            // --- PROGRESSION PHASE ---
            CurrentPhase = LightningPhase.Progression;
            var mainBranch = _currentStrike.Branches[0];
            int totalSegments = mainBranch.Points.Count;

            float elapsed = 0f;
            while (elapsed < _progressionDuration)
            {
                elapsed += Time.deltaTime;
                int revealCount = Mathf.CeilToInt(
                    AnimationCurveHelper.ProgressionT(elapsed, _progressionDuration) * totalSegments
                );

                var partialPoints = mainBranch.Points.GetRange(0, Mathf.Max(2, revealCount));
                var partialBranch = new LightningBranch(partialPoints, mainBranch.Width, 0);
                var partialStrike = new LightningStrike(
                    _currentStrike.Origin, _currentStrike.Target,
                    new System.Collections.Generic.List<LightningBranch> { partialBranch }
                );
                _renderer.RenderStrike(partialStrike);
                yield return null;
            }

            // --- PEAK PHASE ---
            CurrentPhase = LightningPhase.Peak;
            _renderer.RenderStrike(_currentStrike);
            yield return new WaitForSeconds(_peakDuration);

            // --- REGRESSION PHASE ---
            CurrentPhase = LightningPhase.Regression;
            elapsed = 0f;
            while (elapsed < _regressionDuration)
            {
                elapsed += Time.deltaTime;
                float intensity = AnimationCurveHelper.DampedSine(
                    elapsed, _dampingCoefficient, _flickerFrequency
                );
                intensity = Mathf.Clamp01(Mathf.Abs(intensity));

                for (int i = 0; i < _currentStrike.Branches.Count; i++)
                    _renderer.SetBranchAlpha(i, intensity);

                yield return null;
            }

            // --- COMPLETE ---
            _renderer.ClearAll();
            CurrentPhase = LightningPhase.Idle;
            OnStrikeComplete?.Invoke();
        }
    }
}
