using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels; // Obligatoriu pentru PlayFab

public class HandManager : MonoBehaviour
{
    [Header("Referinte Componente")]
    public GameObject cardPrefab;      
    public Transform handArea;         

    void Start()
    {
        // În loc să generăm direct, cerem cărțile de la PlayFab
        GetCardsFromPlayFab();
    }

    // --- PARTEA 1: CEREM DATELE DE LA SERVER ---
    public void GetCardsFromPlayFab()
    {
        Debug.Log("Cerem inventarul de la PlayFab...");
        
        GetUserInventoryRequest request = new GetUserInventoryRequest();
        PlayFabClientAPI.GetUserInventory(request, OnInventorySuccess, OnError);
    }

    private void OnInventorySuccess(GetUserInventoryResult result)
    {
        Debug.Log("Am primit cărțile de la PlayFab!");
        
        List<string> cardIdsFromDatabase = new List<string>();

        // Trecem prin fiecare item din inventarul jucătorului de pe server
        foreach (ItemInstance item in result.Inventory)
        {
            // Salvăm numele item-ului (ex: "ArmoredSeaLion")
            cardIdsFromDatabase.Add(item.ItemId);
            Debug.Log("Carte găsită pe server: " + item.ItemId);
        }

        // Acum că avem ID-urile, generăm cărțile fizice
        SpawnHand(cardIdsFromDatabase);
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("Eroare la PlayFab: " + error.GenerateErrorReport());
    }

    // --- PARTEA 2: GENERĂM CĂRȚILE ÎN UNITY ---
    public void SpawnHand(List<string> cardIdsFromPlayFab)
    {
        // 1. Încărcăm absolut TOATE cărțile din folderul "Resources/Cards" în memorie
        // (Asigură-te că toate ScriptableObjects-urile tale sunt în acel folder!)
        CardData[] allCardsInGame = Resources.LoadAll<CardData>("Cards");

        // 2. Trecem prin fiecare ID primit de la PlayFab
        foreach (string playFabId in cardIdsFromPlayFab)
        {
            CardData carteGasita = null;

            // 3. Căutăm în baza noastră de date locală cartea cu ID-ul potrivit
            foreach (CardData data in allCardsInGame)
            {
                // ATENȚIE: Înlocuiește "cardID" cu numele exact al variabilei tale din CardData.cs
                if (data.cardID == playFabId) 
                {
                    carteGasita = data;
                    break; // Am găsit-o, ne oprim din căutat pentru acest ID
                }
            }

            // 4. Dacă am găsit-o, o generăm pe masă
            if (carteGasita != null)
            {
                GameObject newCard = Instantiate(cardPrefab, handArea);
                CardDisplay display = newCard.GetComponent<CardDisplay>();

                if (display != null)
                {
                    display.card = carteGasita;
                    display.UpdateVisuals();
                }
            }
            else
            {
                Debug.LogWarning("PlayFab a cerut cartea cu ID-ul: " + playFabId + ", dar nu există nicio carte cu acest ID intern în proiect!");
            }
        }
    }
}