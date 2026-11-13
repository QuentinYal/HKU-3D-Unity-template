using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class Playercamera : MonoBehaviour
{
    [SerializeField] private float camSens = 400f;

    private Vector3 _eulerAngles;

    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.eulerAngles = _eulerAngles = target.eulerAngles;
    }

    public void UpdateRotation(CameraInput input)
    {
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * camSens * Time.deltaTime;
        transform.eulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
