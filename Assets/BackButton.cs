using UnityEngine;
using UnityEngine.EventSystems;

public class BackButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
	[HideInInspector]
	public bool changed;
    
    public void OnPointerDown(PointerEventData eventData) {
        //back = !back;
        changed = true;
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
            /*if  (Input.touches.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
            	Pressed = false;
                Hold = false;  
            }*/
        
    } 
}
