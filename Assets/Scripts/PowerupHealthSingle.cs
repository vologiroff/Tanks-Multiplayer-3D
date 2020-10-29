/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;

 
[Serializable]
public class PowerupHealthSingle
{
    public Transform m_SpawnPoint;
    public int amount = 20;
    [HideInInspector] public GameObject m_Instance;
    public int m_number;

    public void OnTriggerEnter(Collider col)
    {
        GameObject obj = col.gameObject;
        TankHealth player = obj.GetComponent<TankHealth>();
        Debug.Log("!!!!");

        //try to apply collectible to player, the result should be true
        Apply(player);

    }


    public void Apply(TankHealth obj)
    {
        if (obj.m_StartingHealth - obj.m_CurrentHealth >= amount)
            obj.TakeDamage(-amount);
        else
            obj.TakeDamage(-(obj.m_StartingHealth - obj.m_CurrentHealth));
        m_Instance = null;
    }
}
