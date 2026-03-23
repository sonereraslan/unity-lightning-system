// Assets/Scripts/Lightning/LightningSystem.cs
using UnityEngine;

namespace Lightning
{
    [RequireComponent(typeof(LightningRenderer))]
    [RequireComponent(typeof(LightningAnimator))]
    [RequireComponent(typeof(LightningLightController))]
    [RequireComponent(typeof(AudioSource))]
    public class LightningSystem : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private int _segmentCount = 60;
        [SerializeField] private float _maxDeviation = 0.4f;
        [SerializeField] private float _branchProbability = 0.25f;
        [SerializeField] private int _maxBranchDepth = 2;
        [SerializeField] private bool _useRandomSeed = true;
        [SerializeField] private int _fixedSeed = 42;

        [Header("Strike Points")]
        [SerializeField] private Transform _originPoint;
        [SerializeField] private Transform _targetPoint;

        [Header("Sound")]
        [SerializeField] private AudioClip[] _thunderClips;
        [SerializeField][Range(0f, 1f)] private float _volume = 1f;

        private LightningGenerator _generator;
        private LightningAnimator _animator;
        private LightningLightController _lightController;
        private AudioSource _audioSource;

        private void Awake()
        {
            _animator = GetComponent<LightningAnimator>();
            _lightController = GetComponent<LightningLightController>();
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            RebuildGenerator();
        }

        private void RebuildGenerator()
        {
            int seed = _useRandomSeed ? -1 : _fixedSeed;
            _generator = new LightningGenerator(
                _segmentCount, _maxDeviation, _branchProbability, _maxBranchDepth, seed
            );
        }

        /// <summary>
        /// Trigger a lightning strike between the configured origin and target.
        /// </summary>
        [ContextMenu("Trigger Strike")]
        public void TriggerStrike()
        {
            if (_originPoint == null || _targetPoint == null)
            {
                Debug.LogError("[LightningSystem] Origin or Target point is not assigned.", this);
                return;
            }

            RebuildGenerator();
            var strike = _generator.Generate(_originPoint.position, _targetPoint.position);
            _animator.PlayStrike(strike);
            _lightController.TriggerStrike(_targetPoint.position, totalDuration: 0.8f);
            PlayRandomThunder();
        }

        /// <summary>
        /// Trigger a strike between arbitrary world positions.
        /// </summary>
        public void TriggerStrike(Vector3 origin, Vector3 target)
        {
            RebuildGenerator();
            var strike = _generator.Generate(origin, target);
            _animator.PlayStrike(strike);
            _lightController.TriggerStrike(target, totalDuration: 0.8f);
            PlayRandomThunder();
        }

        private void PlayRandomThunder()
        {
            if (_thunderClips == null || _thunderClips.Length == 0) return;
            var clip = _thunderClips[Random.Range(0, _thunderClips.Length)];
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, _volume);
        }
    }
}
