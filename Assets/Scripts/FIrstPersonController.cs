using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class FirstPersonController : MonoBehaviour
{
    public bool canMove { get; private set; } = true;
    private bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    private bool shouldJump => Input.GetKey(jumpKey) && cc.isGrounded;
    private bool shouldCrouch => cc.isGrounded;

    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canUseHeadBob = true;
    [SerializeField] private bool canSlide = true;

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftAlt;

    [Header("Movement Parameters")]
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float slopeSpeed = 8f;

    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80f;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2.5f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool isCrouching;
    private bool duringCrouchAnimation;
    private Coroutine crouchRoutine;
    private bool startedCrouch;
    [Header("Headbob Parameters")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed= 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    private float defaultYPos = 0;
    private float timer;


    // SLIDING PARAMETERS
    private Vector3 hitPointNormal;

    private bool IsSliding
    {
        get
        {
            if (cc.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > cc.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }


    private Camera playerCamera;
    private CharacterController cc;

    private Vector3 moveDir;
    private Vector2 currentInput;

    private float rotX = 0;

    // Start is called before the first frame update
    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        cc = GetComponent<CharacterController>();
        defaultYPos = playerCamera.transform.localPosition.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    // Update is called once per frame
    void Update()
    {
        if(startedCrouch && Physics.Raycast(playerCamera.transform.position, Vector3.up, 2f))
        {
            startedCrouch = false;
            canCrouch = false;
            
            print("Hitting Head");
        }
        if (!startedCrouch && !Physics.Raycast(playerCamera.transform.position, Vector3.up, 2f))
        {
            startedCrouch = true;
            canCrouch = true;
            crouchRoutine = StartCoroutine(CrouchStand(false));
        }


        if (canMove)
        {
            HandleMovementInput();
            HandleMouseLook();

            if (canJump)
                HandleJump();

            if (canCrouch)
            {
                HandleCrouch();
            }
                
            if (canUseHeadBob)
                HandleHeadbob();

            ApplyFinalMovements();
        }
    }

    private void HandleMovementInput()
    {
        currentInput = new Vector2((isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxisRaw("Vertical"), (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxisRaw("Horizontal"));

        float moveDirY = moveDir.y;
        moveDir = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDir = moveDir.normalized * Mathf.Clamp(moveDir.magnitude, 0, (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed));
        moveDir.y = moveDirY;
    }

    private void HandleMouseLook()
    {
        rotX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotX = Mathf.Clamp(rotX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotX, 0, 0);

        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    private void HandleJump()
    {
        if (shouldJump)
        {
            moveDir.y = jumpForce;
        }
    }

    private void HandleCrouch()
    {
        if (shouldCrouch && Input.GetKeyDown(crouchKey))
        {
            startedCrouch = true;
            if (crouchRoutine != null)
            {
                StopCoroutine(crouchRoutine);
                crouchRoutine = null;
            }
            crouchRoutine = StartCoroutine(CrouchStand(true));
            
        }

        if (shouldCrouch && Input.GetKeyUp(crouchKey))
        {
            startedCrouch = false;
            if (crouchRoutine != null)
            {
                StopCoroutine(crouchRoutine);
                crouchRoutine = null;
            }
            crouchRoutine = StartCoroutine(CrouchStand(false));
        }
    }
    
    private void HandleHeadbob()
    {
        if (!cc.isGrounded) return;

        if (Mathf.Abs(moveDir.x) > 0.1f || Mathf.Abs(moveDir.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : IsSprinting ? sprintBobSpeed : walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3
                (playerCamera.transform.localPosition.x, 
                defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
                playerCamera.transform.localPosition.z);
        }
    }

    private void ApplyFinalMovements()
    {
        if (!cc.isGrounded)
        {
            moveDir.y -= gravity * Time.deltaTime;
        }

        if (canSlide && IsSliding)
            moveDir += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;

        cc.Move(moveDir * Time.deltaTime);
    }

    private IEnumerator CrouchStand(bool isEnter)
    {
        duringCrouchAnimation = true;
        isCrouching = isEnter;

        float timeElapsedCrouch = 0;
        float targetHeight = isEnter ?  crouchHeight : standingHeight;
        float currentHeight = cc.height;
        Vector3 targetCenter = isEnter ? crouchingCenter : standingCenter;
        Vector3 currentCenter = cc.center;

        while(timeElapsedCrouch < timeToCrouch)
        {
            cc.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsedCrouch / timeToCrouch);
            cc.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsedCrouch / timeToCrouch);
            timeElapsedCrouch += Time.deltaTime;
            yield return null;
            
        }

        cc.height = targetHeight;
        cc.center = targetCenter;

        isCrouching = isEnter;

        crouchRoutine = null;

        duringCrouchAnimation = false;
    }
}
