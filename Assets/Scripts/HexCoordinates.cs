using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[System.Serializable]
public class HexCoordinates {
    [SerializeField] private int x, z;

    public int X {
        get { return x; }
    }

    public int Z {
        get { return z; }
    }

    public int Y {
        get { return -X - Z; }
    }

    public HexCoordinates(int x, int z) {
        if (HexMetrics.Wrapping) {
            int oX = x + z / 2;
            if (oX < 0) {
                x += HexMetrics.wrapSize;
            } else if (oX >= HexMetrics.wrapSize) {
                x -= HexMetrics.wrapSize;
            }
        }

        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z) {
        return new HexCoordinates(x - z / 2, z);
    }

    public override string ToString() {
        return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines() {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    public static HexCoordinates FromPosition(Vector3 position) {
        float x = position.x / HexMetrics.innerDiameter;
        float y = -x;

        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0) {
            Debug.LogWarning("rounding error!");
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ) {
                iX = -iY - iZ;
            } else if (dZ > dY) {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public int DistanceTo(HexCoordinates other) {
        // return ((x < other.x ? other.x - x : x - other.x) +
        //         (Y < other.Y ? other.Y - Y : Y - other.Y) +
        //         (z < other.z ? other.z - z : z - other.z)) / 2;
        int xy =
            (x < other.x ? other.x - x : x - other.x) +
            (Y < other.Y ? other.Y - Y : Y - other.Y);

        if (HexMetrics.Wrapping) {
            other.x += HexMetrics.wrapSize;
            int xyWrapped =
                (x < other.x ? other.x - x : x - other.x) +
                (Y < other.Y ? other.Y - Y : Y - other.Y);
            if (xyWrapped < xy) {
                xy = xyWrapped;
            } else {
                other.x -= 2 * HexMetrics.wrapSize;
                xyWrapped =
                    (x < other.x ? other.x - x : x - other.x) +
                    (Y < other.Y ? other.Y - Y : Y - other.Y);
                if (xyWrapped < xy) {
                    xy = xyWrapped;
                }
            }
        }

        return (xy + (z < other.z ? other.z - z : z - other.z)) / 2;
    }

    public void Save(BinaryWriter writer) {
        writer.Write(x);
        writer.Write(z);
    }

    public static HexCoordinates Load(BinaryReader reader) {
        HexCoordinates c = new HexCoordinates(reader.ReadInt32(), reader.ReadInt32());
        return c;
    }
}