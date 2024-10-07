using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public Transform playerBody;
    public float clampAngle = 80f;

    public bool isThirdPerson = false;
    public float thirdPersonDistance = 5f;
    public Vector3 thirdPersonOffset = new Vector3(0f, 2f, 0f);

    private float verticalRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Получаем входные данные мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Вращаем игрока вокруг оси Y
        playerBody.Rotate(Vector3.up * mouseX);

        // Вращаем камеру вокруг оси X
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Переключение между первым и третьим лицом
        if (Input.GetKeyDown(KeyCode.V))
        {
            isThirdPerson = !isThirdPerson;
        }

        // Устанавливаем позицию камеры
        if (isThirdPerson)
        {
            Vector3 desiredPosition = playerBody.position - transform.forward * thirdPersonDistance + thirdPersonOffset;
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = playerBody.position + thirdPersonOffset;
        }
    }
}