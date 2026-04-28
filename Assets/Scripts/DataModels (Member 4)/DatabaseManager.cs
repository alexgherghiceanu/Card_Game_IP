using System.Collections.Generic;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance;

    public List<CardData> allCardsInGame; 

    void Awake()
    {
        Instance = this;
    }

    public CardData GetCardByID(string idToFind)
    {
        foreach (CardData card in allCardsInGame)
        {
            if (card.cardID == idToFind)
                return card;
        }
        Debug.LogError("Cartea cu ID " + idToFind + " nu există în baza de date locală!");
        return null;
    }
}