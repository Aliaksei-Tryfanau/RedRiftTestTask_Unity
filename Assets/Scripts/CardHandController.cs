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
    [SerializeField] private Transform cardHandCenter;
    [SerializeField] private Transform cardRotationCenter;
    [SerializeField] private AnimationCurve cardsVerticalPlacementCurve;
    [SerializeField] private AnimationCurve cardsHorizontalPlacementCurve;
    [SerializeField] private float maxCardHandHeight = 0.5f;
    [SerializeField] private float maxCardHandLength = 4f;
    [SerializeField] private float additionalCardRotation = 6f;
    [SerializeField] private int minNumberOfCards = 2;
    [SerializeField] private int maxNumberOfCards = 6;
    [SerializeField] private int minStatValue = -2;
    [SerializeField] private int maxStatValue = 9;
    [SerializeField] private float cardsRearrangeTime = 0.5f;
    [SerializeField] private float cardMoveSpeed = 5f;
    [SerializeField] private LayerMask cardLayer;
    [SerializeField] private LayerMask groundLayer;

    private List<CardController> cardControllers = new List<CardController>();
    private int lastChangedCardIndex = 0;
    private bool isCardStatsChanging;
    private CardController selectedCard;
    private Vector3 selectedCardInitialPosition;
    private Vector3 selectedCardInitialRotation;
    private int selectedCardSiblingIndex;

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
            var cardEvaluationStep = 1f / (numberOfCards + 1);
            var cardSpawnPos = new Vector3(
                cardHandCenter.position.x + maxCardHandLength * cardsHorizontalPlacementCurve.Evaluate(cardEvaluationStep + cardEvaluationStep * i), 
                cardHandCenter.position.y + maxCardHandHeight * cardsVerticalPlacementCurve.Evaluate(cardEvaluationStep + cardEvaluationStep * i),
                cardsRoot.position.z);
            var cardController = Instantiate(cardControllerPrefab, cardSpawnPos, Quaternion.identity, cardsRoot);
            var cardTransform = cardController.transform;
            var cardSpawnRotation = Quaternion.Euler(0f, 0f, additionalCardRotation) * (cardTransform.position - cardRotationCenter.position);
            cardTransform.rotation = Quaternion.FromToRotation(Vector3.up, cardSpawnRotation);
            cardController.SetInfo(cardStatsSO.StartingAttack, cardStatsSO.StartingHealth, cardStatsSO.StartingMana, cardSprite);
            cardController.EventCardDestroyed += OnCardDestroyed;
            cardController.EventStatChangeComplete += OnCardStatChangeCompleted;
            cardControllers.Add(cardController);
        }
    }

    private void OnDisable()
    {
        foreach (var cardController in cardControllers)
        {
            cardController.EventCardDestroyed -= OnCardDestroyed;
            cardController.EventStatChangeComplete -= OnCardStatChangeCompleted;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f, cardLayer))
            {
                var cardController = hit.collider.GetComponent<CardController>();

                if (cardController != null)
                {
                    selectedCard = cardController;
                    var selectedCardTransform = selectedCard.transform;
                    selectedCardInitialPosition = selectedCardTransform.position;
                    selectedCardInitialRotation = selectedCardTransform.rotation.eulerAngles;
                    selectedCardSiblingIndex = selectedCardTransform.GetSiblingIndex();
                    selectedCard.transform.SetAsLastSibling();
                    selectedCard.transform.DORotate(Vector3.zero, cardsRearrangeTime);
                    selectedCard.SetSelected(true);
                }
            }
        }
        else if (Input.GetMouseButtonUp(0) && selectedCard != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                cardControllers.Remove(selectedCard);
                selectedCard.transform.DOMove(hit.point + new Vector3(0f, 0.0001f, 0f), cardsRearrangeTime);
                selectedCard.transform.DORotate(hit.collider.transform.rotation.eulerAngles, cardsRearrangeTime);
                RearrangeCards();
            }
            else
            {
                selectedCard.transform.SetSiblingIndex(selectedCardSiblingIndex);
                selectedCard.transform.DOMove(selectedCardInitialPosition, cardsRearrangeTime);
                selectedCard.transform.DORotate(selectedCardInitialRotation, cardsRearrangeTime);
            }

            selectedCard.SetSelected(false);
            selectedCard = null;
        }
        else if (selectedCard != null)
        {
            var mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 
                Mathf.Abs((Camera.main.transform.position - cardsRoot.position).z)));
            var selectedCardTransform = selectedCard.transform;
            selectedCardTransform.position = Vector3.Lerp(selectedCard.transform.position, 
                new Vector3(mousePosition.x, mousePosition.y, selectedCardTransform.position.z), cardMoveSpeed * Time.deltaTime);
        }
    }

    public void ChangeCardInfo()
    {
        if (cardControllers.Count == 0 || isCardStatsChanging)
        {
            return;
        }

        if (lastChangedCardIndex >= cardControllers.Count)
        {
            lastChangedCardIndex = 0;
        }

        isCardStatsChanging = true;
        cardControllers[lastChangedCardIndex].ChangeStats(Random.Range(minStatValue, maxStatValue + 1));
        lastChangedCardIndex++;
    }

    private void OnCardDestroyed(CardController cardControllerArg)
    {
        cardControllerArg.EventCardDestroyed -= OnCardDestroyed;
        cardControllers.Remove(cardControllerArg);
        Destroy(cardControllerArg.gameObject);
        RearrangeCards();
    }

    private void RearrangeCards()
    {
        for (int i = 0; i < cardControllers.Count; i++)
        {
            var cardEvaluationStep = 1f / (cardControllers.Count + 1);
            var newPosition = new Vector3(
                cardHandCenter.position.x + maxCardHandLength * cardsHorizontalPlacementCurve.Evaluate(cardEvaluationStep + cardEvaluationStep * i),
                cardHandCenter.position.y + maxCardHandHeight * cardsVerticalPlacementCurve.Evaluate(cardEvaluationStep + cardEvaluationStep * i),
                cardsRoot.position.z);
            var cardTransform = cardControllers[i].transform;
            var newRotationDirection = Quaternion.Euler(0f, 0f, additionalCardRotation) * (newPosition - cardRotationCenter.position);
            var newRotation = Quaternion.FromToRotation(Vector3.up, newRotationDirection);
            cardTransform.DOMove(newPosition, cardsRearrangeTime);
            cardTransform.DORotateQuaternion(newRotation, cardsRearrangeTime);
        }
    }

    private void OnCardStatChangeCompleted()
    {
        isCardStatsChanging = false;
    }
}
