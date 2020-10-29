using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingColor : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(GetComponent<Image>().color);
        //GetComponent<Image>().color = new Color(Random.Range(0,255), Random.Range(0, 255), Random.Range(0, 255), 255);
        GetComponent<Image>().color = Color.Lerp(Color.red, Color.white, Mathf.Abs(Mathf.Sin(Time.time)));

        //GetComponent<Image>().material.color = Color.white;

    }
}
