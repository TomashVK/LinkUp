using System;
using System.IO;
using UnityEngine;

public class LocalJsonFileStorage : ISaveStorage
{
    private readonly string savePath;
    private readonly string tempPath;

    public LocalJsonFileStorage()
    {
        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        tempPath = savePath + ".tmp";
    }

    public void Load(Action<string> onLoaded)
    {
        if (!File.Exists(savePath))
        {
            onLoaded?.Invoke(null);
            return;
        }

        try
        {
            onLoaded?.Invoke(File.ReadAllText(savePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"LocalJsonFileStorage: failed to read save file: {e}");
            onLoaded?.Invoke(null);
        }
    }

    public void Save(string json)
    {
        try
        {
            File.WriteAllText(tempPath, json);
            // Write-then-replace avoids leaving a half-written savegame.json if the
            // process is killed mid-write.
            File.Copy(tempPath, savePath, overwrite: true);
            File.Delete(tempPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"LocalJsonFileStorage: failed to write save file: {e}");
        }
    }
}
