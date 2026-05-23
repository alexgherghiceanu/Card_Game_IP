using UnityEngine;

public class StoreButton : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnClickBuyPack()
    {
        if (PlayFabInventoryManager.Instance != null)
        {
            PlayFabInventoryManager.Instance.BuyBoosterPack();
        }
        else
        {
            Debug.LogError("Nu gasesc PlayFabInventoryManager! Te-ai logat inainte?");
        }
    }
}
