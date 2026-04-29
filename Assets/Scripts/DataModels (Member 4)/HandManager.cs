using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("Referinte Componente")]
    public GameObject cardPrefab;      // Aici vei trage Card_Template-ul tau
    public Transform handArea;         // Aici vei trage obiectul HandArea (cel cu Horizontal Layout Group)

    [Header("Baza de Date de Test")]
    public List<CardData> cardsToSpawn; // O lista in care poti pune ce carti vrei sa apara la inceput

    void Start()
    {
        // La inceputul jocului, generam cartile din lista
        SpawnHand();
    }

    public void SpawnHand()
    {
        foreach (CardData data in cardsToSpawn)
        {
            // 1. Cream o copie a prefab-ului in interiorul HandArea
            GameObject newCard = Instantiate(cardPrefab, handArea);

            // 2. Accesam scriptul CardDisplay de pe noua carte
            CardDisplay display = newCard.GetComponent<CardDisplay>();

            if (display != null)
            {
                // 3. Ii dam datele specifice (ex: Armored Sea Lion)
                display.card = data;

                // 4. Fortam cartea sa isi actualizeze textele si imaginea
                display.UpdateVisuals();
            }
        }
    }
}