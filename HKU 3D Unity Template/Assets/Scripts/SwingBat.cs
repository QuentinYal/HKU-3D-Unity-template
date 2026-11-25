using UnityEngine;

public class SwingBat : MonoBehaviour
{
    public Rigidbody batRb;
    public Transform swingTarget;

    bool swinging;

    public Transform hip; 

    Vector3 restLocalPos;
    Quaternion restLocalRot;
    public void StartSwing()
    {
        restLocalPos = hip.InverseTransformPoint(batRb.position);
        restLocalRot = Quaternion.Inverse(hip.rotation) * batRb.rotation;

        swinging = true;
        batRb.isKinematic = true;
    }

    public void EndSwing()
    {
        Debug.Log("EndSwing called");
        swinging = false;
        batRb.isKinematic = false;

        batRb.position = hip.TransformPoint(restLocalPos);
        batRb.rotation = hip.rotation * restLocalRot;
        batRb.linearVelocity = Vector3.zero;   
        batRb.angularVelocity = Vector3.zero;
        batRb.WakeUp();
    }

    void Start()
    {
        restLocalPos = hip.InverseTransformPoint(batRb.position);
        restLocalRot = Quaternion.Inverse(hip.rotation) * batRb.rotation;
    }
    void Update()
    {
        if (!swinging) return;
        batRb.MovePosition(swingTarget.position);
        batRb.MoveRotation(swingTarget.rotation);
    }
}
