using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardGen : MonoBehaviour
{
    [Header("CARD SLOTS")]
    public Transform cardSlot;
    public GameObject cardPrefab;

    int prevRound;
    int currRound;
    GameManager gm;

    void Start(){
        gm = GameManager.Instance;

        prevRound = gm.currRound;
        currRound = gm.currRound;
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.Space)){
            gm.currRound++;
            currRound = gm.currRound;
        }

        if(prevRound != currRound){
            ResetCard();
            DrawCard();
            prevRound = currRound;
        }
    }

    void ResetCard(){
        foreach(Transform child in cardSlot){
            Destroy(child.gameObject);
        }
    }

    void DrawCard(){
        //Generate 5 utility cards
        for(int i = 0; i < 5; i++){
            GameObject card = Instantiate(cardPrefab, cardSlot);
            int randIndex = Random.Range(0, utilityCards.Length);
            utilityInfo selectedCard = utilityCards[randIndex];
            
            CardObj cardObj = card.GetComponent<CardObj>();
            cardObj.InitCard(selectedCard.name, selectedCard.cost);
        }
    }

    struct utilityInfo{
        public string name;
        public int cost;

        public utilityInfo(string name, int cost){
            this.name = name;
            this.cost = cost;
        }
    }

    utilityInfo[] utilityCards = new utilityInfo[]{
        new utilityInfo("Card A", 1),
        new utilityInfo("Card B", 2),
        new utilityInfo("Card C", 3),   
        new utilityInfo("Card D", 4),
        new utilityInfo("Card E", 5),
    };
}
