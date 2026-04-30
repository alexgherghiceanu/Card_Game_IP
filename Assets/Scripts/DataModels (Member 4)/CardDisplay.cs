using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("Vizualuri Statice (Local)")]
    // Pastram referinta la ScriptableObject doar pentru poze si nume
    public CardData card;

    [Header("Elemente UI pe ecran")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI flavorText;
    public Image artworkImage;

    // Aceasta functie este apelata DOAR de HandManager dupa ce are datele de la PlayFab
    public void SetupCardFromCloud(CloudCardStats cloudStats, string cardId)
    {
        // 1. INITIALIZAM VIZUALUL (din memoria locala Unity)
        if (card != null)
        {
            nameText.text = card.cardName;

            if (card.artwork != null)
            {
                artworkImage.sprite = card.artwork;
            }
        }
        else
        {
            // Fallback in caz ca ai uitat sa pui CardData pe prefab
            nameText.text = cardId;
            Debug.LogWarning($"CardData lipseste de pe prefab-ul cartii: {cardId}");
        }

        // 2. SUPRASCRIEM STATISTICILE (din Cloud / PlayFab)
        if (cloudStats != null)
        {
            // Folosim valorile dinamice venite de pe server
            attackText.text = cloudStats.Attack;
            hpText.text = cloudStats.Health;

            // Daca ai completat o descriere pe server, o afisam. Altfel, o lasam goala.
            if (!string.IsNullOrEmpty(cloudStats.flavorText))
            {
                flavorText.text = cloudStats.flavorText;
            }
            else if (card != null && card.flavorText != null)
            {
                // Fallback la textul local daca pe cloud e gol
                flavorText.text = card.flavorText;
            }
        }
        else
        {
            Debug.LogError($"Nu s-au putut aplica statisticile din Cloud pentru cartea: {cardId}");
        }
    }
}