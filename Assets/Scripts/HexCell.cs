using System;
using UnityEngine;

public class HexCell : MonoBehaviour {
    public HexCoordinates coordinates;

    [SerializeField] private bool[] roads;

    Color color;

    private int elevation = int.MinValue;
    private int waterLevel;

    public RectTransform uiRect;

    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;

    private int urbanLevel, farmLevel, plantLevel;

    private bool walled;

    public bool Walled {
        get => walled;
        set {
            if (walled != value) {
                walled = value;
                Refresh();
            }
        }
    }

    public int UrbanLevel {
        get { return urbanLevel; }
        set {
            if (urbanLevel != value) {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel {
        get { return farmLevel; }
        set {
            if (farmLevel != value) {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel {
        get => plantLevel;
        set {
            if (plantLevel != value) {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int WaterLevel {
        get { return waterLevel; }
        set {
            if (waterLevel == value) {
                return;
            }

            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderWater {
        get { return waterLevel > elevation; }
    }

    public int Elevation {
        get { return elevation; }
        set {
            if (elevation == value) {
                return;
            }

            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            // set ui z position
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y; //elevation * -HexMetrics.elevationStep;
            uiRect.localPosition = uiPosition;

            // preventing uphill rivers
            // if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation) {
            //     RemoveOutgoingRiver();
            // }
            //
            // if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation) {
            //     RemoveIncomingRiver();
            // }
            ValidateRivers();

            for (int i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection) i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public Color Color {
        get { return color; }

        set {
            if (color == value) {
                return;
            }

            color = value;
            Refresh();
        }
    }

    public float StreamBedY {
        get { return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep; }
    }

    public bool HasIncomingRiver {
        get { return hasIncomingRiver; }
    }

    public bool HasOutgoingRiver {
        get { return hasOutgoingRiver; }
    }

    public HexDirection IncomingRiver {
        get { return incomingRiver; }
    }

    public HexDirection OutgoingRiver {
        get { return outgoingRiver; }
    }

    public bool HasRiver {
        get { return hasIncomingRiver || hasOutgoingRiver; }
    }

    public bool HasRoads {
        get {
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i])
                    return true;
            }

            return false;
        }
    }

    public bool HasRiverBeginOrEnd {
        get { return hasIncomingRiver != hasOutgoingRiver; }
    }

    public HexDirection RiverBeginOrEndDirection {
        get { return hasIncomingRiver ? incomingRiver : outgoingRiver; }
    }

    public float RiverSurfaceY {
        get { return (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep; }
    }

    public float WaterSurfaceY {
        get { return (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep; }
    }

    public bool HasRiverThroughEdge(HexDirection direction) {
        return hasIncomingRiver && incomingRiver == direction ||
               hasOutgoingRiver && outgoingRiver == direction;
    }

    public bool HasRoadThroughEdge(HexDirection direction) {
        return roads[(int) direction];
    }

    public HexGridChunk chunk;

    public Vector3 Position {
        get { return transform.localPosition; }
    }

    [SerializeField] private HexCell[] neighbors;

    public HexCell GetNeighbor(HexDirection direction) {
        return neighbors[(int) direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        neighbors[(int) direction] = cell;
        cell.neighbors[(int) direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int) direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell) {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    public int GetElevationDifference(HexDirection direction) {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    public void Refresh() {
        if (chunk) {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++) {
                HexCell neighbor = neighbors[i];
                neighbor?.chunk?.Refresh();
            }
        }
    }

    public void RefreshSelfOnly() {
        chunk.Refresh();
    }

    public void RemoveOutgoingRiver() {
        if (!hasOutgoingRiver)
            return;

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        // allow river flow out map edge
        if (neighbor) {
            neighbor.hasIncomingRiver = false;
            neighbor.RefreshSelfOnly();
        }
    }

    public void RemoveIncomingRiver() {
        if (!hasIncomingRiver)
            return;

        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        if (neighbor) {
            neighbor.hasOutgoingRiver = false;
            neighbor.RefreshSelfOnly();
        }
    }

    public void RemoveRiver() {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void SetOutgoingRiver(HexDirection direction) {
        if (hasOutgoingRiver && outgoingRiver == direction)
            return;

        HexCell neighbor = GetNeighbor(direction);
        // TODO allow river flow out map edge: if (neighbor && elevation < neighbor.elevation)
        if (!IsValidRiverDestination(neighbor)) {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction) {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = direction;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();

        SetRoad((int) direction, false);
    }

    public void RemoveRoads() {
        for (int i = 0; i < neighbors.Length; i++) {
            if (roads[i]) {
                roads[i] = false;
                neighbors[i].roads[(int) ((HexDirection) i).Opposite()] = false;
                neighbors[i].RefreshSelfOnly();
                RefreshSelfOnly();
            }
        }
    }

    public void AddRoad(HexDirection direction) {
        if (!roads[(int) direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= 1) {
            SetRoad((int) direction, true);
        }
    }

    void SetRoad(int index, bool state) {
        roads[index] = state;
        neighbors[index].roads[(int) ((HexDirection) index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    bool IsValidRiverDestination(HexCell neighbor) {
        return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }

    void ValidateRivers() {
        if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver))) {
            RemoveOutgoingRiver();
        }

        if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this)) {
            RemoveIncomingRiver();
        }
    }
}