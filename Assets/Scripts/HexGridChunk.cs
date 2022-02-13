using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour {
    private HexCell[] cells;

    private Canvas gridCanvas;

    private HexMesh hexMesh;

    void Awake() {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        ShowUI(false);
    }

    // use `enabled` state to control hexMesh.Triangulate in LateUpdate
    // void Start() {
    //     hexMesh.Triangulate(cells);
    // }

    public void AddCell(int index, HexCell cell) {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void Refresh() {
        enabled = true;
    }

    private void LateUpdate() {
        hexMesh.Triangulate(cells);
        enabled = false;
    }
    
    public void ShowUI(bool visible) {
        gridCanvas.gameObject.SetActive(visible);
    }
}