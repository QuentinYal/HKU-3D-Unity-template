using UnityEngine;
using KinematicCharacterController;
using Unity.Mathematics;

public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch, Slide
}

public struct CharacterInput
{
    public Quaternion Rotation;

    public Vector2 Move;

    public bool Jump;

    public bool JumpSustain;

    public CrouchInput Crouch;

    public bool Attack;

    public bool Emote1;
    public bool Emote2;
    public bool Emote3;
}

public struct CharacterState
{
    public bool Grounded;

    public Stance Stance;

    public Vector3 Velocity;

    public Vector3 Acceleration;
}

public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;

    [SerializeField] private Transform root;

    [SerializeField] private Transform cameraTarget;
    [Space]

    [SerializeField] private float walkSpeed = 20f;
    [Space]

    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float airspeed = 15f;
    [SerializeField] private float airAcceleration = 70f;
    [Space]
    [SerializeField] private float JumpSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [Range(0f, 1f)]

    [SerializeField] private float jumpSustainGravity = 0.4f;

    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f;
    [SerializeField] private float slideEndSpeed = 15f;
    [SerializeField] private float slideFriction = 0.8f;
    [SerializeField] private float slideSteerAcceleration = 5f;
    [SerializeField] private float slideGravity = -90f;
    [Space]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;

    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]

    [SerializeField] private float crouchCameraTargetHeight = 0.7f;


    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;


    private Quaternion _requestedRotation;

    private Vector3 _requestedMovement;

    private bool _requestedJump;
    
    private bool _requestedSustainedJump;

    private bool _requestCrouch;

    private bool _requestCrouchInAir;

    private float _timeSinceUnderground;

    private float _timeSinceJumpRequested;

    private bool _ungroundedDueToJump;

    private Collider[] _uncrouchOverlapResults;
    public void Initialize()
    {
        _state.Stance = Stance.Stand;
        _lastState = _state;

        _uncrouchOverlapResults = new Collider[8];

        motor.CharacterController = this;
    }

    public void UpdateInput(CharacterInput input)
    {
        _requestedRotation = input.Rotation;
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = input.Rotation * _requestedMovement;

        var wasRequestingJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump;
        if(_requestedJump && !wasRequestingJump)
        {
            _timeSinceJumpRequested = 0f;
        }
        _requestedSustainedJump = input.JumpSustain;

        var wasRequestingCrouch = _requestCrouch;
        _requestCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestCrouch,
            CrouchInput.None => _requestCrouch,
            _=>_requestCrouch
        };
        if(_requestCrouch && !wasRequestingCrouch)
        {
            _requestCrouchInAir = !_state.Grounded;
        }
        else if(!_requestCrouch && wasRequestingCrouch)
        {
            _requestCrouchInAir = false;
        }
    }

    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;

        var cameraTargetHeight = currentHeight *
        (
            _state.Stance is Stance.Stand
                ? standCameraTargetHeight
                : crouchCameraTargetHeight
        );
        var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);

        cameraTarget.localPosition = Vector3.Lerp
        (
            a: cameraTarget.localPosition,
            b: new Vector3(0f, cameraTargetHeight, 0f),
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
        root.localScale = Vector3.Lerp
        (
            a: root.localScale,
            b: rootTargetScale,
            t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;
        //crouch
        if (_requestCrouch && _state.Stance is Stance.Stand)
        {
            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions
            (
                radius: motor.Capsule.radius,
                height: crouchHeight,
                yOffset: crouchHeight * 0.5f
            );
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        //Uncrouch
        if(!_requestCrouch && _state.Stance is not Stance.Stand)
        {
            // Tentatively "standup" the character capsule.
            motor.SetCapsuleDimensions
            (
                radius: motor.Capsule.radius,
                height: standHeight,
                yOffset: standHeight * 0.5f
            );

            // Then see if the capsule overlaps any colliders before actually
            // allowing the character to standup.
            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            if (motor.CharacterOverlap(pos, rot, _uncrouchOverlapResults, mask, QueryTriggerInteraction.Ignore) > 0)
            {
                //re-crouch.
                _requestCrouch = true;
                motor.SetCapsuleDimensions
                (
                    radius: motor.Capsule.radius,
                    height: crouchHeight,
                    yOffset: crouchHeight * 0.5f
                );
            }
            else
            {
                _state.Stance = Stance.Stand;
            }
        }

        // Update state to reflect relevant motor properties.
        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _state.Velocity = motor.Velocity;
        // And update the _lastState to store the character state snapshot taken at
        // the beginning of this character update.
        _lastState = _tempState;
    }

    public bool IsColliderValidForCollisions(Collider coll) => true;

    public void OnDiscreteCollisionDetected(Collider hitCollider){}

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport){}

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport){}

    public void PostGroundingUpdate(float deltaTime)
    {
        if(!motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide)
        {
            _state.Stance = Stance.Crouch;
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport){}

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // Update the character's rotation to face in the same direction as the
        // requested rotation (camera rotation).

        // We don't want the character to pitch up and down, so the direction the character
        // looks should always be "flattend."

        // This is done by projecting a vector pointing in the same direction that
        // the player is looking onto a flat ground plane.

        var forward = Vector3.ProjectOnPlane(_requestedRotation * Vector3.forward, motor.CharacterUp);

        if(forward != Vector3.zero)
        {
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;
        // if on the ground...
        if (motor.GroundingStatus.IsStableOnGround)
        {
            _timeSinceUnderground = 0f;
            _ungroundedDueToJump = false;

            // Snap the requested movement to the angle of the surface
            // the character is currently walking on.
            var groundMovement = motor.GetDirectionTangentToSurface(direction: _requestedMovement, surfaceNormal: motor.GroundingStatus.GroundNormal) * _requestedMovement.magnitude;

            // Start Sliding
            {
                var moving = groundMovement.sqrMagnitude > 0f;
                var crouching = _state.Stance is Stance.Crouch;
                var wasStanding = _lastState.Stance is Stance.Stand;
                var wasInAir = !_lastState.Grounded;
                if(moving && crouching && (wasStanding || wasInAir))
                {
                    _state.Stance = Stance.Slide;

                    // When landing on stable ground the character motor projects the velocity onto a flat ground plane.
                    // See: KinematicCharacterMotor.HandleVelocityProjection()
                    // This is good, because under normal cicumstances the player shouldn't slide when landing on the ground.
                    // in this case the player needs to slide
                    // Reproject the last frames (falling) velocity onto the ground
                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane
                        (
                            vector: _lastState.Velocity,
                            planeNormal: motor.GroundingStatus.GroundNormal
                        );
                    }


                    var effectiveSlideStartSpeed = slideStartSpeed;
                    if(!_lastState.Grounded && !_requestCrouchInAir)
                    {
                        effectiveSlideStartSpeed = 0f;
                        _requestCrouchInAir = false;
                    }

                    var slideSpeed = Mathf.Max(slideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = motor.GetDirectionTangentToSurface
                    (
                        direction: currentVelocity,
                        surfaceNormal: motor.GroundingStatus.GroundNormal
                    ) * slideSpeed;
                }
            }
            //move.
            if(_state.Stance is Stance.Stand or Stance.Crouch)
            {

                //calculate the speed and responsiveness of movement based
                // on the character's stance.
                var speed = _state.Stance is Stance.Stand
                    ? walkSpeed
                    : crouchSpeed;

                var response = _state.Stance is Stance.Stand
                    ? walkResponse
                    : crouchResponse;

                // and smoothly move along the ground in that direction
                var targetVelocity = groundMovement * speed; ;
                var moveVelocity = Vector3.Lerp
                (
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-response * deltaTime)
                );
                _state.Acceleration = moveVelocity - currentVelocity;

                currentVelocity = moveVelocity;
            }
            // continue sliding
            else
            {
                //friction
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);

                //Slope
                {
                    var force = Vector3.ProjectOnPlane
                    (
                        vector: -motor.CharacterUp,
                        planeNormal: motor.GroundingStatus.GroundNormal
                    ) * slideGravity;

                    currentVelocity -= force * deltaTime;
                }

                //steer
                {
                    // Target velocity is the player's movement direction, at the current speed
                    var currentSpeed = currentVelocity.magnitude;
                    var targetVelocity = groundMovement * currentSpeed;
                    var steerVelocity = currentVelocity;
                    var steerForce = (targetVelocity - steerVelocity) * slideSteerAcceleration * deltaTime;
                    // Add steer force, but clamp velocity so the slide speed doesn't increase due to direct movement input
                    steerVelocity += steerForce;
                    steerVelocity = Vector3.ClampMagnitude(steerVelocity, currentSpeed);

                    _state.Acceleration = (steerVelocity - currentVelocity) / deltaTime;

                    currentVelocity = steerVelocity;
                }

                //Stop
                if(currentVelocity.magnitude < slideEndSpeed)
                {
                    _state.Stance = Stance.Crouch;
                }
            }

        }
        // else in the air...
        else
        {
            _timeSinceUnderground += deltaTime;

            //move.
            if(_requestedMovement.sqrMagnitude > 0f)
            {
                //requested movement projected onto movement plane. (magnitude preserved)
                var planarMovement = Vector3.ProjectOnPlane
                (
                    vector: _requestedMovement,
                    planeNormal: motor.CharacterUp
                ) * _requestedMovement.magnitude;

                // Current velocity on movement plane.
                var currentPlanarVelocity = Vector3.ProjectOnPlane
                (
                    vector: currentVelocity,
                    planeNormal: motor.CharacterUp
                );

                //Calculate movement force.
                // Will be changend depending on current velocity
                var movementForce = planarMovement * airAcceleration * deltaTime;

                // If moving slower than the max air speed, treat movementForce as a simple steering force
                if(currentPlanarVelocity.magnitude < airspeed)
                {

                    //Add it to the current planar velocity for a target velocity
                    var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                    //Limit target velocity to air speed
                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airspeed);

                    // steer towards target velocity
                    movementForce = targetPlanarVelocity - currentPlanarVelocity;
                }
                // Otherwise nerf the movement force when it is in the direction of the cirrent planar velocity
                // to prevent accelerating further beyond the max air speed
                else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0)
                {
                    // Project movement force onto the plane whose normal is the current planar velocity
                    var constrainedMovementForce = Vector3.ProjectOnPlane
                    (
                        vector: movementForce,
                        planeNormal: currentPlanarVelocity.normalized
                    );

                    movementForce = constrainedMovementForce;
                }

                // Prevent air-climbing steep slopes
                if (motor.GroundingStatus.FoundAnyGround)
                {
                    // If moving in the same direction as the resultant velocity
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        // calculate obstruction normal
                        var obstructionNormal = Vector3.Cross
                        (
                            motor.CharacterUp,
                            Vector3.Cross
                            (
                                motor.CharacterUp,
                                motor.GroundingStatus.GroundNormal
                            )
                        ).normalized;

                        // Project movement force onto obstruction plane
                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                    }
                }

                currentVelocity += movementForce;
            }
            // gravity
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (_requestedSustainedJump && verticalSpeed > 0f)
            {
                effectiveGravity *= jumpSustainGravity;
            }
            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;
        }

        if (_requestedJump)
        {
            var grounded = motor.GroundingStatus.IsStableOnGround;
            var canCoyoteJump = _timeSinceUnderground < coyoteTime && !_ungroundedDueToJump;

            if (grounded || canCoyoteJump) 
            {
                _requestedJump = false; // Unset jump request
                _requestCrouch = false; // and request the character uncrouches
                _requestCrouchInAir = false;

                // Unstick the player from the ground
                motor.ForceUnground(time: 0f);
                _ungroundedDueToJump = true;

                // set minimum vertical speed to the jump speed
                var currenVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var tragetVerticalSpeed = Mathf.Max(currenVerticalSpeed, JumpSpeed);

                // Add the difference in current and target vertical speed to the character's velocity
                currentVelocity += motor.CharacterUp * (tragetVerticalSpeed - currenVerticalSpeed);
            }
            else
            {
                _timeSinceJumpRequested += deltaTime;

                // Defer the jump request until coyote time has passed.
                var canJumLater = _timeSinceJumpRequested < coyoteTime;
                _requestedJump = canJumLater;
            }
        }

    }

    public Transform GetCameraTarget() => cameraTarget;

    public CharacterState GetState() => _state;

    public CharacterState GetLastState() => _lastState;
}
