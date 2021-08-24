using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CinemachineVirtualCamera camera;
    [SerializeField] private CinemachinePOV cameraPov;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Renderer renderer;
    [SerializeField] private Animator anim;
    [SerializeField] private GameplayUI myUI;

    [Header("Variables")]
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float velocitySmooth = 0.05f;
    [SerializeField] private float cameraSens = 1.0f;
    [SerializeField] private float cameraSensGamepad = 1.0f;
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private float timeBetweenShooting = 0.1f;
    [SerializeField] private int currentHealth = 100, minHealth = 0, maxHealth = 100;
    [SerializeField] private int damagePerShot;
    [SerializeField] private float respawnTime = 3.0f;

    private Vector3 inputVector = new Vector3();
    private Vector3 movementVector = new Vector3();
    private Vector2 cameraRotation = new Vector2();
    private PlayerControllerInput input;
    private Vector3 currentVelocity;
    private bool isShooting = false;
    private float currentShootingCooldown;

    private void Awake()
    {
        if (!rb)
            rb = GetComponent<Rigidbody>();
        if (!camera)
            camera = GetComponentInChildren<CinemachineVirtualCamera>();
        if (!mainCollider)
            mainCollider = GetComponent<Collider>();
        if (!renderer)
            renderer = GetComponent<MeshRenderer>();
        if (!anim)
            anim = GetComponentInChildren<Animator>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cameraPov = (camera.GetCinemachineComponent(CinemachineCore.Stage.Aim) as CinemachinePOV);
        
        input = new PlayerControllerInput();
        
        EnableController();

        currentShootingCooldown = timeBetweenShooting;
    }

    private void EnableController()
    {
        Gamepad gamepad = null;
        if (playerNumber <= Gamepad.all.Count)
        {
            gamepad = Gamepad.all[playerNumber - 1];
            InputUser myUser = InputUser.CreateUserWithoutPairedDevices();
            myUser = InputUser.PerformPairingWithDevice(gamepad, myUser);
            myUser.AssociateActionsWithUser(input);
            input.Keyboard.Disable();
            input.Gamepad.Enable();
            
            input.Gamepad.Movement.performed += Move;
            input.Gamepad.Movement.canceled += Move;
            input.Gamepad.CameraRotation.performed += RotateCameraGamepad;
            input.Gamepad.CameraRotation.canceled += RotateCameraGamepad;
            input.Gamepad.Shoot.performed += ShootingAction;
            input.Gamepad.Shoot.canceled += ShootingAction;
        }
        else
        {
            Debug.Log("There's no joystick.");
            input.Gamepad.Disable();
            input.Keyboard.Enable();
            
            input.Keyboard.Movement.performed += Move;
            input.Keyboard.Movement.canceled += Move;
            input.Keyboard.CameraRotation.performed += RotateCamera;
            input.Keyboard.CameraRotation.canceled += RotateCamera;
            input.Keyboard.Shoot.performed += ShootingAction;
            input.Keyboard.Shoot.canceled += ShootingAction;
        }
    }

    private void ShootingAction(InputAction.CallbackContext callbackContext)
    {
        isShooting = !callbackContext.canceled;
    }

    private void RotateCamera(InputAction.CallbackContext obj)
    {
        cameraRotation = obj.ReadValue<Vector2>() * cameraSens;
        
        //cameraPov.m_VerticalAxis.Value -= obj.ReadValue<Vector2>().y * cameraSens;
        //transform.Rotate(0f, obj.ReadValue<Vector2>().x * cameraSens, 0f);
        
        //inputVector = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * inputVector;
    }
    
    private void RotateCameraGamepad(InputAction.CallbackContext obj)
    {
        cameraRotation = obj.ReadValue<Vector2>() * cameraSensGamepad;
        
        //cameraPov.m_VerticalAxis.Value -= obj.ReadValue<Vector2>().y * cameraSens;
        //transform.Rotate(0f, obj.ReadValue<Vector2>().x * cameraSens, 0f);
        
        //inputVector = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * inputVector;
    }

    private void Update()
    {
        //cameraRotation = input.Gameplay.CameraRotation.ReadValue<Vector2>();
        cameraPov.m_VerticalAxis.Value -= cameraRotation.y;
        transform.Rotate(0f, cameraRotation.x, 0f);
        camera.transform.eulerAngles = new Vector3(cameraPov.m_VerticalAxis.Value, camera.transform.eulerAngles.y, 0f);

        if (currentShootingCooldown < timeBetweenShooting)
        {
            currentShootingCooldown += Time.deltaTime;
        }
        
        if (isShooting && currentShootingCooldown >= timeBetweenShooting)
        {
            Fire();
        }
    }

    private void Fire()
    {
        currentShootingCooldown = 0.0f;
        RaycastHit hit;
        //Debug.Log($"{cameraPov.m_VerticalAxis.Value}");
        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        if (Physics.Raycast(ray, out hit,
            Single.PositiveInfinity))
        {
            Debug.Log($"Hit: {hit.transform.gameObject}");
            if (hit.transform.TryGetComponent<PlayerController>(out PlayerController player))
            {
                player.DealDamage(damagePerShot);
            }
        }
        
        Debug.DrawRay(ray.origin, ray.direction * 5f, Color.green, 2.0f);
    }

    private void DealDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= minHealth)
        {
            Knockout();
        }
    }

    private void Knockout()
    {
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        renderer.material.color = Color.red;
        mainCollider.enabled = false;
        rb.detectCollisions = false;
        rb.isKinematic = true;
        enabled = false;
        myUI.DisplayKOVisual(true);
        yield return new WaitForSeconds(respawnTime);
        myUI.DisplayKOVisual(false);
        Respawn();
    }

    private void Respawn()
    {
        transform.position = GameManager.Instance.GetRandomSpawnPoint();
        renderer.material.color = Color.white;
        mainCollider.enabled = true;
        rb.detectCollisions = true;
        rb.isKinematic = false;
        currentHealth = maxHealth;
        enabled = true;
    }

    private void Move(InputAction.CallbackContext obj)
    {
        var temp = obj.ReadValue<Vector2>();
        inputVector.Set(temp.x, 0.0f, temp.y);
        
        anim.SetFloat("MovementSpeed", (currentVelocity).magnitude);
    }

    private void FixedUpdate()
    {
        movementVector = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * inputVector;
        //movementVector.z = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * inputVector.z;
        
        rb.velocity = Vector3.SmoothDamp(rb.velocity, movementVector * movementSpeed, ref currentVelocity, velocitySmooth);
        //anim.SetFloat("MovementSpeed", (currentVelocity).magnitude);
        
        //rb.velocity = rb.velocity + (inputVector * Time.fixedDeltaTime * movementSpeed);
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }
}
