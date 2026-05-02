using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckBuilderManager : MonoBehaviour
{
    [Header("Test Data")]
    [Tooltip("Cartile disponibile pentru test. Mai tarziu vor veni de la Membrul 4 / PlayFab.")]
    public List<CardData> availableCards = new List<CardData>();

    [Tooltip("Clasele obligatorii. Daca lista ramane goala, se iau automat clasele distincte din availableCards.")]
    public List<string> requiredClasses = new List<string>();

    [Header("UI References")]
    public Transform collectionContent;
    public Transform deckContent;
    public DeckbuilderCardButton cardButtonPrefab;

    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI validationText;

    public Button saveButton;
    public Button clearButton;

    [Header("Runtime Data")]
    [SerializeField] private List<CardData> currentDeck = new List<CardData>();

    private void Start()
    {
        InitializeRequiredClassesIfNeeded();

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveDeck);
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(ClearDeck);
        }

        RefreshUI();
    }

    public void AddCardToDeck(CardData card)
    {
        if (card == null)
        {
            ShowMessage("Nu pot adauga o carte nula.");
            return;
        }

        if (currentDeck.Count >= DeckValidator.RequiredDeckSize)
        {
            ShowMessage("Deck-ul este deja plin. Maxim " + DeckValidator.RequiredDeckSize + " carti.");
            return;
        }

        int copies = CountCopiesInDeck(card);

        if (copies >= DeckValidator.MaxCopiesPerCard)
        {
            ShowMessage("Nu poti avea mai mult de " + DeckValidator.MaxCopiesPerCard + " copii din cartea \"" + card.cardName + "\".");
            return;
        }

        currentDeck.Add(card);
        RefreshUI();
    }

    public void RemoveCardFromDeck(CardData card)
    {
        if (card == null)
        {
            return;
        }

        CardData cardToRemove = currentDeck.FirstOrDefault(deckCard => deckCard != null && deckCard.cardID == card.cardID);

        if (cardToRemove != null)
        {
            currentDeck.Remove(cardToRemove);
        }

        RefreshUI();
    }

    public void SaveDeck()
    {
        DeckValidationResult result = DeckValidator.ValidateDeck(currentDeck, requiredClasses);

        if (!result.IsValid)
        {
            ShowMessage("Deck invalid:\n" + result.GetMessage());
            return;
        }

        ShowMessage("Deck salvat local cu succes. Urmatorul pas: conectare la PlayFab / save API.");
        Debug.Log("Deck valid. Se poate salva.");
    }

    public void ClearDeck()
    {
        currentDeck.Clear();
        RefreshUI();
    }

    public List<CardData> GetCurrentDeck()
    {
        return new List<CardData>(currentDeck);
    }

    private void RefreshUI()
    {
        RefreshCollectionUI();
        RefreshDeckUI();
        RefreshValidationUI();
    }

    private void RefreshCollectionUI()
    {
        ClearChildren(collectionContent);

        if (collectionContent == null || cardButtonPrefab == null)
        {
            return;
        }

        foreach (CardData card in availableCards)
        {
            if (card == null)
            {
                continue;
            }

            DeckbuilderCardButton buttonInstance = Instantiate(cardButtonPrefab, collectionContent);
            buttonInstance.Setup(card, this, false, CountCopiesInDeck(card));
        }
    }

    private void RefreshDeckUI()
    {
        ClearChildren(deckContent);

        if (deckContent == null || cardButtonPrefab == null)
        {
            return;
        }

        foreach (CardData card in currentDeck)
        {
            if (card == null)
            {
                continue;
            }

            DeckbuilderCardButton buttonInstance = Instantiate(cardButtonPrefab, deckContent);
            buttonInstance.Setup(card, this, true, CountCopiesInDeck(card));
        }
    }

    private void RefreshValidationUI()
    {
        if (deckCountText != null)
        {
            deckCountText.text = "Deck: " + currentDeck.Count + "/" + DeckValidator.RequiredDeckSize;
        }

        DeckValidationResult result = DeckValidator.ValidateDeck(currentDeck, requiredClasses);

        if (validationText != null)
        {
            if (result.IsValid)
            {
                validationText.text = "Deck valid. Poate fi salvat.";
            }
            else
            {
                validationText.text = result.GetMessage();
            }
        }
    }

    private int CountCopiesInDeck(CardData card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.cardID))
        {
            return 0;
        }

        return currentDeck.Count(deckCard => deckCard != null && deckCard.cardID == card.cardID);
    }

    private void InitializeRequiredClassesIfNeeded()
    {
        if (requiredClasses != null && requiredClasses.Count > 0)
        {
            return;
        }

        requiredClasses = availableCards
            .Where(card => card != null && !string.IsNullOrWhiteSpace(card.cardClass))
            .Select(card => card.cardClass.Trim())
            .Distinct()
            .ToList();
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void ShowMessage(string message)
    {
        if (validationText != null)
        {
            validationText.text = message;
        }

        Debug.Log(message);
    }
}
