using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class PlayerController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CinemachineVirtualCamera camera;
    [SerializeField] private CinemachinePOV cameraPov;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Renderer renderer;
    [SerializeField] private Animator anim;
    [SerializeField] private GameplayUI myUI;
    [SerializeField] private CharacterController controller;
    [SerializeField] private CinemachineVirtualCamera deathCam;
    [SerializeField] private FpsEnabler fpsSwitcher;

    [Header("Variables")]
    [SerializeField] private float movementSpeed = 10f;
    [SerializeField] private float velocitySmooth = 0.05f;
    [SerializeField] private float cameraSens = 1.0f;
    [SerializeField] private float cameraSensGamepad = 1.0f;
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private float timeBetweenShooting = 0.1f;
    [SyncVar(hook = nameof(OnChangeHealth)), SerializeField] private int currentHealth = 100;
    [SyncVar, SerializeField] private string playerName = "Player";
    [SyncVar(hook = nameof(OnChangeTeam)), SerializeField] private int teamNumber = 0;
    
    [SerializeField] private int minHealth = 0, maxHealth = 100;
    [SerializeField] private int damagePerShot;
    [SerializeField] private float respawnTime = 3.0f;

    [SyncVar, SerializeField] private Vector3 movementVector = new Vector3();
    [SyncVar, SerializeField] private Vector2 cameraRotation = new Vector2();
    [SyncVar, SerializeField] private Vector3 currentVelocity;
    [SerializeField] private bool isShooting = false;
    [SyncVar, SerializeField] private float currentShootingCooldown;
    [SyncVar, SerializeField] private Vector3 inputVector = new Vector3();

    private PlayerControllerInput input;
    public static string insertedName = "Player";

    public static PlayerController localPlayer;
    
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
        
        input = new PlayerControllerInput();
    }

    private void Start()
    {
        myUI = GameplayUI.Instance;
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cameraPov = (camera.GetCinemachineComponent(CinemachineCore.Stage.Aim) as CinemachinePOV);
        
        camera.enabled = true;

        EnableController();

        currentShootingCooldown = timeBetweenShooting;
        
        transform.position = GameManager.Instance.GetRandomSpawnPoint();

        CmdSetName(insertedName);

        localPlayer = this;
    }

    [Command]
    private void CmdSetName(string newName)
    {
        playerName = newName;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer)
        {
            deathCam.enabled = false;
            deathCam.Priority = -100;
            camera.enabled = false;
            camera.Priority = -100;
            fpsSwitcher.Switch(false);
        }
        else
        {
            fpsSwitcher.Switch(true);
        }
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
            //Debug.Log("There's no joystick.");
            input.Gamepad.Disable();
            input.Keyboard.Enable();
            
            input.Keyboard.Movement.performed += Move;
            input.Keyboard.Movement.canceled += Move;
            input.Keyboard.CameraRotation.performed += RotateCamera;
            input.Keyboard.CameraRotation.canceled += RotateCamera;
            input.Keyboard.Shoot.performed += ShootingAction;
            input.Keyboard.Shoot.canceled += ShootingAction;
            input.Keyboard.Pause.performed += PauseMenu;
        }
    }

    private void PauseMenu(InputAction.CallbackContext obj)
    {
        if (GameplayUI.Instance.ShowPauseMenu())
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            input.Keyboard.CameraRotation.Disable();
            input.Keyboard.Shoot.Disable();
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            input.Keyboard.CameraRotation.Enable();
            input.Keyboard.Shoot.Enable();
        }
    }

    private void ShootingAction(InputAction.CallbackContext callbackContext)
    {
        isShooting = !callbackContext.canceled;
    }

    private void RotateCamera(InputAction.CallbackContext obj)
    {
        cameraRotation = obj.ReadValue<Vector2>() * cameraSens;
    }
    
    private void RotateCameraGamepad(InputAction.CallbackContext obj)
    {
        cameraRotation = obj.ReadValue<Vector2>() * cameraSensGamepad;
    }

    private void Update()
    {
        if (isServer)
        {
            if (currentShootingCooldown < timeBetweenShooting)
            {
                currentShootingCooldown += Time.deltaTime;
            }
        }
        
        if (isLocalPlayer)
        {
            cameraPov.m_VerticalAxis.Value -= cameraRotation.y;
            camera.transform.eulerAngles =
                new Vector3(cameraPov.m_VerticalAxis.Value, camera.transform.eulerAngles.y, 0f);
            //cameraRotation = input.Gameplay.CameraRotation.ReadValue<Vector2>();
            transform.Rotate(0f, cameraRotation.x, 0f);
            
            movementVector = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.ClampMagnitude(inputVector, 1.0f);

            //controller.Move(movementVector * movementSpeed * Time.deltaTime);
            //TODO: I really don't want to use controller.SimpleMove, but right now I don't think there's any other way.
            controller.SimpleMove(movementVector * movementSpeed);

            if (isShooting)
            {
                CmdFire();
            }
        }
    }

    [Command]
    private void CmdFire()
    {
        Fire();
    }

    private void Fire()
    {
        if (currentShootingCooldown >= timeBetweenShooting)
        {
            currentShootingCooldown = 0.0f;
            RaycastHit hit;
            //Debug.Log($"{cameraPov.m_VerticalAxis.Value}");
            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            RaycastHit[] hits = new RaycastHit[16];
            var numberOfHits = Physics.RaycastNonAlloc(ray, hits);
            hits = Physics.RaycastAll(ray);
            
            hits = hits.OrderBy((h => h.distance)).ToArray();
            for (int i = 0; i < hits.Length; i++)
            {
                
                if (hits[i].transform.TryGetComponent<PlayerController>(out PlayerController player))
                {
                    if (player == this)
                    {
                        continue;
                    }
                    //Debug.Log("There IS a player!");
                    //Debug.Log($"This: {this.teamNumber}, other: {player.teamNumber}", gameObject);
                    //Debug.Log($"Are they equal? {player.teamNumber == this.teamNumber}");
                    if (player.teamNumber != this.teamNumber)
                    {
                        
                        player.DealDamage(damagePerShot, this);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    //Debug.Log($"Hit: {hits[i].transform.gameObject}");
                    break;
                }
            }
            /*
            if (Physics.Raycast(ray, out hit, Single.PositiveInfinity))
            {
                
            }*/
            //Debug.DrawRay(ray.origin, ray.direction * 5f, Color.green, 2.0f);
        }
    }

    private void DealDamage(int damage, PlayerController damager = null)
    {
        currentHealth -= damage;
        if (currentHealth <= minHealth)
        {
            Knockout(true);
            if (damager)
            {
                RpcDrawElimination(damager.playerName, this.playerName);
            }
            else
            {
                RpcDrawElimination(playerName, playerName);
            }
        }
    }

    [ClientRpc]
    private void RpcDrawElimination(string eliminater, string eliminated)
    {
        GameplayUI.Instance.DrawElimination(eliminater, eliminated);
    }

    private void Knockout(bool ranOnServer = false)
    {

        renderer.materials[0].SetColor("_EmissionColor", Color.gray/2);
        renderer.materials[1].SetColor("_EmissionColor", Color.gray/2);
        
        anim.SetTrigger("Knockout");
        mainCollider.enabled = false;
        if (hasAuthority)
        {
            deathCam.Priority = 20;
            fpsSwitcher.Switch(false);
            //rb.detectCollisions = false;
            //rb.isKinematic = true;
            myUI.DisplayKOVisual(true);
        }
        
        if(isServer && ranOnServer)
        {
            StartCoroutine(RespawnCoroutine());
        }
        enabled = false;
    }

    [Command]
    private void CmdRespawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);
        currentHealth = maxHealth;
        Debug.Log("Should spawn?");
        Respawn();
        RpcRespawn();
    }

    [ClientRpc]
    private void RpcRespawn()
    {
        //if(isLocalPlayer)
            
        if(!isServer)
            Respawn();
        else
        {
            Debug.Log("Is server.");
        }
    }

    private void Respawn()
    {
        if (isLocalPlayer)
        {
            transform.position = GameManager.Instance.GetRandomSpawnPoint();
            deathCam.Priority = 0;
            fpsSwitcher.Switch(true);
            myUI.DisplayKOVisual(false);
            //rb.detectCollisions = true;
            //rb.isKinematic = false;
        }

        //transform.position = GameManager.Instance.GetRandomSpawnPoint();
        Color temp = Color.red;
        if (!isServerOnly)
        {
            temp = (teamNumber == localPlayer.teamNumber) ? Color.green : Color.red;

            float intensity = Mathf.Pow(2, 2);
            temp *= intensity;

            renderer.materials[0].SetColor("_EmissionColor", temp);
            renderer.materials[1].SetColor("_EmissionColor", temp);
        }

        anim.SetTrigger("Respawn");
        mainCollider.enabled = true;
        //mainCollider.enabled = true;
        enabled = true;
    }

    private void Move(InputAction.CallbackContext obj)
    {
        var temp = obj.ReadValue<Vector2>();
        inputVector.Set(temp.x, 0.0f, temp.y);
        
        anim.SetFloat("MovementSpeed", inputVector.magnitude);
    }

    [ClientCallback]
    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            //Movement();
        }
    }

    [Command]
    private void CmdMovement()
    {
        Movement();
    }

    [SerializeField] private float velocityMagnitudeLimit = 10f;
    
    private void Movement()
    {
        //var temp = Vector3.ClampMagnitude(obj.ReadValue<Vector2>(), 1.0f);
        //inputVector.Set(temp.x, 0.0f, temp.y);
        movementVector = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.ClampMagnitude(inputVector, 1.0f);
        //movementVector.y = rb.velocity.y;
        var tempVelocity = rb.velocity;
        tempVelocity.y = 0.0f;

        Vector3 tempVec = Vector3.SmoothDamp(rb.velocity, movementVector * movementSpeed, ref currentVelocity,
            velocitySmooth);
        tempVec.y = rb.velocity.y;
        
        if (tempVelocity.magnitude <= velocityMagnitudeLimit)
        {
            rb.AddForce(movementVector * movementSpeed);
        }

        //rb.velocity = tempVec;

        //rb.MovePosition(rb.position + movementVector * movementSpeed * Time.fixedDeltaTime);
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void OnChangeHealth(int old, int val)
    {
        if(isLocalPlayer)
            GameplayUI.Instance.UpdateHealth(val);
            
        if(val <= minHealth)
            Knockout();
    }

    [SerializeField] private Vector3 movementForce = new Vector3(0, 0, 100);
    
    [ContextMenu("Force")]
    public void AddForce()
    {
        rb.AddForce(movementForce);
        //Debug.Log($"{Camera.FieldOfViewToFocalLength(camera.m_Lens.FieldOfView, camera.m_Lens.SensorSize.x)}");
    }

    [ContextMenu("Force death")]
    public void ForceDeath()
    {
        DealDamage(900);
    }

    public void SetTeam(int teamNumber)
    {
        this.teamNumber = teamNumber;
    }
    
    private void OnChangeTeam(int old, int number)
    {
        {
            StartCoroutine(SetColorCoroutine(number));
        }
    }

    private IEnumerator SetColorCoroutine(int number)
    {
        yield return new WaitUntil(() => localPlayer != null);
        //Debug.LogError($"this: {number}, local: {localPlayer.teamNumber}", gameObject);
        Color temp = Color.red;
        temp = (number == localPlayer.teamNumber) ? Color.green : Color.red;

        float intensity = Mathf.Pow(2, 2);
        temp *= intensity;
        
        renderer.materials[0].SetColor("_EmissionColor", temp);
        renderer.materials[1].SetColor("_EmissionColor", temp);
    }
}
