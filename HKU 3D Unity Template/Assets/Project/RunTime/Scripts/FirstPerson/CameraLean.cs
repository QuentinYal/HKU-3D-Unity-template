using System.Runtime.InteropServices;
using UnityEngine;

public class CameraLean : MonoBehaviour
{
    [SerializeField] private float attackDamping = 0.5f;
    [SerializeField] private float decayDamping = 0.3f;
    [SerializeField] private float walkStrength = 0.075f;
    [SerializeField] private float slideStrength = 0.2f;
    [SerializeField] private float strengthResponse = 5f;

    private Vector3 _dampedAccelertation;

    private Vector3 _dampedAccelerationVel;

    private float _smoothSrength;
    public void Initialize()
    {
        _smoothSrength = walkStrength;
    }

    public void UpdateLean(float deltaTime, bool sliding, Vector3 acceleration, Vector3 up)
    {
        var planarAcceleration = Vector3.ProjectOnPlane(acceleration, up);
        var damping = planarAcceleration.magnitude > _dampedAccelertation.magnitude
            ? attackDamping
            : decayDamping;

        _dampedAccelertation = Vector3.SmoothDamp
        (
            current: _dampedAccelertation,
            target: planarAcceleration,
            currentVelocity: ref _dampedAccelerationVel,
            smoothTime: damping,
            maxSpeed: float.PositiveInfinity,
            deltaTime: deltaTime
        );

        //Get the rotation axis based on the acceleration Vector.
        var leanAxis = Vector3.Cross(_dampedAccelertation.normalized, up).normalized;

        // reset rotation to that of its parent.
        transform.localRotation = Quaternion.identity;

        // Rotate around the lean axis.
        var targetStrength = sliding
            ? slideStrength
            : walkStrength;

        _smoothSrength = Mathf.Lerp(_smoothSrength, targetStrength, 1f - Mathf.Exp(-strengthResponse * deltaTime));

        transform.rotation = Quaternion.AngleAxis(-_dampedAccelertation.magnitude * _smoothSrength, leanAxis) * transform.rotation;
    }
}
