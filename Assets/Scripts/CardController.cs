using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Random = UnityEngine.Random;

public class CardController : MonoBehaviour
{
    public event Action<CardController> EventCardDestroyed;

    [SerializeField] private Image cardGlow;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI attackValueText;
    [SerializeField] private TextMeshProUGUI healthValueText;
    [SerializeField] private TextMeshProUGUI manaValueText;
    [SerializeField] private float statChangeTime = 1f;

    private int attackValue;
    private int healthValue;
    private int manaValue;

    public void SetInfo(int attackArg, int healthArg, int manaArg, Sprite cardImageArg)
    {
        attackValue = attackArg;
        attackValueText.text = attackValue.ToString();
        healthValue = healthArg;
        healthValueText.text = healthValue.ToString();
        manaValue = manaArg;
        manaValueText.text = manaValue.ToString();
        cardImage.sprite = cardImageArg;
    }

    public void ChangeStats(int newStatValue)
    {
        var randomStatIndex = Random.Range(0, 3);

        switch (randomStatIndex)
        {
            case 0:
                DOTween.To(() => attackValue, x => attackValue = x, newStatValue, statChangeTime).OnUpdate(() =>
                {
                    attackValueText.text = attackValue.ToString();
                });
                break;
            case 1:
                DOTween.To(() => healthValue, x => healthValue = x, newStatValue, statChangeTime).OnUpdate(() => 
                { 
                    healthValueText.text = healthValue.ToString(); 
                }).OnComplete(() =>
                {
                    if (healthValue <= 0)
                    {
                        EventCardDestroyed.Invoke(this);
                    }
                });
                break;
            case 2:
                DOTween.To(() => manaValue, x => manaValue = x, newStatValue, statChangeTime).OnUpdate(() =>
                {
                    manaValueText.text = manaValue.ToString();
                });
                break;
            default:
                Debug.LogError("Random value outside stat count");
                break;
        }
    }
}
