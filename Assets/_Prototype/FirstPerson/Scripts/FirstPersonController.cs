using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Basic")] public bool IsLockCursor;

    [Header("Reference")] public Camera fpsCam;
    public Transform fpsCrouch;
    public Transform LeanPivot;
    public CharacterController controller;

    [Header("Movement")] public float CrouchDst = 0.5f;
    public float CrouchSpeed = 2;
    public float WalkSpeed = 4;
    public float RunSpeed = 8;
    public float JumpForce = 8;
    public float Gravity = 18;

    [Tooltip("The sensitivity between input and moving"), Range(0f, 0.25f)]
    public float MoveSmoothTime = 0.1f;

    [Tooltip("The sensitivity between input and rotating"), Range(0f, 0.25f)]
    public float RotationSmoothTime = 0.05f;

    [Tooltip("The time for crouch animation"), Range(0.1f, 0.5f)]
    public float CrouchSmoothTime = 0.3f;

    [Range(10f, 45f)] public float LeanAngle = 30f;

    [Header("Camera")] public float MouseSensitivity = 10;
    public Vector2 PitchMinMax = new Vector2(-40, 85);

    [Space] [Header("Status")] float yaw;
    float pitch;
    float smoothYaw;
    float smoothPitch;
    float yawSmoothV;
    float pitchSmoothV;
    float verticalVelocity;
    Vector3 velocity;
    Vector3 smoothV;

    bool isJumping;
    bool isCrouching;
    bool isRunning;
    bool isRightLeaning;
    bool isLeftLeaning;
    float lastGroundedTime;

    void Start()
    {
        isCrouching = false;
        if (IsLockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        controller = GetComponent<CharacterController>();
        yaw = fpsCam.transform.localEulerAngles.y;
        pitch = this.transform.localEulerAngles.x;
        smoothYaw = yaw;
        smoothPitch = pitch;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            IsLockCursor = false;
        if (Input.GetKeyDown(KeyCode.Mouse0))
            IsLockCursor = true;
        
        if (Time.timeScale == 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            if (IsLockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
        }

        CalculateCamera();
        CalculatePosition();
        CheckCrouch();
        CheckLean();
    }

    void CalculateCamera()
    {
        // Camera
        float mX = Input.GetAxisRaw("Mouse X");
        float mY = Input.GetAxisRaw("Mouse Y");
        float mMag = Mathf.Sqrt(mX * mX + mY * mY);

        if (mMag > 5)
        {
            mX = 0;
            mY = 0;
        }

        yaw += mX * MouseSensitivity;
        pitch -= mY * MouseSensitivity;
        pitch = Mathf.Clamp(pitch, PitchMinMax.x, PitchMinMax.y);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchSmoothV, RotationSmoothTime);
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawSmoothV, RotationSmoothTime);

        fpsCam.transform.localEulerAngles = Vector3.right * smoothPitch;
        this.transform.localEulerAngles = Vector3.up * smoothYaw;
    }
    
    /*
     * 
     */
    void CalculatePosition()
    {
        var forward = Vector3.Cross(this.transform.right, Vector3.up).normalized;
        var right = Vector3.Cross(this.transform.forward, Vector3.up).normalized;
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 dir = -right * input.x + forward * input.y;

        isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = WalkSpeed;
        if (isCrouching && !isRunning)
            currentSpeed = CrouchSpeed;

        if (isRunning)
        {
            currentSpeed = RunSpeed;
            UnCrouch();
        }

        Vector3 targetVelocity = dir * currentSpeed;
        velocity = Vector3.SmoothDamp(velocity, targetVelocity, ref smoothV, MoveSmoothTime);

        verticalVelocity -= Gravity * Time.deltaTime;
        velocity = new Vector3(velocity.x, verticalVelocity, velocity.z);

        var flags = controller.Move(velocity * Time.deltaTime);
        if (flags == CollisionFlags.Below)
        {
            isJumping = false;
            lastGroundedTime = Time.time;
            verticalVelocity = 0;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            float timeSinceLastTouchedGround = Time.time - lastGroundedTime;
            UnCrouch();


            if (controller.isGrounded || (!isJumping && timeSinceLastTouchedGround < 0.15f))
            {
                isJumping = true;
                verticalVelocity = JumpForce;
            }
        }
    }

    void CheckCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!isCrouching)
            {
                fpsCrouch.transform.DOComplete();
                fpsCrouch.transform.DOLocalMoveY(fpsCrouch.transform.localPosition.y - CrouchDst, CrouchSmoothTime);
            }
            else
            {
                fpsCrouch.transform.DOComplete();
                fpsCrouch.transform.DOLocalMoveY(fpsCrouch.transform.localPosition.y + CrouchDst, CrouchSmoothTime);
            }

            isCrouching = !isCrouching;
        }
    }

    void UnCrouch()
    {
        if (isCrouching)
        {
            isCrouching = false;
            fpsCrouch.transform.DOComplete();
            fpsCrouch.transform.DOLocalMoveY(fpsCrouch.transform.localPosition.y + CrouchDst, CrouchSmoothTime);
        }
    }

    void CheckLean()
    {
        if (Input.GetKey(KeyCode.E) && !isRightLeaning && !isLeftLeaning)
        {
            isRightLeaning = true;
            LeanPivot.transform.DOComplete();
            LeanPivot.transform.DOLocalRotate(LeanPivot.transform.localEulerAngles - new Vector3(0, 0, LeanAngle),
                0.2f);
        }
        else if (Input.GetKeyUp(KeyCode.E) && isRightLeaning)
        {
            isRightLeaning = false;
            LeanPivot.transform.DOComplete();
            LeanPivot.transform.DOLocalRotate(LeanPivot.transform.localEulerAngles + new Vector3(0, 0, LeanAngle),
                0.2f);
        }

        if (Input.GetKey(KeyCode.Q) && !isLeftLeaning && !isRightLeaning)
        {
            isLeftLeaning = true;
            LeanPivot.transform.DOComplete();
            LeanPivot.transform.DOLocalRotate(LeanPivot.transform.localEulerAngles + new Vector3(0, 0, LeanAngle),
                0.2f);
        }
        else if (Input.GetKeyUp(KeyCode.Q) && isLeftLeaning)
        {
            isLeftLeaning = false;
            LeanPivot.transform.DOComplete();
            LeanPivot.transform.DOLocalRotate(LeanPivot.transform.localEulerAngles - new Vector3(0, 0, LeanAngle),
                0.2f);
        }
    }
}