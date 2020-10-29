using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TankShootingBot : MonoBehaviour
{
    public int m_BotNumber = 1;       
    public Rigidbody m_Shell;            
    public Transform m_FireTransform;    
    //public Slider m_AimSlider;           
    public AudioSource m_ShootingAudio;  
    public AudioClip m_ChargingClip;     
    public AudioClip m_FireClip;         
    public float m_MinLaunchForce = 10f; 
    public float m_MaxLaunchForce = 40f; 
    public float m_MaxChargeTime = 0.75f;       
    public float m_CurrentLaunchForce;  
    public float m_ChargeSpeed;         
    public bool m_Fired;                


    private void OnEnable()
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        //m_AimSlider.value = m_MinLaunchForce;
    }


    private void Start()
    {
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
    }


    public void Fire()
    {
        // Instantiate and launch the shell.
        if (m_CurrentLaunchForce < m_MinLaunchForce)
            m_CurrentLaunchForce = m_MinLaunchForce;
        m_Fired = true;
        Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
        shellInstance.GetComponent<ShellExplosion>().ownerBot = true;
        shellInstance.GetComponent<ShellExplosion>().ownerObject = this;
        shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;
        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();
        //m_CurrentLaunchForce = m_MinLaunchForce;

    }

}