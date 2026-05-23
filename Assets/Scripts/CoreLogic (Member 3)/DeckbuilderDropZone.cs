using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DeckbuilderDropZoneType
{
    Collection,
    Deck
}

public class DeckbuilderDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Drop Zone Settings")]
    public DeckbuilderDropZoneType zoneType;
    public DeckBuilderManager deckBuilderManager;

    [Header("Visual Feedback")]
    public bool useHighlight = true;

    private Image zoneImage;
    private Color originalColor;

    private void Awake()
    {
        zoneImage = GetComponent<Image>();

        if (zoneImage != null)
        {
            originalColor = zoneImage.color;
        }

        if (deckBuilderManager == null)
        {
            deckBuilderManager = FindAnyObjectByType<DeckBuilderManager>();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        DeckbuilderCardButton draggedCardButton = DeckbuilderCardButton.CurrentDraggedButton;

        if (draggedCardButton == null && eventData.pointerDrag != null)
        {
            draggedCardButton = eventData.pointerDrag.GetComponentInParent<DeckbuilderCardButton>();
        }

        if (draggedCardButton == null)
        {
            ResetHighlight();
            return;
        }

        DeckBuilderManager manager = draggedCardButton.DeckBuilderManager;

        if (manager == null)
        {
            manager = deckBuilderManager;
        }

        if (manager == null || draggedCardButton.CardData == null)
        {
            draggedCardButton.CleanupDragVisuals();
            ResetHighlight();
            return;
        }

        CardData droppedCard = draggedCardButton.CardData;
        bool cameFromDeck = draggedCardButton.IsDeckCard;

        draggedCardButton.CleanupDragVisuals();

        if (zoneType == DeckbuilderDropZoneType.Deck && !cameFromDeck)
        {
            manager.AddCardToDeck(droppedCard);
        }
        else if (zoneType == DeckbuilderDropZoneType.Collection && cameFromDeck)
        {
            manager.RemoveCardFromDeck(droppedCard);
        }

        ResetHighlight();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!useHighlight || zoneImage == null)
        {
            return;
        }

        if (DeckbuilderCardButton.CurrentDraggedButton == null)
        {
            return;
        }

        zoneImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            Mathf.Clamp01(originalColor.a + 0.25f)
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHighlight();
    }

    private void ResetHighlight()
    {
        if (zoneImage != null)
        {
            zoneImage.color = originalColor;
        }
    }
}
