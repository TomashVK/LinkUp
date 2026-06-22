using System;

public interface ISaveStorage
{
    // Callback-based so a synchronous local implementation and a future async
    // remote implementation (cloud save) can share the same call shape.
    void Load(Action<string> onLoaded);
    void Save(string json);
}
