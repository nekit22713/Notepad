namespace Notepandus.Models {
    public enum FileTypes { SysDrive, Drive, BackFolder, Folder, FILE }

    public class FileItem {
        public FileItem(FileTypes type, string name) {
            Type = type;
            Name = name;
        }
        public FileTypes Type { get; }
        public string Name { get; }
    }
}
