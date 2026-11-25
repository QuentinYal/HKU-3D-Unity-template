
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
            Attack = input.Attack.WasPressedThisFrame(),
            Crouch = input.Crouch.WasPressedThisFrame()
                        ? CrouchInput.Toggle
                        : CrouchInput.None,

            Emote1 = input.Emote1.WasPressedThisFrame(),
            Emote2 = input.Emote2.WasPressedThisFrame(),
            Emote3 = input.Emote3.WasPressedThisFrame()
        };
        playerCharacter.UpdateInput(characterInput);

        bool isWalking = characterInput.Move.sqrMagnitude > 0.001f;
        bool isRunning = characterInput.Move.sqrMagnitude > 0.1f;

        var state = playerCharacter.GetState();
        bool isCrouching = state.Stance == Stance.Crouch;

        bool isSliding = state.Stance == Stance.Slide;

        bool isJumping = characterInput.Jump;

        bool isAttacking = characterInput.Attack;

        bool isEmoting1 = characterInput.Emote1;
        bool isEmoting2 = characterInput.Emote2;
        bool isEmoting3 = characterInput.Emote3;

        animator.SetBool("Idle", !isWalking);

        if (isWalking)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Run", true);
        }

        if (!isWalking)
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

        if(isCrouching && isWalking)
        {
            animator.SetBool("CrouchIdle", false);
            animator.SetBool("CrouchWalk", true);
        }

        if (!isCrouching && !isWalking)
        {
            animator.SetBool("CrouchIdle", false);
            animator.SetBool("CrouchWalk", false);
            animator.SetBool("Idle", true);
        }

        if (isCrouching && !isWalking)
        {
            animator.SetBool("CrouchIdle", true);
            animator.SetBool("CrouchWalk", false);
        }

        if (!isCrouching && isWalking)
        {
            animator.SetBool("Run", true);
            animator.SetBool("CrouchWalk", false);
            animator.SetBool("Slide", false);
        }

        if (isSliding && isWalking)
        {
            animator.SetBool("Slide", true);
            animator.SetBool("Run", false);
        }

        if (!isSliding && isCrouching && !isWalking)
        {
            animator.SetBool("Run",false);
            animator.SetBool("Slide", false);
            animator.SetBool("CrouchIdle", true);
        }

        if (!isSliding && isCrouching && isWalking)
        {
            animator.SetBool("Run", false);
            animator.SetBool("Slide", false);
            animator.SetBool("CrouchWalk", true);
        }

        if (isJumping)
        {
            animator.SetTrigger("Jump");
        }

        if (isAttacking)
        {
            animator.SetTrigger("Hit");
        }

        if (isEmoting1)
        {
            animator.SetTrigger("Emote1");
        }

        if (isEmoting2)
        {
            animator.SetTrigger("Emote2");
        }

        if (isEmoting3)
        {
            animator.SetTrigger("Emote3");
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
