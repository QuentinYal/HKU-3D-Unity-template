
using UnityEngine;

public class ThirdPersonPlayer : MonoBehaviour
{
    [SerializeField] private ThirdPersonPlayerCharacter playerCharacter;
    [SerializeField] private ThirdPersonPlayerCamera playerCamera;
    [Space]
    [SerializeField] private ThirdPersonCameraSpring cameraSpring;
    [SerializeField] private ThirdPersonCameraLean cameraLean;
    [Space]
    [SerializeField] private Animator animator;
    [Space]

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

        bool isMoving = characterInput.Move.sqrMagnitude > 0.01f;

        var state = playerCharacter.GetState();
        bool isCrouching = state.Stance == Stance.Crouch;

        bool isSliding = state.Stance == Stance.Slide;

        animator.SetBool("Idle", !isMoving);

        if (isMoving)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Run", true);
        }

        if (!isMoving)
        {
            animator.SetBool("Idle", true);
            animator.SetBool("Run", false);
        }

        if (isCrouching)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Run", false);
            animator.SetBool("CrouchIdle", true);
        }

        if(isCrouching && isMoving)
        {
            animator.SetBool("CrouchIdle", false);
            animator.SetBool("CrouchWalk", true);
        }

        if (!isCrouching && !isMoving)
        {
            animator.SetBool("CrouchIdle", false);
            animator.SetBool("CrouchWalk", false);
            animator.SetBool("Idle", true);
        }

        if (isCrouching && !isMoving)
        {
            animator.SetBool("CrouchIdle", true);
            animator.SetBool("CrouchWalk", false);
        }

        if (!isCrouching && isMoving)
        {
            animator.SetBool("Run", true);
            animator.SetBool("CrouchWalk", false);
            animator.SetBool("Slide", false);
        }

        if (isSliding && isMoving)
        {
            animator.SetBool("Slide", true);
            animator.SetBool("Run", false);
        }

        if (!isSliding && isCrouching && !isMoving)
        {
            animator.SetBool("Run",false);
            animator.SetBool("Slide", false);
            animator.SetBool("CrouchIdle", true);
        }

        if (!isSliding && isCrouching && isMoving)
        {
            animator.SetBool("Run", false);
            animator.SetBool("Slide", false);
            animator.SetBool("CrouchWalk", true);
        }
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
