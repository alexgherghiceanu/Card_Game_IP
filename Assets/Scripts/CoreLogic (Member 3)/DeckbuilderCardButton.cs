using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckbuilderCardButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public TextMeshProUGUI labelText;
    public Button button;

    private CardData cardData;
    private DeckBuilderManager deckBuilderManager;
    private bool isDeckCard;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private GameObject dragPreview;

    public static DeckbuilderCardButton CurrentDraggedButton { get; private set; }

    public CardData CardData
    {
        get { return cardData; }
    }

    public DeckBuilderManager DeckBuilderManager
    {
        get { return deckBuilderManager; }
    }

    public bool IsDeckCard
    {
        get { return isDeckCard; }
    }

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

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rootCanvas = GetComponentInParent<Canvas>();
    }

    private void OnDisable()
    {
        CleanupDragVisuals();
    }

    private void OnDestroy()
    {
        CleanupDragVisuals();
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
            labelText.text =
                cardName +
                "\nCost: " + cardData.manaCost +
                " | ATK: " + cardData.attack +
                " | HP: " + cardData.hp +
                "\nClick pentru eliminare";
        }
        else
        {
            labelText.text =
                cardName +
                "\nClasa: " + cardClass +
                " | Cost: " + cardData.manaCost +
                "\nIn deck: " + copiesInDeck + "/2";
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardData == null || deckBuilderManager == null)
        {
            return;
        }

        CurrentDraggedButton = this;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        CreateDragPreview(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview == null)
        {
            return;
        }

        RectTransform previewRectTransform = dragPreview.GetComponent<RectTransform>();

        if (previewRectTransform != null)
        {
            previewRectTransform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CleanupDragVisuals();
    }

    public void CleanupDragVisuals()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }

        if (CurrentDraggedButton == this)
        {
            CurrentDraggedButton = null;
        }
    }

    private void CreateDragPreview(Vector2 startPosition)
    {
        CleanupOldDragPreviews();

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (rootCanvas == null)
        {
            return;
        }

        dragPreview = new GameObject("DragPreview_" + cardData.cardName);
        dragPreview.transform.SetParent(rootCanvas.transform, false);

        RectTransform previewRect = dragPreview.AddComponent<RectTransform>();
        RectTransform originalRect = GetComponent<RectTransform>();

        if (originalRect != null)
        {
            previewRect.sizeDelta = originalRect.rect.size;
        }
        else
        {
            previewRect.sizeDelta = new Vector2(800f, 120f);
        }

        previewRect.position = startPosition;

        Image previewImage = dragPreview.AddComponent<Image>();
        Image originalImage = GetComponent<Image>();

        if (originalImage != null)
        {
            previewImage.sprite = originalImage.sprite;
            previewImage.type = originalImage.type;
            previewImage.color = new Color(
                originalImage.color.r,
                originalImage.color.g,
                originalImage.color.b,
                0.65f
            );
        }
        else
        {
            previewImage.color = new Color(1f, 1f, 1f, 0.65f);
        }

        previewImage.raycastTarget = false;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(dragPreview.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI previewText = textObject.AddComponent<TextMeshProUGUI>();
        previewText.text = labelText != null ? labelText.text : cardData.cardName;
        previewText.alignment = TextAlignmentOptions.Center;
        previewText.fontSize = labelText != null ? labelText.fontSize : 18f;
        previewText.color = Color.black;
        previewText.raycastTarget = false;
    }

    private void CleanupOldDragPreviews()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (rootCanvas == null)
        {
            return;
        }

        for (int i = rootCanvas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = rootCanvas.transform.GetChild(i);

            if (child.name.StartsWith("DragPreview_"))
            {
                Destroy(child.gameObject);
            }
        }
    }
}
