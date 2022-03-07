using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HexCell : MonoBehaviour {
    public HexCoordinates coordinates;

    [SerializeField] private bool[] roads;

    private int terrainTypeIndex;

    private int elevation = int.MinValue;
    private int waterLevel;

    public RectTransform uiRect;

    private bool hasIncomingRiver, hasOutgoingRiver;
    private HexDirection incomingRiver, outgoingRiver;

    private int urbanLevel, farmLevel, plantLevel;

    private int specialIndex;

    private bool walled;

    private int distance;

    private int visibility;

    public bool Explorable { get; set; }

    public bool IsVisible {
        get => visibility > 0 && Explorable;
    }

    private bool explored;

    public bool IsExplored {
        get => explored && Explorable;
        private set => explored = value;
    }

    public int Index { get; set; }

    public HexCellShaderData ShaderData { get; set; }

    public int SearchPhase { get; set; }

    public int Distance {
        get => distance;
        set {
            distance = value;
            // UpdateDistanceLabel();
        }
    }

    public HexCell PathFrom { get; set; }

    public int SearchHeuristic { get; set; }

    public int SearchPriority {
        get { return distance + SearchHeuristic /** 5*/; }
    }

    public HexCell NexWithSamePriority { get; set; }

    public HexUnit Unit { get; set; }

    public int SpecialIndex {
        get => specialIndex;
        set {
            if (specialIndex != value && !HasRiver) {
                specialIndex = value;
                // avoiding roads
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    public bool IsSpecial {
        get { return specialIndex > 0; }
    }

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
        get => waterLevel;
        set {
            if (waterLevel == value) {
                return;
            }

            int originalViewElevation = ViewElevation;
            waterLevel = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }

            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderWater => waterLevel > elevation;

    public int Elevation {
        get => elevation;
        set {
            if (elevation == value) {
                return;
            }

            int originalViewElevation = ViewElevation;
            elevation = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }

            RefreshPosition();
            ValidateRivers();
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection) i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public int ViewElevation => elevation >= waterLevel ? elevation : waterLevel;

    public int TerrainTypeIndex {
        get { return terrainTypeIndex; }
        set {
            if (terrainTypeIndex != value) {
                terrainTypeIndex = value;
                ShaderData.RefreshTerrain(this);
            }
        }
    }

    public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

    public bool HasIncomingRiver => hasIncomingRiver;

    public bool HasOutgoingRiver => hasOutgoingRiver;

    public HexDirection IncomingRiver => incomingRiver;

    public HexDirection OutgoingRiver => outgoingRiver;

    public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;

    public bool HasRoads {
        get {
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i])
                    return true;
            }

            return false;
        }
    }

    public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;

    public HexDirection RiverBeginOrEndDirection => hasIncomingRiver ? incomingRiver : outgoingRiver;

    public float RiverSurfaceY => (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

    public float WaterSurfaceY => (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

    public bool HasRiverThroughEdge(HexDirection direction) {
        return hasIncomingRiver && incomingRiver == direction ||
               hasOutgoingRiver && outgoingRiver == direction;
    }

    public bool HasRoadThroughEdge(HexDirection direction) {
        return roads[(int) direction];
    }

    public HexGridChunk chunk;

    public Vector3 Position => transform.localPosition;

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

            if (Unit) {
                Unit.ValidateLocation();
            }
        }
    }

    public void RefreshSelfOnly() {
        chunk.Refresh();
        Unit?.ValidateLocation();
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
        specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;

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
        if (!roads[(int) direction] && !HasRiverThroughEdge(direction) &&
            !IsSpecial && !GetNeighbor(direction).IsSpecial &&
            GetElevationDifference(direction) <= 1
        ) {
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

    void RefreshPosition() {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
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
    }

    public void Save(BinaryWriter writer) {
        writer.Write((byte) terrainTypeIndex);
        writer.Write((byte) (elevation + 127));
        writer.Write((byte) waterLevel);
        writer.Write((byte) urbanLevel);
        writer.Write((byte) farmLevel);
        writer.Write((byte) plantLevel);
        writer.Write((byte) specialIndex);

        writer.Write(walled);
        if (hasIncomingRiver) {
            writer.Write((byte) (incomingRiver + 128));
        } else {
            writer.Write((byte) 0);
        }

        if (hasOutgoingRiver) {
            writer.Write((byte) (outgoingRiver + 128));
        } else {
            writer.Write((byte) 0);
        }

        byte roadFlags = 0;
        for (int i = 0; i < roads.Length; i++) {
            if (roads[i])
                roadFlags |= (byte) (1 << i);
        }

        writer.Write(roadFlags);
        writer.Write(IsExplored);
    }

    public void Load(BinaryReader reader, int header) {
        terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        if (header >= 4) {
            elevation -= 127;
        }

        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();

        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        hasIncomingRiver = (riverData >= 128);
        incomingRiver = (HexDirection) (riverData ^ 128);
        riverData = reader.ReadByte();
        hasOutgoingRiver = (riverData >= 128);
        outgoingRiver = (HexDirection) (riverData ^ 128);

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++) {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }

        IsExplored = (header >= 3 && reader.ReadBoolean());
        ShaderData.RefreshVisibility(this);
    }

    // void UpdateDistanceLabel() {
    //     Text label = uiRect.GetComponent<Text>();
    //     label.text = distance == int.MaxValue ? "" : distance.ToString();
    // }
    public void SetLabel(string text) {
        Text label = uiRect.GetComponent<Text>();
        label.text = text;
    }

    public void DisableHighlight() {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color) {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    public void IncreaseVisibility() {
        visibility += 1;
        if (visibility == 1) {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility() {
        visibility -= 1;
        if (visibility == 0) {
            ShaderData.RefreshVisibility(this);
        }
    }

    public void ResetVisibility() {
        if (visibility > 0) {
            visibility = 0;
            ShaderData.RefreshVisibility(this);
        }
    }
}