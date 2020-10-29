using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TankMovementBot : MonoBehaviour
{
    public int m_BotNumber = 1;              // Used to identify which tank belongs to which player.  This is set by this tank's manager.
    public float m_Speed = 6f;                 // How fast the tank moves forward and back.
    public float m_TurnSpeed = 120f;            // How fast the tank turns in degrees per second.
    public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
    public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
    public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
    public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.
    public float obstacleRange = 4.0f;
    public bool tankCatched;


    private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
    private string m_TurnAxisName;              // The name of the input axis for turning.
    private Rigidbody m_Rigidbody;              // Reference used to move the tank.
    private float m_MovementInputValue;         // The current value of the movement input.
    //private float m_TurnInputValue;             // The current value of the turn input.
    private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
    private TankShootingBot m_Shooting;
    private Transform _playerTransform;
    private float angle;
    private float maxAngle;
    private Vector3 playerDirection;
    private NavMeshAgent agent;
    private GameObject tankObj;
    private Coroutine currentCoroutine;
    private Vector3 outPos;
    private Quaternion outOrient;
    private bool fire;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }


    private void OnEnable()
    {
        // When the tank is turned on, make sure it's not kinematic.
        m_Rigidbody.isKinematic = false;
    }


    private void OnDisable()
    {
        // When the tank is turned off, set it to kinematic so it stops moving.
        m_Rigidbody.isKinematic = true;
    }


    private void Start()
    {
        // Store the original pitch of the audio source.
        m_OriginalPitch = m_MovementAudio.pitch;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        fire = true;
        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);

        m_Rigidbody.velocity = transform.forward;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 20);

        int i = 0;
        while (i < hitColliders.Length)
        {
            if(hitColliders[i].GetComponent<TankMovement>()) {
                tankObj = hitColliders[i].gameObject;
                agent.SetDestination(tankObj.transform.position);
            }
            i++;
        }

        if (Physics.SphereCast(ray, 1f, out hit))
        {
            GameObject hitObject = hit.transform.gameObject;
            //Vector3 forward = transform.TransformDirection(Vector3.forward) * 10;
            //Debug.DrawRay(transform.position, forward, Color.red);

            if (tankCatched)
            {
                playerDirection = (tankObj.transform.position - this.transform.position).normalized;
                angle = Vector3.Angle(this.transform.forward, playerDirection);
                maxAngle = m_TurnSpeed * Time.deltaTime;
                //Quaternion rot = Quaternion.LookRotation(tankObj.transform.position - this.transform.position);
                agent.SetDestination(tankObj.transform.position);

                m_Shooting = GetComponent<TankShootingBot>();

                VehicleMovementPrediction(tankObj.transform.position, tankObj.GetComponent<Rigidbody>().velocity, tankObj.transform.rotation,
                                tankObj.GetComponent<Rigidbody>().angularVelocity, 1f, 10, out outPos, out outOrient);

                //if(tankObj.transform.position != outPos)
                 //   Debug.Log("Current: " + tankObj.transform.position + "Predicted: " + outPos);

                Vector3 toforward = outPos - transform.position;
                Quaternion targetRotation = Quaternion.LookRotation(toforward);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, maxAngle);

                m_Shooting.m_CurrentLaunchForce = Vector3.Distance(outPos, this.transform.position) * Random.Range(1.5f,1.6f);

                if (Vector3.Distance(tankObj.transform.position, this.transform.position) > 30 || tankObj.GetComponent<TankHealth>().m_Dead)
                {
                    tankCatched = false;
                    agent.ResetPath();
                    fire = false;
                    agent.SetDestination(new Vector3(Random.Range(-36, 43), this.transform.position.y, Random.Range(-149, 46)));
                } else if(!hitObject.GetComponent<TankMovement>())
                {
                    fire = false;
                }
                else
                {
                    if (!m_Shooting.m_Fired && fire)
                    {
                        m_Shooting.Fire();
                    }
                }
                /*if (angle > maxAngle)
                {
                    this.transform.rotation = Quaternion.Slerp(this.transform.rotation, rot, maxAngle);
                }*/
            }
            else
            {
                if (hitObject.GetComponent<TankMovement>() && Vector3.Distance(hitObject.transform.position, this.transform.position) < 40 )
                {
                    tankCatched = true;
                    tankObj = hitObject;
                }
                else
                {
                    if (agent.remainingDistance < 2)
                    {
                        agent.SetDestination(new Vector3(Random.Range(-36, 43), this.transform.position.y, Random.Range(-149, 46)));
                    }
                    /*Vector3 movement = transform.forward * m_Speed * Time.deltaTime;
                    m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
                    if (hit.distance < obstacleRange)
                    {
                        float angle = Random.Range(-180, 180);
                        float turn = angle * m_TurnSpeed * Time.deltaTime;
                        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                    }*/
                }
            }
        }
        //EngineAudio();

        /* if (tankCatched)
         {
             Debug.Log(tankObj.transform.position);
             playerDirection = (tankObj.transform.position - this.transform.position).normalized;
             angle = Vector3.Angle(this.transform.forward, playerDirection);
             maxAngle = m_TurnSpeed * Time.deltaTime;

             /*if(angle > maxAngle)
             {
                 this.transform.rotation = Quaternion.Slerp(this.transform.rotation, rot, maxAngle);
             }

             if (angle < m_TurnSpeed && Vector3.Distance(tankObj.transform.position, this.transform.position) < 40.0) //&& hit.distance > obstacleRange)
             {
                 m_Shooting = GetComponent<TankShootingBot>();
                 m_Shooting.m_CurrentLaunchForce = m_Shooting.m_MaxLaunchForce;
                 m_Shooting.Fire();
             }

         } else 
         {
             RaycastHit hit;
             Ray ray = new Ray(transform.position, transform.forward);
             if (Physics.SphereCast(ray, 2f, out hit))
             {
                 GameObject hitObject = hit.transform.gameObject;
                 if (hitObject.GetComponent<TankMovement>())
                 {
                     tankCatched = true;
                     tankObj = hitObject;
                     agent.SetDestination(_playerTransform.position);
                 }
                 else
                 {
                     Vector3 movement = transform.forward * m_Speed * Time.deltaTime;
                     m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
                     if (hit.distance < obstacleRange)
                     {
                         float angle = Random.Range(-180, 180);
                         float turn = angle * m_TurnSpeed * Time.deltaTime;
                         Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                         m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                     }
                 }
             }
         } */
        
    }


    Vector3 LinearMovementPrediction(Vector3 CurrentPosition, Vector3 CurrentVelocity, float PredictionTime)
    {
        Vector3 PredictedPosition = CurrentPosition + CurrentVelocity * PredictionTime;
        return PredictedPosition;
    }

    Quaternion RotationalMovementPrediction(Quaternion CurrentOrientation, Vector3 AngularVelocity, float PredictionTime)
    {
        float RotationAngle = AngularVelocity.magnitude * PredictionTime;
        Vector3 RotationAxis = AngularVelocity.normalized;
        Quaternion RotationFromAngularVelocity = Quaternion.AngleAxis(RotationAngle * Mathf.Rad2Deg, RotationAxis);
        Quaternion PredictedOrientation = CurrentOrientation * RotationFromAngularVelocity;
        return PredictedOrientation;
    }

    void VehicleMovementPrediction(Vector3 Position, Vector3 LinearVelocity, Quaternion Orientation,
        Vector3 AngularVelocity, float PredictionTime, int NumberOfIterations, out Vector3 outPosition, out Quaternion outOrientation)
    {
        float DeltaTime = PredictionTime / NumberOfIterations;

        for (int i = 1; i <= NumberOfIterations; ++i)

        {
            Position = LinearMovementPrediction(Position, LinearVelocity, DeltaTime);
            Orientation = RotationalMovementPrediction(Orientation, AngularVelocity, DeltaTime);

            // Match LinearVelocity with the new forward direction from Orientation.
            LinearVelocity = Orientation * new Vector3(0.0f, 0.0f, LinearVelocity.magnitude);

        }
        outPosition = Position;
        outOrientation = Orientation;
    }


    private void EngineAudio()
    {
        // If there is no input (the tank is stationary)...
        
            // Otherwise if the tank is moving and if the idling clip is currently playing...
            if (m_MovementAudio.clip == m_EngineIdling)
            {
                // ... change the clip to driving and play.
                m_MovementAudio.clip = m_EngineDriving;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        
    }


    /*private void FixedUpdate()
    {
        // Adjust the rigidbodies position and orientation in FixedUpdate.
        Move();
        Turn();
    }*/


    /*private void Move()
    {
        // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
        Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;

        // Apply this movement to the rigidbody's position.
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }


    private void Turn()
    {
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

        // Make this into a rotation in the y axis.
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

        // Apply this rotation to the rigidbody's rotation.
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation); 
    }*/
}