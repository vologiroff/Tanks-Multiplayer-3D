using UnityEngine;
using System;
using TanksMP;
using System.Collections;

public class TankMovement : MonoBehaviour
{
    public int m_PlayerNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
    public float m_Speed = 13f;                 // How fast the tank moves forward and back.
    public float m_TurnSpeed = 40f;            // How fast the tank turns in degrees per second.
    public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
    public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
    public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
    public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

    private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
    private string m_TurnAxisName;              // The name of the input axis for turning.
    private Rigidbody m_Rigidbody;              // Reference used to move the tank.

    private float m_MovementInputValue;
    private float m_MovementInputValue1;         // The current value of the movement input.
    private float m_TurnInputValue1;               // The current value of the turn input.
    private float m_OriginalPitch; 
    private bool back;           // The pitch of the audio source at the start of the scene.
    private TankShooting tankShoot;
    private TankHealth tankHealth;
    public PowerupHealthSingle powerupHealthSingle;
    private GameManager gameManager;

    protected Joystick joystick;
    protected BackButton backButton; 
    

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        tankShoot = GetComponent<TankShooting>();
        tankHealth = GetComponent<TankHealth>();
        gameManager = FindObjectOfType<GameManager>();
    }


    private void OnEnable()
    {
        // When the tank is turned on, make sure it's not kinematic.
        m_Rigidbody.isKinematic = false;
        back = false;
        // Also reset the input values.
        m_MovementInputValue1 = 0f;
        m_TurnInputValue1 = 0f;
    }


    private void OnDisable()
    {
        // When the tank is turned off, set it to kinematic so it stops moving.
        m_Rigidbody.isKinematic = true;
    }


    private void Start()
    {
        // The axes names are based on player number.
        //m_MovementAxisName = "Vertical" + m_PlayerNumber;
        //m_TurnAxisName = "Horizontal" + m_PlayerNumber;
        // Store the original pitch of the audio source.
        m_OriginalPitch = m_MovementAudio.pitch;
        joystick = FindObjectOfType<Joystick>();
        backButton = FindObjectOfType<BackButton>();
    }


    private void Update()
    {
        // Store the value of both input axes.
        //m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
        //m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
        m_MovementInputValue1 = joystick.Vertical;
        m_TurnInputValue1 = joystick.Horizontal;
        EngineAudio();
    }


    private void EngineAudio()
    {
        // If there is no input (the tank is stationary)...
        if (Mathf.Abs(m_MovementInputValue1) < 0.1f && Mathf.Abs(m_TurnInputValue1) < 0.1f)
        {
            // ... and if the audio source is currently playing the driving clip...
            if (m_MovementAudio.clip == m_EngineDriving)
            {
                // ... change the clip to idling and play it.
                m_MovementAudio.clip = m_EngineIdling;
                m_MovementAudio.pitch = UnityEngine.Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
        else
        {
            // Otherwise if the tank is moving and if the idling clip is currently playing...
            if (m_MovementAudio.clip == m_EngineIdling)
            {
                // ... change the clip to driving and play.
                m_MovementAudio.clip = m_EngineDriving;
                m_MovementAudio.pitch = UnityEngine.Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
    }


    private void FixedUpdate()
    {
    	//Debug.Log(backButton.back);
    	if(backButton.changed) {
    		backButton.changed = false;
    		back = !back;
    	}
        // Adjust the rigidbodies position and orientation in FixedUpdate.
        if(m_MovementInputValue1 != 0 && m_TurnInputValue1 != 0) {
     		m_MovementInputValue = m_MovementInputValue1;
     		if(Math.Abs(m_TurnInputValue1) > Math.Abs(m_MovementInputValue))
     			m_MovementInputValue = m_TurnInputValue1;
     		if(Math.Abs(m_MovementInputValue) <= 0.5)
     			m_MovementInputValue = 0;
        	Turn();	
        	Move();
        }
    }


    private void Move()
    {
    	Vector3 movement = transform.forward * Math.Abs(m_MovementInputValue) * m_Speed * Time.deltaTime;
        // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
        if(back) {
        	m_Speed = 5f;
        	tankShoot.m_MinLaunchForce = 10f;
        	m_Rigidbody.MovePosition(m_Rigidbody.position - movement);
        } else {
        	m_Speed = 13f;
        	tankShoot.m_MinLaunchForce = 15f;
        	m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }
        // Apply this movement to the rigidbody's position.
        //controller.Move (transform.forward * Time.deltaTime * m_Speed);
        
    }


    private void Turn()
    {
    	float x = m_TurnInputValue1 / 64;
        float y = m_MovementInputValue1 / 64;
        transform.rotation = Rotating (transform, x, y);
        transform.Rotate (0,60,0);
    	//Vector3 toforward = new Vector3(transform.position.x, 0, transform.position.z - m_TurnInputValue1 * m_TurnSpeed * Time.deltaTime);
        //Quaternion targetRotation = Quaternion.LookRotation(toforward);
        //transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, m_TurnSpeed * Time.deltaTime);
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        //float turn = m_TurnInputValue1 * m_TurnSpeed * Time.deltaTime;
        // Make this into a rotation in the y axis.
//        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, m_TurnSpeed * Time.deltaTime);
        //Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        // Apply this rotation to the rigidbody's rotation.
        //m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }


    Quaternion Rotating (Transform tr, float horizontal, float vertical)
    {
                Vector3 targetDirection = new Vector3 (horizontal, 0f, vertical);
                Quaternion targetRotation = Quaternion.LookRotation (targetDirection, Vector3.up);
                Quaternion newRotation = Quaternion.Lerp (tr.transform.rotation, targetRotation, m_TurnSpeed * Time.deltaTime);
                return newRotation;
    }


    private void OnTriggerEnter(Collider other)
    {
        GameObject obj = other.gameObject;
        PowerupHealth player = obj.GetComponent<PowerupHealth>();
        if(player != null) {
            ApplyHealth(obj);
            return;
        }

        PowerupShield player1 = obj.GetComponent<PowerupShield>();
        if (player1 != null)
        {
            ApplyShield(obj);
            return;
        }
    }


    public void ApplyHealth(GameObject obj)
    {
        if (tankHealth.m_StartingHealth - tankHealth.m_CurrentHealth != 0)
        {
            if (tankHealth.m_StartingHealth - tankHealth.m_CurrentHealth >= 40)
                tankHealth.TakeDamage(-40);
            else
                tankHealth.TakeDamage(-(tankHealth.m_StartingHealth - tankHealth.m_CurrentHealth));

            gameManager.SpawnAllPowerUpHealth(obj);
            //powerupHealthSingle.m_Instance = obj2;
            //Instantiate(gameManager.m_PowerUpHealthPrefab, temp,
            //temp2) as GameObject;

        }
    }


    


    public void ApplyShield(GameObject obj)
    {
        tankHealth.SetShield();
        gameManager.SpawnAllPowerUpShield(obj);
    }
}