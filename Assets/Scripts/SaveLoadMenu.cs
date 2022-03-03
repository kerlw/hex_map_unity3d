using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour {
    public HexGrid hexGrid;

    public Text menuLabel, actionButtonLabel;

    public InputField nameInput;

    public RectTransform listContent;

    public SaveLoadItem itemPrefab;

    private bool saveMode;

    public void Open(bool saveMode) {
        this.saveMode = saveMode;
        menuLabel.text = saveMode ? "Save Map" : "Load Map";
        actionButtonLabel.text = saveMode ? "Save" : "Load";
        FillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close() {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    string GetSelectedPath() {
        string mapName = nameInput.text;
        if (mapName.Length == 0) {
            return null;
        }

        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    public void SelectItem(string name) {
        nameInput.text = name;
    }

    public void Action() {
        string path = GetSelectedPath();
        if (path == null) {
            return;
        }

        if (saveMode) {
            Save(path);
        } else {
            Load(path);
        }

        Close();
    }

    public void Delete() {
        string path = GetSelectedPath();
        if (path == null)
            return;

        if (File.Exists(path)) {
            File.Delete(path);
        }

        FillList();
    }

    void Save(string path) {
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
            writer.Write(HexMapEditor.mapFileFormatVersion);
            hexGrid.Save(writer);
        }
    }

    void Load(string path) {
        if (!File.Exists(path)) {
            Debug.LogError("File not found: " + path);
            return;
        }

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            int header = reader.ReadInt32();
            if (header == HexMapEditor.mapFileFormatVersion) {
                hexGrid.Load(reader);
                HexMapCamera.ValidatePosition();
            } else {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }

    void FillList() {
        for (int i = 0; i < listContent.childCount; i++) {
            Destroy(listContent.GetChild(i).gameObject);
        }

        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for (int i = 0; i < paths.Length; i++) {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(listContent, false);
        }

        nameInput.text = "";
    }
}