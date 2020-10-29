using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    public float m_StartingHealth = 200f;          
    public Slider m_Slider;
    public Slider m_SliderShield;
    public Image m_FillImage;
    public Image m_FillImageShield;
    public Color m_FullHealthColor = Color.green;  
    public Color m_ZeroHealthColor = Color.red;    
    public GameObject m_ExplosionPrefab;
    
    
    private AudioSource m_ExplosionAudio;          
    private ParticleSystem m_ExplosionParticles;   
    [HideInInspector] public float m_CurrentHealth;
    [HideInInspector] public float m_CurrentShield;
    public bool m_Dead;            


    private void Awake()
    {
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

        m_ExplosionParticles.gameObject.SetActive(false);
    }


    private void OnEnable()
    {
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;

        SetHealthUI();
    }
    

    public void TakeDamage(float amount)
    {
        // Adjust the tank's current health, update the UI based on the new health and check whether or not the tank is dead.
        if(m_CurrentShield - amount >= 0 && amount >= 0)
        {
            m_CurrentShield -= amount;
        } else if(m_CurrentShield - amount < 0 && amount >= 0)
        {
            m_CurrentHealth += m_CurrentShield;
            m_CurrentHealth -= amount;
            m_CurrentShield = 0;
        } else 
        {
            m_CurrentHealth -= amount;
        }
        
        SetHealthUI();
        SetShieldUI();
        if(m_CurrentHealth <= 0f && !m_Dead)
        {
            OnDeath();
        }
    }


    public void SetShield()
    {
        // Adjust the tank's current health, update the UI based on the new health and check whether or not the tank is dead.
        m_CurrentShield = 100;
        SetShieldUI();
    }


    private void SetHealthUI()
    {
        // Adjust the value and colour of the slider.
        m_Slider.value = m_CurrentHealth;
        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        //Debug.Log("Health: " + m_CurrentHealth);
    }


    private void SetShieldUI()
    {
        // Adjust the value and colour of the slider.
        m_SliderShield.value = m_CurrentShield;
        m_FillImageShield.color = Color.Lerp(Color.cyan, Color.cyan, m_CurrentShield / 100);
        //Debug.Log("Shield: " + m_CurrentShield);
    }


    private void OnDeath()
    {
        // Play the effects for the death of the tank and deactivate it.
        m_Dead = true;
        m_ExplosionParticles.transform.position = transform.position;
        m_ExplosionParticles.gameObject.SetActive(true);
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();
        gameObject.SetActive(false);
    }
}