using UnityEngine;

public class ShellExplosion : MonoBehaviour
{
    public LayerMask m_TankMask;
    public ParticleSystem m_ExplosionParticles;       
    public AudioSource m_ExplosionAudio;              
    public float m_MaxDamage = 50f;                  
    public float m_ExplosionForce = 300f;            
    public float m_MaxLifeTime = 2f;                  
    public float m_ExplosionRadius = 5f;
    public bool ownerBot;
    public TankShootingBot ownerObject;


    private void Start()
    {
        Destroy(gameObject, m_MaxLifeTime);
            
    }


    private void OnTriggerEnter(Collider other)
    {
        // Find all the tanks in an area around the shell and damage them.
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);
        for (int i = 0; i < colliders.Length; i++)
        {
            Rigidbody targetRigidBody = colliders[i].GetComponent<Rigidbody>();
            if (!targetRigidBody)
                continue;
            targetRigidBody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);
            TankHealth targetHealth = targetRigidBody.GetComponent<TankHealth>();
            TankHealthBot targetHealthBot = targetRigidBody.GetComponent<TankHealthBot>();
            if (!targetHealth && !targetHealthBot)
                continue;
            float damage = CalculateDamage(targetRigidBody.position);
            if (targetHealth)
                targetHealth.TakeDamage(damage);
            else if(!ownerBot)
                targetHealthBot.TakeDamage(damage);
        }

        m_ExplosionParticles.transform.parent = null;
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();
        if(ownerBot == true)
            ownerObject.m_Fired = false;
        Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.duration);
        Destroy(gameObject);
    }


    private float CalculateDamage(Vector3 targetPosition)
    {
        // Calculate the amount of damage a target should take based on it's position.
        Vector3 explosionToTarget = targetPosition - transform.position;
        float explosionDistance = explosionToTarget.magnitude;
        float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
        float damage = relativeDistance * m_MaxDamage;
        damage = Mathf.Max(0f, damage);
        return damage;
    }
}