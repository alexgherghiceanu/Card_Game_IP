using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragNDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] 
    public Transform parentAfterDrag; // Where the card will return if not dropped on a valid target
    
    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // CLick before dragging starts
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Trag de carte!");
        
        parentAfterDrag = transform.parent; 
        
        // Temporarily move the card to the root of the scene so it can be dragged freely without being clipped by its original parent
        transform.SetParent(transform.root); 
        transform.SetAsLastSibling(); 

        // Make the card ignore raycasts so it doesn't block other UI elements while dragging
        canvasGroup.blocksRaycasts = false; 
        
        // Rescale the card to make it more visible while dragging
        transform.localScale = Vector3.one; 
    }

    // While dragging
    public void OnDrag(PointerEventData eventData)
    {
        // Card follows the mouse position
        transform.position = Input.mousePosition; 
    }

    // When mouse button is released
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Am dat drumul la carte!");
        
        transform.SetParent(parentAfterDrag); 
        
        canvasGroup.blocksRaycasts = true; 
    }
}