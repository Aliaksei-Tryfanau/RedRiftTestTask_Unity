using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using DG.Tweening;

public class CardHandController : MonoBehaviour
{
    [SerializeField] private CardController cardControllerPrefab;
    [SerializeField] private Transform cardsRoot;
    [SerializeField] private CardStatsSO cardStatsSO;
    [SerializeField] private int minNumberOfCards = 2;
    [SerializeField] private int maxNumberOfCards = 6;
    [SerializeField] private int minStatValue = -2;
    [SerializeField] private int maxStatValue = 9;

    private List<CardController> cardControllers = new List<CardController>();
    private int lastChangedCardIndex = 0;

    private const string SPRITE_DOWNLOAD_URL = "https://picsum.photos/200";

    private IEnumerator Start()
    {
        DOTween.Init();
        Sprite cardSprite;
        UnityWebRequest spriteRequest = UnityWebRequestTexture.GetTexture(SPRITE_DOWNLOAD_URL);

        yield return spriteRequest.SendWebRequest();

        if (spriteRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download card sprite");
            yield break;
        }
        else
        {
            var texture = DownloadHandlerTexture.GetContent(spriteRequest);
            cardSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        var numberOfCards = Random.Range(minNumberOfCards, maxNumberOfCards + 1);
        for (int i = 0; i < numberOfCards; i++)
        {
            var cardController = Instantiate(cardControllerPrefab, cardsRoot);
            cardController.SetInfo(cardStatsSO.StartingAttack, cardStatsSO.StartingHealth, cardStatsSO.StartingMana, cardSprite);
            cardController.EventCardDestroyed += OnCardDestroyed;
            cardControllers.Add(cardController);
        }
    }

    private void OnDisable()
    {
        foreach (var cardController in cardControllers)
        {
            cardController.EventCardDestroyed -= OnCardDestroyed;
        }
    }

    public void ChangeCardInfo()
    {
        if (cardControllers.Count == 0)
        {
            return;
        }

        cardControllers[lastChangedCardIndex].ChangeStats(Random.Range(minStatValue, maxStatValue + 1));
        lastChangedCardIndex++;

        if (lastChangedCardIndex >= cardControllers.Count)
        {
            lastChangedCardIndex = 0;
        }
    }

    private void OnCardDestroyed(CardController cardControllerArg)
    {
        cardControllerArg.EventCardDestroyed -= OnCardDestroyed;
        cardControllers.Remove(cardControllerArg);
        Destroy(cardControllerArg.gameObject);
        //rearrange remaining cards
    }
}
