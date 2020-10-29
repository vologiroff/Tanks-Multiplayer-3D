using UnityEngine;
using UnityEngine.EventSystems;

public class Joybutton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
	[HideInInspector]
	public bool Pressed;
    public bool Hold;


    void Update() {
        if (Pressed) {
            Hold = true;
        }
        /*foreach (Touch touch in Input.touches) {
            if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled && touch.phase != TouchPhase.Began) {
                Hold = true;
                Pressed = false;
            }
        }
        /*foreach (Touch touch in Input.touches) {
            Vector3 vector = Camera.main.ScreenToWorldPoint(touch.position);
            Debug.DrawLine(Vector3.zero, touch.position, Color.red);
            Debug.Log(touch.position);
            //if (Physics.Raycast(ray)) {
              //  Debug.DrawLine(Vector3.zero, vector.position, Color.red);
                //Debug.Log("!!!!");
            //}
            if (touch.phase == TouchPhase.Began) {
                Pressed = true;
                Hold = false;
            } else
            if  (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled) {
                Hold = true;
                Pressed = false;
            } else {
                Pressed = false;
                Hold = false;
            }    
        } */
    } 
    
    
    public void OnPointerDown(PointerEventData eventData) {
        Pressed = true;
        //Hold = true;
        	/*if (Input.touches.phase == TouchPhase.Began) {
                Pressed = true;
                Hold = false;
            } else if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled) {
                Hold = true;
                Pressed = false;
            } */
        
    }

    public void OnPointerUp(PointerEventData eventData) {
        Pressed = false;
        Hold = false;
            /*if  (Input.touches.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
            	Pressed = false;
                Hold = false;  
            }*/
        
    } 
}
