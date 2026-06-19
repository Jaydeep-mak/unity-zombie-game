using UnityEngine;

public abstract class TrapPlantBase : PlantBase {
    protected bool isTriggered = false;

    protected override void Start() {
        // Skip random attack timer offset since traps do not attack periodically
        base.Start();

        var bc = GetComponent<BoxCollider2D>();
        if (bc != null) {
            bc.size = new Vector2(1f, 1f);
            bc.isTrigger = true;
        }
    }

    protected override void Update() {
        // Trap plants do not perform lane range detection or shoot periodic projectiles
    }

    protected override GameObject DetectZombieInLane() {
        // Trap plants only trigger when a zombie physically reaches the trap's grid cell
        return null;
    }

    protected override void Attack(GameObject target) {
        // Trap plants do not use periodic ranged lane attacks
    }

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        if (isTriggered) return;

        var zombie = other.GetComponent<ZombieController>();
        if (zombie != null) {
            isTriggered = true;
            OnTrapTriggered(zombie);
        }
    }

    // Extensible callback for specific trap behaviors
    protected abstract void OnTrapTriggered(ZombieController zombie);

    protected virtual void ConsumeTrap() {
        // Free the grid cell for future placements
        FreeGridCell();

        // Destroy the trap plant GameObject
        Destroy(gameObject);
    }

    private void FreeGridCell() {
        var cells = FindObjectsByType<GridCell>(FindObjectsSortMode.None);
        foreach (var cell in cells) {
            if (cell.placedPlant == gameObject) {
                cell.isOccupied = false;
                cell.placedPlant = null;
                cell.ResetHighlight();
                break;
            }
        }
    }
}
