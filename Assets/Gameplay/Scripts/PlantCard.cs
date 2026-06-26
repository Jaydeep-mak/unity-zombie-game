using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlantCard : MonoBehaviour {
    [SerializeField] private Image glowImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private Image cardBg;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private TextMeshProUGUI nameText;
    
    private float currentCooldown = 0f;
    private float maxCooldown = 0f;
    private bool isSelected = false;
    private bool isLocked = false;
    private bool isAffordable = true;
    
    private Vector3 originalScale = Vector3.one;
    
    [HideInInspector] public int realSlotIndex = 0;

    public void Initialize(Image bg, Image glow, Image cdOverlay, TextMeshProUGUI cdText, Image icon, TextMeshProUGUI cost, GameObject lockOvl, bool locked, TextMeshProUGUI nameTxt) {
        cardBg = bg;
        glowImage = glow;
        cooldownOverlay = cdOverlay;
        cooldownText = cdText;
        iconImage = icon;
        costText = cost;
        lockOverlay = lockOvl;
        isLocked = locked;
        nameText = nameTxt;
        originalScale = transform.localScale;

        if (glowImage != null) glowImage.gameObject.SetActive(false);
        if (cooldownOverlay != null) {
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Vertical;
            cooldownOverlay.fillAmount = 0f;
        }
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (lockOverlay != null) lockOverlay.SetActive(isLocked);
        
        UpdateVisualTint();
    }

    public void InitializeRuntime(bool locked, string cleanName, int cost, Sprite plantSprite, Color slotTint) {
        isLocked = locked;
        originalScale = transform.localScale;

        if (glowImage != null) glowImage.gameObject.SetActive(isSelected);
        if (cooldownOverlay != null) {
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Vertical;
            cooldownOverlay.fillAmount = currentCooldown > 0f ? currentCooldown / maxCooldown : 0f;
        }
        if (cooldownText != null) {
            cooldownText.gameObject.SetActive(currentCooldown > 0f);
            if (currentCooldown > 0f) {
                cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
            }
        }
        if (lockOverlay != null) lockOverlay.SetActive(isLocked);
        if (nameText != null) nameText.text = isLocked ? "Locked" : cleanName;
        if (costText != null) costText.text = isLocked ? "Soon" : $"Cost: {cost}";
        if (iconImage != null) {
            if (!isLocked) {
                iconImage.sprite = plantSprite;
                iconImage.color = plantSprite != null ? Color.white : slotTint;
            } else {
                iconImage.sprite = plantSprite;
                iconImage.color = new Color(0.3f, 0.3f, 0.3f, 0.2f);
            }
        }

        UpdateVisualTint();
    }

    public void SetSelected(bool selected) {
        isSelected = selected;
        if (glowImage != null) {
            glowImage.gameObject.SetActive(selected);
        }
        StopAllCoroutines();
        StartCoroutine(ScaleCard(selected ? originalScale * 1.12f : originalScale));
    }

    private System.Collections.IEnumerator ScaleCard(Vector3 targetScale) {
        Vector3 startScale = transform.localScale;
        float duration = 0.1f;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    public void StartCooldown(float duration) {
        maxCooldown = duration;
        currentCooldown = duration;
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 1f;
        if (cooldownText != null) {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
        }
        UpdateVisualTint();
    }

    public bool IsOnCooldown() {
        return currentCooldown > 0f;
    }

    public void SetAffordable(bool affordable) {
        isAffordable = affordable;
        UpdateVisualTint();
    }

    private void UpdateVisualTint() {
        if (isLocked) {
            if (cardBg != null) cardBg.color = new Color(0.3f, 0.3f, 0.35f, 0.6f);
            if (iconImage != null) iconImage.color = new Color(0.3f, 0.3f, 0.3f, 0.2f);
            if (nameText != null) {
                nameText.text = "Locked";
                nameText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            if (costText != null) {
                costText.text = "Soon";
                costText.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            }
            return;
        }
        
        Color tint = Color.white;
        if (currentCooldown > 0f) {
            tint = new Color(0.5f, 0.5f, 0.5f, 1f);
        } else if (!isAffordable) {
            tint = new Color(1.0f, 0.4f, 0.4f, 1f); // Reddish if unaffordable
        }
        
        if (cardBg != null) cardBg.color = tint;
        if (iconImage != null) iconImage.color = new Color(tint.r, tint.g, tint.b, currentCooldown > 0f ? 0.5f : 1f);
    }

    private void Update() {
        if (isSelected && glowImage != null) {
            float pulse = 0.75f + Mathf.PingPong(Time.time * 2f, 0.25f);
            glowImage.color = new Color(1f, 0.85f, 0.2f, pulse);
        }

        if (currentCooldown > 0f) {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0f) {
                currentCooldown = 0f;
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
                if (cooldownText != null) cooldownText.gameObject.SetActive(false);
                UpdateVisualTint();
            } else {
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = currentCooldown / maxCooldown;
                if (cooldownText != null) cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
            }
        }
    }
}
