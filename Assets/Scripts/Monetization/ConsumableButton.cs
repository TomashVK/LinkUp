using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ConsumableButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string consumableId;
    [SerializeField] private int coinCost;
    [SerializeField] private TMP_Text badgeText;
    [SerializeField] private GameObject adIcon;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private UnityEvent onGranted;

    public System.Func<bool> CanActivate;
    public string ConsumableId => consumableId;

    private int freeUsesRemaining;

    // Free uses come from the current level's data, not a fixed inspector value —
    // GameController calls this once per level load.
    public void Init(int freeUses)
    {
        freeUsesRemaining = freeUses;
        RefreshDisplay();
    }

    private void OnEnable()
    {
        CoinService.CoinsChanged += RefreshDisplay;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        CoinService.CoinsChanged -= RefreshDisplay;
    }

    public void OnPointerClick(PointerEventData eventData) => RequestUse();

    private void RequestUse()
    {
        if (CanActivate != null && !CanActivate()) return;

        if (freeUsesRemaining > 0)
        {
            freeUsesRemaining--;
            RefreshDisplay();
            onGranted?.Invoke();
            return;
        }

        if (CoinService.Instance != null && CoinService.Instance.TrySpend(coinCost))
        {
            RefreshDisplay();
            onGranted?.Invoke();
            return;
        }

        AdService.ShowRewardedAd(() => onGranted?.Invoke());
    }

    private void RefreshDisplay()
    {
        bool hasFreeUses = freeUsesRemaining > 0;
        bool hasEnoughCoins = CoinService.Instance != null && CoinService.Instance.Coins >= coinCost;

        if (badgeText != null) badgeText.gameObject.SetActive(hasFreeUses);
        if (badgeText != null) badgeText.text = freeUsesRemaining.ToString();
        if (costLabel != null) costLabel.gameObject.SetActive(!hasFreeUses);
        if (costLabel != null) costLabel.text = coinCost.ToString();
        if (adIcon != null) adIcon.SetActive(!hasFreeUses && !hasEnoughCoins);
    }
}
