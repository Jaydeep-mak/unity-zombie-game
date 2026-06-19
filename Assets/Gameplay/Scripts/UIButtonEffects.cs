using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float clickScale = 0.92f;
    [SerializeField] private float animationDuration = 0.12f;

    private UnityEngine.UI.Button button;

    private void Awake() {
        originalScale = transform.localScale;
        button = GetComponent<UnityEngine.UI.Button>();
    }

    private bool IsInteractable() {
        return button == null || button.interactable;
    }

    private void OnDisable() {
        if (scaleCoroutine != null) {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!IsInteractable()) return;
        AnimateScale(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (!IsInteractable()) return;
        AnimateScale(originalScale);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (!IsInteractable()) return;
        AnimateScale(originalScale * clickScale);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (!IsInteractable()) return;
        AnimateScale(originalScale * hoverScale);
    }

    private void AnimateScale(Vector3 targetScale) {
        if (scaleCoroutine != null) {
            StopCoroutine(scaleCoroutine);
        }
        if (gameObject.activeInHierarchy) {
            scaleCoroutine = StartCoroutine(ScaleRoutine(targetScale));
        } else {
            transform.localScale = targetScale;
        }
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale) {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < animationDuration) {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time so it works even when game is paused!
            float t = elapsed / animationDuration;
            // Smooth ease out
            t = t * (2f - t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        transform.localScale = targetScale;
        scaleCoroutine = null;
    }
}
