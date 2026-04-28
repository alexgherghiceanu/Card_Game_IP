using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card Game/Card Data")]
public class CardData : ScriptableObject
{
    public string cardID;
    public string cardName;
    public int manaCost;
    public int attack;
    public int hp;
    public string cardClass;

    public string flavorText;
    public Sprite artwork;  
}