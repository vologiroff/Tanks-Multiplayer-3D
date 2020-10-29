/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;

[Serializable]
public class PowerupShieldSingle
{
    public int amount = 50;
    [HideInInspector] public GameObject m_Instance;
    public Transform m_SpawnPoint;

}
