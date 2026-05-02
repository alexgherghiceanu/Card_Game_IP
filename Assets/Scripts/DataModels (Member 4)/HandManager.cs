using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("Referinte Componente")]
    public Transform handArea; // Panoul (Horizontal Layout Group) unde stau cartile in mana

    // Cand obiectul devine activ, se aboneaza la anuntul lui PlayFab
    private void OnEnable()
    {
        PlayFabInventoryManager.OnInventoryReady += InitializeHand;
    }

    // E crucial sa ne dezabonam daca obiectul este distrus, ca sa nu dea erori Unity
    private void OnDisable()
    {
        PlayFabInventoryManager.OnInventoryReady -= InitializeHand;
    }

    // Aceasta functie nu mai ruleaza la Start, ci DOAR cand striga PlayFab ca e gata
    private void InitializeHand()
    {
        if (PlayFabInventoryManager.Instance != null)
        {
            List<string> jucatorDeck = PlayFabInventoryManager.Instance.ownedCards;

            if (jucatorDeck.Count > 0)
            {
                SpawnHand(jucatorDeck);
            }
            else
            {
                Debug.LogWarning("Jucatorul nu are carti in inventar (dupa ce s-a terminat descarcarea)!");
            }
        }
    }

    public void SpawnHand(List<string> cardIdsToSpawn)
    {
        foreach (string cardId in cardIdsToSpawn)
        {
            // 1. Cerem Managerului prefab-ul vizual pentru acest ID
            GameObject prefabToSpawn = PlayFabInventoryManager.Instance.GetVisualPrefabByID(cardId);

            // 2. Cerem Managerului statisticile din Cloud (Atac, HP, Cost)
            CloudCardStats cloudStats = PlayFabInventoryManager.Instance.GetCloudStatsByID(cardId);

            // Verificam daca ambele au fost gasite cu succes
            if (prefabToSpawn != null && cloudStats != null)
            {
                // 3. Generam cartea in mana
                GameObject newCard = Instantiate(prefabToSpawn, handArea);

                // 4. Cautam componenta vizuala a cartii
                CardDisplay display = newCard.GetComponent<CardDisplay>();
                if (display != null)
                {
                    // Trimitem noile statistici din cloud catre componenta vizuala
                    display.SetupCardFromCloud(cloudStats, cardId);
                }
            }
            else
            {
                Debug.LogWarning($"Eroare la generarea cartii cu ID-ul {cardId}. Lipseste Prefab-ul sau Datele!");
            }
        }
    }
}