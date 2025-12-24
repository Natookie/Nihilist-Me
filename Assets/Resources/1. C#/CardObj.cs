using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardObj : MonoBehaviour
{
    [Header("REFERENCES")]
    public TextMeshProUGUI cardName;
    public TextMeshProUGUI cardCost;

    public void InitCard(string name, int cost){
        cardName.text = name;
        cardCost.text = $"Cost: {cost}";
    }

    void Update(){
        if(Input.GetMouseButtonDown(0)){
            Vector2 mousePos = Input.mousePosition;
            PointerEventData pointerData = new PointerEventData(EventSystem.current){
                position = mousePos
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult result in results){
                if(result.gameObject == this.gameObject){
                    Debug.Log($"{cardName.text} clicked!");
                }
            }
        }
    }
}
