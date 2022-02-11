using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {
    public Color[] colors;

    public HexGrid hexGrid;

    private Color activeColor;

    private int activeElevation;

    private void Awake() {
        SelectColor(0);
    }

    void Update() {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
            HandleInput();
        }
    }

    void HandleInput() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            EditCell(hexGrid.GetCell(hit.point, activeColor));
        }
    }

    void EditCell(HexCell cell) {
        cell.color = activeColor;
        cell.Elevation = activeElevation;
        hexGrid.Refresh();
    }

    public void SelectColor(int index) {
        activeColor = colors[index];
    }

    public void SetElevation(float elevation) {
        activeElevation = (int) elevation;
    }
}