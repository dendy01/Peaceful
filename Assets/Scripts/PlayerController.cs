using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpForce = 7f;
    public float gravity = -9.81f;
    public float groundCheckDistance = 0.4f;
    public GameObject playerModel;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private CameraController cameraController;
    private Animator animator;
    private float currentSpeed = 0f;
    private Coroutine speedChangeCoroutine;
    private bool isRunning = false;
    private bool isJumping = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraController = GetComponentInChildren<CameraController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            if (isJumping)
            {
                isJumping = false;
                animator.SetBool("IsJumping", false);
            }
        }

        // Movement
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        // Check for running
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Calculate target speed for animation
        float targetSpeed = new Vector2(x, z).magnitude;

        // Adjust speed based on whether the player is running
        float currentMoveSpeed = isRunning ? runSpeed : moveSpeed;

        // Start or update speed change coroutine
        if (speedChangeCoroutine != null)
            StopCoroutine(speedChangeCoroutine);
        speedChangeCoroutine = StartCoroutine(ChangeSpeed(targetSpeed));

        controller.Move(move * currentMoveSpeed * Time.deltaTime);

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            isJumping = true;
            animator.SetTrigger("Jump");
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Toggle player model visibility
        if (playerModel != null)
        {
            playerModel.SetActive(cameraController.isThirdPerson);
        }
    }

    private IEnumerator ChangeSpeed(float target)
    {
        while (Mathf.Abs(currentSpeed - target) > 0.01f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, Time.deltaTime * 5f);
            animator.SetFloat("Speed", currentSpeed);
            animator.SetBool("IsRunning", isRunning);
            yield return null;
        }
        currentSpeed = target;
        animator.SetFloat("Speed", currentSpeed);
        animator.SetBool("IsRunning", isRunning);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize ground check
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}
