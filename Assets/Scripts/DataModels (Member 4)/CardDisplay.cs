using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("Datele Cartii")]
    public CardData card; // Se leaga la baza ta de date (ScriptableObject)

    [Header("Elemente UI pe ecran")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI flavorText;
    public Image artworkImage;

    // Această funcție ia datele și le lipește pe ecran
    public void UpdateVisuals()
    {
        if (card != null)
        {
            nameText.text = card.cardName;
            attackText.text = card.attack.ToString();
            hpText.text = card.hp.ToString();
            
            // Dacă ai pus o poză în ScriptableObject, o pune pe carte
            if (card.artwork != null)
            {
                artworkImage.sprite = card.artwork;
            }

            if(card.flavorText != null)
            {
                flavorText.text = card.flavorText;
            }

        }
    }

    // Pentru testare: actualizează cartea imediat ce dai Play la joc
    void Start()
    {
        UpdateVisuals();
    }
}