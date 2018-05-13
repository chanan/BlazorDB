namespace BlazorDB.Storage
{
    internal class SerializedModel
    {
        public bool ScanDone { get; set; }
        public bool HasAssociation { get; set; }
        public string StringModel { get; set; }
        public object Model { get; set; }
    }
}
