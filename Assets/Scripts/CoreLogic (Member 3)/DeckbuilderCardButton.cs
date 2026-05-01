using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckbuilderCardButton : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI labelText;
    public Button button;

    private CardData cardData;
    private DeckBuilderManager deckBuilderManager;
    private bool isDeckCard;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (labelText == null)
        {
            labelText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void Setup(CardData card, DeckBuilderManager manager, bool representsDeckCard, int copiesInDeck)
    {
        cardData = card;
        deckBuilderManager = manager;
        isDeckCard = representsDeckCard;

        UpdateLabel(copiesInDeck);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void UpdateLabel(int copiesInDeck)
    {
        if (labelText == null)
        {
            return;
        }

        if (cardData == null)
        {
            labelText.text = "Carte lipsa";
            return;
        }

        string cardName = string.IsNullOrWhiteSpace(cardData.cardName) ? cardData.cardID : cardData.cardName;
        string cardClass = string.IsNullOrWhiteSpace(cardData.cardClass) ? "No class" : cardData.cardClass;

        if (isDeckCard)
        {
            labelText.text = cardName + "\nCost: " + cardData.manaCost + " | ATK: " + cardData.attack + " | HP: " + cardData.hp + "\nClick pentru eliminare";
        }
        else
        {
            labelText.text = cardName + "\nClasa: " + cardClass + " | Cost: " + cardData.manaCost + "\nIn deck: " + copiesInDeck + "/2";
        }
    }

    private void OnButtonClicked()
    {
        if (deckBuilderManager == null || cardData == null)
        {
            return;
        }

        if (isDeckCard)
        {
            deckBuilderManager.RemoveCardFromDeck(cardData);
        }
        else
        {
            deckBuilderManager.AddCardToDeck(cardData);
        }
    }
}
