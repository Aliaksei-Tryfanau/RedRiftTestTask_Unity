using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "ScriptableObjects/CardDataScriptableObject", order = 1)]
public class CardStatsSO : ScriptableObject
{
    public int StartingAttack = 4;
    public int StartingHealth = 5;
    public int StartingMana = 6;
}
