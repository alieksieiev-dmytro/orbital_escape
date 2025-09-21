using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CircularMovement2DInput : MonoBehaviour
{
    [SerializeField] private Transform _centerPoint;
    [SerializeField] private float _baseRadius = 3f;
    [SerializeField] private float _angularSpeed = 2f;
    [SerializeField] private bool _faceCenter = true;
    [SerializeField] private InputActionReference _clockwiseAction;
    [SerializeField] private InputActionReference _counterclockwiseAction;
    [SerializeField] private InputActionReference _toggleRadiusAction;

    [Header("Toggle Radius")]
    [SerializeField] private float _deltaRadius = 1f;
    [SerializeField] private bool _smoothToggle = false;
    [SerializeField] private float _toggleTime = 0.12f;

    private float _angle;
    private int _direction = 1;
    private float _currentRadius;
    private bool _isExpanded;
    private Coroutine _toggleRoutine;

    private void Awake()
    {
        _currentRadius = Mathf.Max(0f, _baseRadius);
    }

    private void OnEnable()
    {
        if (_clockwiseAction != null)
        {
            _clockwiseAction.action.Enable();
            _clockwiseAction.action.performed += OnClockwise;
        }
        if (_counterclockwiseAction != null)
        {
            _counterclockwiseAction.action.Enable();
            _counterclockwiseAction.action.performed += OnCounterclockwise;
        }
        if (_toggleRadiusAction != null)
        {
            _toggleRadiusAction.action.Enable();
            _toggleRadiusAction.action.performed += OnToggleRadius;
        }
    }

    private void OnDisable()
    {
        if (_clockwiseAction != null)
        {
            _clockwiseAction.action.performed -= OnClockwise;
            _clockwiseAction.action.Disable();
        }
        if (_counterclockwiseAction != null)
        {
            _counterclockwiseAction.action.performed -= OnCounterclockwise;
            _counterclockwiseAction.action.Disable();
        }
        if (_toggleRadiusAction != null)
        {
            _toggleRadiusAction.action.performed -= OnToggleRadius;
            _toggleRadiusAction.action.Disable();
        }
    }

    private void Update()
    {
        _angle += _direction * _angularSpeed * Time.deltaTime;

        float x = Mathf.Cos(_angle) * _currentRadius;
        float y = Mathf.Sin(_angle) * _currentRadius;
        transform.position = _centerPoint.position + new Vector3(x, y, 0f);

        if (_faceCenter)
        {
            Vector2 dir = (_centerPoint.position - transform.position).normalized;
            float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rot - 90f);
        }
    }

    private void OnClockwise(InputAction.CallbackContext _)
    {
        _direction = -1;
    }

    private void OnCounterclockwise(InputAction.CallbackContext _)
    {
        _direction = 1;
    }

    private void OnToggleRadius(InputAction.CallbackContext _)
    {
        _isExpanded = !_isExpanded;
        float target = Mathf.Max(0f, _baseRadius + (_isExpanded ? _deltaRadius : 0f));

        if (!_smoothToggle || _toggleTime <= 0f)
        {
            _currentRadius = target;
            return;
        }

        if (_toggleRoutine != null) StopCoroutine(_toggleRoutine);
        _toggleRoutine = StartCoroutine(TweenRadius(_currentRadius, target, _toggleTime));
    }

    private IEnumerator TweenRadius(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = t / time;
            _currentRadius = Mathf.Lerp(from, to, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        _currentRadius = to;
        _toggleRoutine = null;
    }

    public void SetBaseRadius(float value)
    {
        _baseRadius = Mathf.Max(0f, value);
        float target = Mathf.Max(0f, _baseRadius + (_isExpanded ? _deltaRadius : 0f));
        _currentRadius = target;
    }

    public void SetAngularSpeed(float value)
    {
        _angularSpeed = Mathf.Max(0f, value);
    }

    public void SetDirectionClockwise()
    {
        _direction = -1;
    }

    public void SetDirectionCounterclockwise()
    {
        _direction = 1;
    }
}
