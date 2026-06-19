using TMPro;
using UnityEngine;

public class CoinHud : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;

    private void OnEnable()
    {
        CoinService.CoinsChanged += Refresh;
    }

    private void OnDisable()
    {
        CoinService.CoinsChanged -= Refresh;
    }

    // Start (not OnEnable) so it runs after CoinService.Awake has set Instance.
    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (coinText != null && CoinService.Instance != null)
            coinText.text = CoinService.Instance.Coins.ToString();
    }
}
