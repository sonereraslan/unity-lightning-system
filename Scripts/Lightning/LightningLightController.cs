using System.Collections;
using UnityEngine;

namespace Lightning
{
    public class LightningLightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Light _strikeLight;

        [Header("Light Settings")]
        [SerializeField] private float _peakIntensity = 8f;
        [SerializeField] private float _lightRange = 30f;
        [SerializeField] private Color _lightColor = new Color(0.8f, 0.9f, 1.0f);

        [Header("Camera Shake")]
        [SerializeField] private bool _enableCameraShake = true;
        [SerializeField] private float _shakeIntensity = 0.15f;
        [SerializeField] private float _shakeDuration = 0.2f;

        private Coroutine _lightCoroutine;
        private Coroutine _shakeCoroutine;
        private Vector3 _cameraOriginPosition;

        private void Awake()
        {
            if (_strikeLight == null)
            {
                var go = new GameObject("LightningStrikeLight");
                go.transform.SetParent(transform);
                _strikeLight = go.AddComponent<Light>();
            }

            _strikeLight.type = LightType.Point;
            _strikeLight.color = _lightColor;
            _strikeLight.range = _lightRange;
            _strikeLight.intensity = 0f;
            _strikeLight.enabled = false;
        }

        public void TriggerStrike(Vector3 strikePosition, float totalDuration)
        {
            _strikeLight.transform.position = strikePosition;

            if (_lightCoroutine != null)
                StopCoroutine(_lightCoroutine);
            _lightCoroutine = StartCoroutine(AnimateLight(totalDuration));

            if (_enableCameraShake && Camera.main != null)
            {
                if (_shakeCoroutine != null)
                {
                    StopCoroutine(_shakeCoroutine);
                    _shakeCoroutine = null;
                    if (Camera.main != null)
                        Camera.main.transform.localPosition = _cameraOriginPosition;
                }
                _shakeCoroutine = StartCoroutine(ShakeCamera());
            }
        }

        public void Stop()
        {
            if (_lightCoroutine != null)
            {
                StopCoroutine(_lightCoroutine);
                _lightCoroutine = null;
            }
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
                if (Camera.main != null)
                    Camera.main.transform.localPosition = _cameraOriginPosition;
            }
            _strikeLight.enabled = false;
            _strikeLight.intensity = 0f;
        }

        private IEnumerator AnimateLight(float totalDuration)
        {
            _strikeLight.enabled = true;
            _strikeLight.intensity = _peakIntensity;

            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                float flicker = AnimationCurveHelper.DampedSine(elapsed, 3f, 10f);
                _strikeLight.intensity = _peakIntensity * Mathf.Clamp01(Mathf.Abs(flicker));
                yield return null;
            }

            _strikeLight.intensity = 0f;
            _strikeLight.enabled = false;
            _lightCoroutine = null;
        }

        private IEnumerator ShakeCamera()
        {
            var cam = Camera.main.transform;
            _cameraOriginPosition = cam.localPosition;
            float elapsed = 0f;

            while (elapsed < _shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / _shakeDuration);
                cam.localPosition = _cameraOriginPosition + Random.insideUnitSphere * _shakeIntensity * t;
                yield return null;
            }

            cam.localPosition = _cameraOriginPosition;
            _shakeCoroutine = null;
        }
    }
}
