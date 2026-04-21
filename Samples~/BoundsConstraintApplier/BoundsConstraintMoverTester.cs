using UnityEngine;

namespace Calluna.UI.BoundsConstraintApplierSample
{
    public class BoundsConstraintMoverTester : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _speed = 1f;
        [SerializeField] private float _range = 300f;

        private Vector2 _origin;

        private void Start()
        {
            if (_target != null)
                _origin = _target.anchoredPosition;
        }

        private void Update()
        {
            if (_target == null)
                return;

            float t = Time.time * _speed;
            _target.anchoredPosition = _origin + new Vector2(
                Mathf.Sin(t) * _range,
                Mathf.Cos(t * 0.7f) * _range);
        }
    }
}
