using UnityEngine;
using System.Collections;

public class PlantPlacementManager : MonoBehaviour {
    public static PlantPlacementManager Instance { get; private set; }

    [System.Serializable]
    public class PlantSlotData {
        public string name;
        public int cost;
        public Color tintColor = Color.white;
        public bool isLocked = false;
        
        [Header("Plant Config Parameters")]
        public int damage = 2;
        public float attackInterval = 1.5f;
        public float projectileSpeed = 5f;
        public float cooldown = 5f;
    }

    [Header("Placement References")]
    [SerializeField] private GameObject plantPrefab;
    [SerializeField] private Transform gridParent;
    [SerializeField] private PlantSlotData[] slots;

    [Header("Preview Settings")]
    [SerializeField] private Color validPreviewColor = new Color(0.2f, 1.0f, 0.2f, 0.6f);
    [SerializeField] private Color invalidPreviewColor = new Color(1.0f, 0.2f, 0.2f, 0.6f);

    private int selectedSlotIndex = -1;
    private GameObject previewGo;
    private SpriteRenderer previewRenderer;
    private GridCell hoveredCell;

    public int SlotsCount => slots != null ? slots.Length : 0;

    public bool IsSlotLocked(int index) {
        if (slots != null && index >= 0 && index < slots.Length) {
            return slots[index].isLocked;
        }
        return true;
    }

    public int GetSlotCost(int index) {
        if (slots != null && index >= 0 && index < slots.Length) {
            return slots[index].cost;
        }
        return 0;
    }

    public string GetSlotName(int index) {
        if (slots != null && index >= 0 && index < slots.Length) {
            return slots[index].name;
        }
        return "Unknown";
    }

    public Color GetSlotTintColor(int index) {
        if (slots != null && index >= 0 && index < slots.Length) {
            return slots[index].tintColor;
        }
        return Color.white;
    }

    private void Start() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }

        // Bind initial plants placed in the scene
        BindInitialPlants();
    }

    private void BindInitialPlants() {
        var cells = FindObjectsByType<GridCell>(FindObjectsSortMode.None);
        var plants = FindObjectsByType<PlantBase>(FindObjectsSortMode.None);

        foreach (var cell in cells) {
            foreach (var plant in plants) {
                if (Vector2.Distance(cell.transform.position, plant.transform.position) < 0.1f) {
                    cell.isOccupied = true;
                    cell.placedPlant = plant.gameObject;
                    break;
                }
            }
        }
    }

    public void SelectPlant(int slotIndex) {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return;

        var data = slots[slotIndex];
        
        if (data.isLocked) {
            Debug.Log(data.name + " is locked!");
            return;
        }

        if (GameplayManager.Instance != null && GameplayManager.Instance.IsSlotOnCooldown(slotIndex)) {
            Debug.Log(data.name + " is on cooldown!");
            return;
        }

        if (GameplayManager.Instance != null && GameplayManager.Instance.Coins < data.cost) {
            Debug.Log("Not enough coins for " + data.name + "! Cost: " + data.cost);
            return;
        }

        selectedSlotIndex = slotIndex;

        if (GameplayManager.Instance != null) {
            GameplayManager.Instance.SetSlotHighlight(slotIndex);
        }

        CreatePreview(data);
    }

    public void CancelSelection() {
        selectedSlotIndex = -1;
        if (GameplayManager.Instance != null) {
            GameplayManager.Instance.SetSlotHighlight(-1);
        }
        DestroyPreview();
        if (hoveredCell != null) {
            hoveredCell.ResetHighlight();
            hoveredCell = null;
        }
    }

    private void CreatePreview(PlantSlotData data) {
        DestroyPreview();

        previewGo = new GameObject("PlantPlacementPreview");
        previewRenderer = previewGo.AddComponent<SpriteRenderer>();

        Sprite customSprite = PlantVisuals.GetPlantSprite(data.name);
        if (customSprite != null) {
            previewRenderer.sprite = customSprite;
            previewGo.transform.localScale = plantPrefab != null ? plantPrefab.transform.localScale : Vector3.one;
        } else if (plantPrefab != null) {
            var prefabSr = plantPrefab.GetComponent<SpriteRenderer>();
            if (prefabSr != null) {
                previewRenderer.sprite = prefabSr.sprite;
                previewGo.transform.localScale = plantPrefab.transform.localScale;
            }
        }

        previewRenderer.color = invalidPreviewColor;
        previewRenderer.sortingOrder = 10; // Overlay order
    }

    private void DestroyPreview() {
        if (previewGo != null) {
            Destroy(previewGo);
            previewGo = null;
            previewRenderer = null;
        }
    }

    private void Update() {
        if (selectedSlotIndex == -1) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
        GridCell cell = null;

        if (hit.collider != null) {
            cell = hit.collider.GetComponent<GridCell>();
        }

        if (cell != null) {
            if (hoveredCell != cell) {
                if (hoveredCell != null) hoveredCell.ResetHighlight();
                hoveredCell = cell;
            }

            if (previewGo != null) {
                previewGo.transform.position = cell.transform.position;
            }

            if (!cell.isOccupied) {
                cell.SetHighlight(new Color(0.2f, 1.0f, 0.2f, 0.4f)); // Green highlight
                if (previewRenderer != null) {
                    previewRenderer.color = validPreviewColor;
                }

                if (Input.GetMouseButtonDown(0)) {
                    PlacePlantOnCell(cell, slots[selectedSlotIndex]);
                }
            } else {
                cell.SetHighlight(new Color(1.0f, 0.2f, 0.2f, 0.4f)); // Red highlight
                if (previewRenderer != null) {
                    previewRenderer.color = invalidPreviewColor;
                }
            }
        } else {
            if (hoveredCell != null) {
                hoveredCell.ResetHighlight();
                hoveredCell = null;
            }

            if (previewGo != null) {
                previewGo.transform.position = mouseWorldPos;
                if (previewRenderer != null) {
                    previewRenderer.color = invalidPreviewColor;
                }
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) {
            CancelSelection();
        }
    }

    private void PlacePlantOnCell(GridCell cell, PlantSlotData data) {
        if (GameplayManager.Instance != null && GameplayManager.Instance.UseCoins(data.cost)) {
            GameObject plantGo = Instantiate(plantPrefab, cell.transform.position, Quaternion.identity);

            // Extensible Trap/Plant Component Mapping
            PlantBase plant = null;
            GameObject projPrefab = null;
            var oldPlant = plantGo.GetComponent<SingleShotPlant>();
            if (oldPlant != null) {
                projPrefab = oldPlant.ProjectilePrefab;
            }

            if (data.name != null && (data.name.Contains("Ice") || data.name.Contains("Frost") || data.name.Contains("Trap"))) {
                if (oldPlant != null) {
                    DestroyImmediate(oldPlant);
                }
                
                if (data.name.Contains("Ice") || data.name.Contains("Frost")) {
                    plant = plantGo.AddComponent<FreezeTrapPlant>();
                }
            } else if (data.name != null && (data.name.Contains("Thorn") || data.name.Contains("Vine"))) {
                if (oldPlant != null) {
                    DestroyImmediate(oldPlant);
                }
                plant = plantGo.AddComponent<ThornVinePlant>();
            } else if (data.name != null && (data.name.Contains("Bomb") || data.name.Contains("Cactus"))) {
                if (oldPlant != null) {
                    DestroyImmediate(oldPlant);
                }
                plant = plantGo.AddComponent<BombCactusPlant>();
            } else if (data.name != null && (data.name.Contains("Magic") || data.name.Contains("Blossom"))) {
                if (oldPlant != null) {
                    DestroyImmediate(oldPlant);
                }
                plant = plantGo.AddComponent<MagicBlossomPlant>();
            } else {
                plant = plantGo.GetComponent<PlantBase>();
            }

            if (plant != null) {
                if (projPrefab != null) {
                    plant.ProjectilePrefab = projPrefab;
                }
                plant.Configure(data.damage, data.attackInterval, data.projectileSpeed, data.tintColor, data.name);
            }

            cell.isOccupied = true;
            cell.placedPlant = plantGo;

            cell.ResetHighlight();
            
            // Trigger cooldown for this slot
            float cooldownTime = data.cooldown > 0f ? data.cooldown : 5f;
            if (GameplayManager.Instance != null) {
                GameplayManager.Instance.StartSlotCooldown(selectedSlotIndex, cooldownTime);
            }

            CancelSelection();

            Debug.Log("Successfully placed " + data.name + " on Row: " + cell.row + ", Col: " + cell.column);
        } else {
            Debug.Log("Failed placement: Insufficient coins!");
            CancelSelection();
        }
    }
}
