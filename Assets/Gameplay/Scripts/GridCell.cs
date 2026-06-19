using UnityEngine;

public class GridCell : MonoBehaviour {
    public int row;
    public int column;
    public bool isOccupied = false;
    public GameObject placedPlant = null;

    private SpriteRenderer sr;
    private Color originalColor;

    public Color OriginalColor {
        get { return originalColor; }
        set {
            originalColor = value;
            if (sr != null) sr.color = value;
        }
    }

    private void Awake() {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            originalColor = sr.color;
        }
    }

    public void SetHighlight(Color color) {
        if (sr != null) {
            sr.color = color;
        }
    }

    public void ResetHighlight() {
        if (sr != null) {
            sr.color = originalColor;
        }
    }
}
