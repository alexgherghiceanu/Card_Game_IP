using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.EventSystems;

public class DeckBuilderManager : MonoBehaviour
{
    [Header("Sursa colectiei")]
    [Tooltip("Daca e bifat, colectia vine din cartile tale din PlayFab (recomandat). Altfel se folosesc availableCards de mai jos.")]
    public bool incarcaColectiaDinPlayFab = true;

    [Tooltip("Carti de test (fallback) cand nu folosesti PlayFab.")]
    public List<CardData> availableCards = new List<CardData>();

    [Tooltip("Clasele obligatorii. Daca ramane gol, se iau automat clasele distincte din colectie.")]
    public List<string> requiredClasses = new List<string>();

    [Header("UI References")]
    public Transform collectionContent;
    public Transform deckContent;
    public DeckbuilderCardButton cardButtonPrefab;

    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI validationText;

    public Button saveButton;
    public Button clearButton;

    [Header("Preview carte (dublu-click)")]
    [Tooltip("Prefab-ul Card_Template (cel cu CardDisplay, arta si flavor).")]
    public GameObject cardPreviewPrefab;

    [Header("Runtime Data")]
    [SerializeField] private List<CardData> currentDeck = new List<CardData>();

    private bool astepPlayFab = false;
    private GameObject previewOverlay;

    private void Start()
    {
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

        var inv = PlayFabInventoryManager.Instance;
        if (incarcaColectiaDinPlayFab && inv != null)
        {
            bool gata = inv.cardStatsDatabase != null && inv.cardStatsDatabase.Count > 0;
            if (gata)
            {
                IncarcaColectiaDinPlayFab();
            }
            else
            {
                astepPlayFab = true;
                PlayFabInventoryManager.OnInventoryReady += OnPlayFabReady;
                ShowMessage("Astept cartile din PlayFab...");
            }
        }
        else
        {
            InitializeRequiredClassesIfNeeded();
            RefreshUI();
        }
    }

    private void OnDisable()
    {
        if (astepPlayFab)
        {
            PlayFabInventoryManager.OnInventoryReady -= OnPlayFabReady;
            astepPlayFab = false;
        }
    }

    private void OnPlayFabReady()
    {
        PlayFabInventoryManager.OnInventoryReady -= OnPlayFabReady;
        astepPlayFab = false;
        IncarcaColectiaDinPlayFab();
    }

    private void IncarcaColectiaDinPlayFab()
    {
        availableCards = CloudCardFactory.BuildOwnedCollection();
        Debug.Log("[DeckBuilder] Colectie incarcata din PlayFab: " + availableCards.Count + " carti.");
        InitializeRequiredClassesIfNeeded();
        RefreshUI();
    }

    public void AddCardToDeck(CardData card)
    {
        if (card == null) { ShowMessage("Nu pot adauga o carte nula."); return; }

        if (currentDeck.Count >= DeckValidator.RequiredDeckSize)
        {
            ShowMessage("Deck-ul este deja plin. Maxim " + DeckValidator.RequiredDeckSize + " carti.");
            return;
        }

        int copies = CountCopiesInDeck(card);
        if (copies >= DeckValidator.MaxCopiesPerCard)
        {
            ShowMessage("Nu poti avea mai mult de " + DeckValidator.MaxCopiesPerCard + " copii din \"" + card.cardName + "\".");
            return;
        }

        currentDeck.Add(card);
        RefreshUI();
    }

    public void RemoveCardFromDeck(CardData card)
    {
        if (card == null) return;

        CardData cardToRemove = currentDeck.FirstOrDefault(d => d != null && d.cardID == card.cardID);
        if (cardToRemove != null) currentDeck.Remove(cardToRemove);

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

        SalveazaInPlayFab();
    }

    private void SalveazaInPlayFab()
    {
        List<string> ids = currentDeck.Where(c => c != null && !string.IsNullOrWhiteSpace(c.cardID))
                                      .Select(c => c.cardID).ToList();
        string deckData = string.Join(",", ids);

        if (PlayFabInventoryManager.Instance == null)
        {
            ShowMessage("PlayFab indisponibil. Deck validat local, dar nu a putut fi salvat in cloud.");
            return;
        }

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "ActiveDeck", deckData } }
        };
        PlayFabClientAPI.UpdateUserData(request, OnSaveSuccess, OnSaveError);

        // Sincronizam si copia din memorie, ca deck-ul sa fie disponibil imediat
        // in meciul curent, fara re-logare.
        var inv = PlayFabInventoryManager.Instance;
        inv.playerActiveDeck = new List<string>(ids);
        inv.enemyActiveDeck = new List<string>(ids); // pentru test vs AI
    }

    private void OnSaveSuccess(UpdateUserDataResult result)
    {
        ShowMessage("Deck salvat in PlayFab! Va aparea in urmatorul meci.");
        Debug.Log("[DeckBuilder] ActiveDeck salvat in cloud.");
    }

    private void OnSaveError(PlayFabError error)
    {
        ShowMessage("Eroare la salvarea deck-ului: " + error.GenerateErrorReport());
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
        if (collectionContent == null || cardButtonPrefab == null) return;

        foreach (CardData card in availableCards)
        {
            if (card == null) continue;
            DeckbuilderCardButton b = Instantiate(cardButtonPrefab, collectionContent);
            b.Setup(card, this, false, CountCopiesInDeck(card));
        }
    }

    private void RefreshDeckUI()
    {
        ClearChildren(deckContent);
        if (deckContent == null || cardButtonPrefab == null) return;

        foreach (CardData card in currentDeck)
        {
            if (card == null) continue;
            DeckbuilderCardButton b = Instantiate(cardButtonPrefab, deckContent);
            b.Setup(card, this, true, CountCopiesInDeck(card));
        }
    }

    private void RefreshValidationUI()
    {
        if (deckCountText != null)
            deckCountText.text = "Deck: " + currentDeck.Count + "/" + DeckValidator.RequiredDeckSize;

        DeckValidationResult result = DeckValidator.ValidateDeck(currentDeck, requiredClasses);
        if (validationText != null)
            validationText.text = result.IsValid ? "Deck valid. Poate fi salvat." : result.GetMessage();
    }

    private int CountCopiesInDeck(CardData card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.cardID)) return 0;
        return currentDeck.Count(d => d != null && d.cardID == card.cardID);
    }

    private void InitializeRequiredClassesIfNeeded()
    {
        if (requiredClasses != null && requiredClasses.Count > 0) return;

        requiredClasses = availableCards
            .Where(c => c != null && !string.IsNullOrWhiteSpace(c.cardClass))
            .Select(c => c.cardClass.Trim())
            .Distinct()
            .ToList();
    }

    // =================== PREVIEW CARTE (dublu-click) ===================
    public void ShowCardPreview(CardData card)
    {
        if (card == null) return;
        if (cardPreviewPrefab == null)
        {
            Debug.LogWarning("[DeckBuilder] Card Preview Prefab nu este legat (pune prefab-ul Card_Template).");
            return;
        }

        HideCardPreview();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null && collectionContent != null) canvas = collectionContent.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Fundal intunecat pe tot ecranul; click oriunde inchide.
        previewOverlay = new GameObject("CardPreviewOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        previewOverlay.transform.SetParent(canvas.transform, false);
        RectTransform ort = previewOverlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        Image bg = previewOverlay.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = true;
        previewOverlay.transform.SetAsLastSibling();

        DeckPreviewCloser closer = previewOverlay.AddComponent<DeckPreviewCloser>();
        closer.manager = this;

        // Cartea, mare, in centru.
        GameObject cardGo = Instantiate(cardPreviewPrefab, previewOverlay.transform);
        cardGo.SetActive(true);
        RectTransform crt = cardGo.GetComponent<RectTransform>();
        if (crt != null)
        {
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.localScale = Vector3.one * 2f;
        }

        CardDragNDrop dnd = cardGo.GetComponent<CardDragNDrop>();
        if (dnd != null) dnd.enabled = false;

        CardDisplay disp = cardGo.GetComponent<CardDisplay>();
        if (disp == null) disp = cardGo.GetComponentInChildren<CardDisplay>();
        if (disp != null) disp.SetupFromCardData(card);
    }

    public void HideCardPreview()
    {
        if (previewOverlay != null)
        {
            Destroy(previewOverlay);
            previewOverlay = null;
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void ShowMessage(string message)
    {
        if (validationText != null) validationText.text = message;
        Debug.Log(message);
    }
}

/// <summary>Inchide preview-ul cand dai click pe fundal (sau pe carte).</summary>
public class DeckPreviewCloser : MonoBehaviour, IPointerClickHandler
{
    public DeckBuilderManager manager;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null) manager.HideCardPreview();
    }
}