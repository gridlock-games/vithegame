using UnityEngine;

public class FluctuatingYRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("The base speed of the rotation in degrees per second.")]
    public float rotationSpeed = 100f;

    [Tooltip("How frequently (in seconds) fluctuations occur.")]
    public float fluctuationInterval = 2f;

    [Tooltip("The maximum angle of a fluctuation in degrees.")]
    public float fluctuationAmount = 5f;

    [Tooltip("The duration (in seconds) of each fluctuation.")]
    public float fluctuationDuration = 0.1f;

    [Header("Direction Settings")]
    [Tooltip("Set to '1' for positive rotation direction, '-1' for negative rotation.")]
    public int rotationDirection = 1;  // 1 for positive, -1 for negative

    private float _fluctuationTimer;
    private bool _isFluctuating;
    private float _fluctuationEndTime;
    private float _currentRotationY;

    private float _randomStartOffset;  // This will add randomness to each mesh's fluctuation timing

    void Start()
    {
        // Randomize the start offset so that each mesh starts at different times
        _randomStartOffset = Random.Range(0f, fluctuationInterval);
        _fluctuationTimer = _randomStartOffset;  // Apply random start offset
    }

    void Update()
    {
        // Update fluctuation timer with the random offset
        _fluctuationTimer += Time.deltaTime;

        // Determine if a fluctuation should occur
        if (!_isFluctuating && _fluctuationTimer >= fluctuationInterval)
        {
            _isFluctuating = true;
            _fluctuationEndTime = Time.time + fluctuationDuration;
            _fluctuationTimer = 0f;
        }

        // Calculate the base rotation
        float rotationThisFrame = rotationSpeed * Time.deltaTime * rotationDirection;

        if (_isFluctuating)
        {
            // Apply a random fluctuation to the rotation
            rotationThisFrame += Random.Range(-fluctuationAmount, fluctuationAmount);

            // End fluctuation if duration has passed
            if (Time.time >= _fluctuationEndTime)
            {
                _isFluctuating = false;
            }
        }

        // Update the current Y rotation
        _currentRotationY += rotationThisFrame;
        _currentRotationY %= 360; // Keep rotation within 0-360 degrees

        // Apply the rotation to the transform
        transform.rotation = Quaternion.Euler(0, _currentRotationY, 0);
    }
}
