using Mono.Cecil.Cil;
using UnityEngine;

public class ThirdPersonPlayer : MonoBehaviour
{
    [SerializeField] private ThirdPersonPlayerCharacter playerCharacter;
    [SerializeField] private ThirdPersonPlayerCamera playerCamera;
    [Space]
    [SerializeField] private ThirdPersonCameraSpring cameraSpring;
    [SerializeField] private ThirdPersonCameraLean cameraLean;

    public PlayerInputActions _inputActions;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        playerCharacter.Initialize();

        cameraSpring.Initialize();
        cameraLean.Initialize();
    }

    private void OnDestroy()
    {
        _inputActions.Dispose();
    }

    void Update()
    {
        var input = _inputActions.Gameplay;
        var deltaTime = Time.deltaTime;

        //get camera input and update its rotation
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };

        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = input.Crouch.WasPressedThisFrame()
                        ? CrouchInput.Toggle
                        : CrouchInput.None
        };
        playerCharacter.UpdateInput(characterInput);
    }

    private void LateUpdate()
    {
        var deltaTime = Time.deltaTime;

        playerCharacter.UpdateBody(deltaTime);

        var cameraTarget = playerCharacter.GetCameraTarget();
        var state = playerCharacter.GetState();

        cameraSpring.UpdateSpring(deltaTime, cameraTarget.position, cameraTarget.up);
        cameraLean.UpdateLean(deltaTime, state.Stance is Stance.Slide, state.Acceleration, cameraTarget.up);
    }
}
