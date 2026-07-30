namespace RealmShards.Save
{
    public interface ISaveService
    {
        SaveData Current { get; }
        string SaveFilePath { get; }
        bool HasSaveFile { get; }

        SaveData LoadOrCreate();
        void Save();
        void Save(SaveData data);
        void DeleteSave();
    }
}
