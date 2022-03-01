using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {
    enum OptionalToggle {
        Ignore = 0,
        Yes,
        No
    }

    private OptionalToggle riverMode, roadMode, walledMode;

    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;

    private int activeElevation;

    private int activeWaterLevel;

    private int activeUrbanLevel, activeFarmLevel, activePlantLevel;

    private bool applyColor, applyElevation = true, applyWaterLevel = true;

    bool applyUrbanLevel, applyFarmLevel, applyPlantLevel;

    int brushSize = 0;

    private bool isDrag;
    private HexDirection dragDirection;
    private HexCell previousCell;

    private void Awake() {
        SelectColor(0);
    }

    void Update() {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) {
            HandleInput();
        } else {
            previousCell = null;
        }
    }

    void HandleInput() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if (previousCell && previousCell != currentCell) {
                ValidateDrag(currentCell);
            } else {
                isDrag = false;
            }

            EditCells(currentCell);
            previousCell = currentCell;
        } else {
            previousCell = null;
        }
    }

    void ValidateDrag(HexCell currentCell) {
        for (
            dragDirection = HexDirection.NE;
            dragDirection <= HexDirection.NW;
            dragDirection++
        ) {
            if (previousCell.GetNeighbor(dragDirection) == currentCell) {
                isDrag = true;
                return;
            }
        }

        isDrag = false;
    }

    void EditCells(HexCell center) {
        if (!applyColor && !applyElevation)
            return;

        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
            for (int x = centerX - r; x <= centerX + brushSize; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
            for (int x = centerX - brushSize; x <= centerX + r; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    void EditCell(HexCell cell) {
        if (cell == null)
            return;

        if (applyColor) {
            cell.Color = activeColor;
        }

        if (applyElevation) {
            cell.Elevation = activeElevation;
        }

        if (applyWaterLevel) {
            cell.WaterLevel = activeWaterLevel;
        }

        if (applyUrbanLevel) {
            cell.UrbanLevel = activeUrbanLevel;
        }

        if (applyFarmLevel) {
            cell.FarmLevel = activeFarmLevel;
        }

        if (applyPlantLevel) {
            cell.PlantLevel = activePlantLevel;
        }

        if (riverMode == OptionalToggle.No) {
            cell.RemoveRiver();
        }

        if (roadMode == OptionalToggle.No) {
            cell.RemoveRoads();
        }

        if (walledMode != OptionalToggle.Ignore) {
            cell.Walled = walledMode == OptionalToggle.Yes;
        }

        if (isDrag) {
            // if want to ignore brush size, here use previousCell instead of otherCell.
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell) {
                if (riverMode == OptionalToggle.Yes) {
                    otherCell.SetOutgoingRiver(dragDirection);
                }

                if (roadMode == OptionalToggle.Yes) {
                    otherCell.AddRoad(dragDirection);
                }
            }
        }
    }

    public void SelectColor(int index) {
        applyColor = index >= 0;
        if (applyColor) {
            activeColor = colors[index];
        }
    }

    public void SetElevation(float elevation) {
        activeElevation = (int) elevation;
    }

    public void SetApplyElevation(bool toggle) {
        applyElevation = toggle;
    }

    public void SetWaterLevel(float level) {
        activeWaterLevel = (int) level;
    }

    public void SetApplyWaterLevel(bool toggle) {
        applyWaterLevel = toggle;
    }

    public void SetRiverMode(int mode) {
        riverMode = (OptionalToggle) mode;
    }

    public void SetRoadMode(int mode) {
        roadMode = (OptionalToggle) mode;
    }

    public void SetBrushSize(float size) {
        brushSize = (int) size;
    }

    public void ShowUI(bool visible) {
        hexGrid.ShowUI(visible);
    }

    public void SetApplyUrbanLevel(bool toggle) {
        applyUrbanLevel = toggle;
    }

    public void SetUrbanLevel(float level) {
        activeUrbanLevel = (int) level;
    }

    public void SetApplyFarmLevel(bool toggle) {
        applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level) {
        activeFarmLevel = (int) level;
    }

    public void SetApplyPlantLevel(bool toggle) {
        applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level) {
        activePlantLevel = (int) level;
    }

    public void SetWalledMode(int mode) {
        walledMode = (OptionalToggle) mode;
    }
}