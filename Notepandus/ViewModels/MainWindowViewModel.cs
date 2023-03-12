using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Notepandus.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using static Notepandus.Models.FileTypes;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;

namespace Notepandus.ViewModels {
    public class MainWindowViewModel: ViewModelBase {
        private readonly ObservableCollection<FileItem> fileList = new();
        public ObservableCollection<FileItem> FileList { get => fileList; }

        private string cur_dir = "";

        private void LoadDisks() {
            DriveInfo[] drives = DriveInfo.GetDrives();
            string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string? sys_d = Path.GetPathRoot(system);
            fileList.Clear();
            foreach (DriveInfo drive in drives)
                fileList.Add(new FileItem(drive.Name == sys_d ? SysDrive : Drive, drive.Name));
        }
        private void LoadDir() {
            DirectoryInfo directory = new(cur_dir);
            NaturalComparer nc = new();

            List<string> dirs = new();
            foreach (var file in directory.GetDirectories()) dirs.Add(file.Name);
            dirs.Sort(nc);

            List<string> files = new();
            foreach (var file in directory.GetFiles()) files.Add(file.Name);
            files.Sort(nc);

            fileList.Clear();
            fileList.Add(new FileItem(BackFolder, ".."));
            foreach (var name in dirs) fileList.Add(new FileItem(Folder, name));
            foreach (var name in files) fileList.Add(new FileItem(FILE, name));
        }
        private void Loader(bool start = false) {
            FileBox = "";
            // Не знаю, зачем каждый раз при переходе из режима блокнота
            // в режим файлового эксплорера надо сбрасывать текущую директорию,
            // но т.к. вроде бы у Вас так было в видео, то воть:
            if (start) cur_dir = Directory.GetCurrentDirectory();
            if (cur_dir == "") LoadDisks();
            else LoadDir();
        }
        private void UpdButtonMode() { // Сильноукороченная версия DoubleTap для конкретно этой узкоспециализированной задачки
            if (openMode) return; // Нет смысла что-то обновлять в режиме открытия, а не сохранения файла...

            string path = Path.Combine(cur_dir, fileBox);
            if (!File.Exists(path)) {
                ButtonMode = "Открыть";
                return;
            }

            var attrs = File.GetAttributes(path);
            bool is_file = (attrs & FileAttributes.Archive) != 0;
            ButtonMode = is_file ? "Сохранить" : "Открыть";
        }

        private bool explorerMode = false;
        private bool openMode = false;
        private string buttonMode = "";
        public bool ExplorerMode { get => explorerMode; set => this.RaiseAndSetIfChanged(ref explorerMode, value); }
        public string ButtonMode { get => buttonMode; set => this.RaiseAndSetIfChanged(ref buttonMode, value); }

        private void FuncOpen() {
            if (explorerMode) return;
            ExplorerMode = true;
            Loader(true);
            ButtonMode = "Открыть";
            openMode = true;
        }
        private void FuncSave() {
            if (explorerMode) return;
            ExplorerMode = true;
            Loader(true);
            ButtonMode = "Открыть";
            openMode = false;
        }

        private void FuncOk() {
            DoubleTap(); // Имитируем двойное нажатие по элементу, название которого содержится в fileBox
        }
        private void FuncCancel() {
            if (!explorerMode) return;
            ExplorerMode = false;
        }

        private void SelectItem(FileItem item) {
            if (item == null) return;
            FileBox = item.Name;
            UpdButtonMode();
        }

        private void Message(string msg) {
            if (!fileBox.StartsWith(msg)) FileBox = msg + fileBox;
        }

        public void DoubleTap() {
            if (!explorerMode) return;

            if (fileBox == "..") { // Движемся назад к корневой папке (или списку дисков)
                var parentDir = Directory.GetParent(cur_dir);
                cur_dir = parentDir == null ? "" : parentDir.FullName;
                Loader();
                return;
            }

            if (cur_dir == "") { // Заходим на диск
                if (Directory.Exists(fileBox)) {
                    cur_dir = fileBox;
                    Loader();
                } else Message("Нет такого диска: ");
                return;
            }

            string path = Path.Combine(cur_dir, fileBox);
            FileAttributes attrs;

            try {
                attrs = File.GetAttributes(path);
            } catch (IOException) { // За одно и !Directory.Exists(path) не понадобился ;'-}
                if (openMode) Message("Нет такой папки/файла: ");
                else { // saveMode
                    File.WriteAllText(path, contentBox);
                    ExplorerMode = false;
                }
                return;
            }

            // В питоне вполне могло быть, что это и не файл и не папка,
            // если это, к примеру, ссылка или ещё что-то поособеннее...
            bool is_dir = (attrs & FileAttributes.Directory) != 0;
            bool is_file = (attrs & FileAttributes.Archive) != 0;

            if (is_dir) {
                cur_dir = path;
                Loader();
            } else if (is_file) {
                if (openMode) {
                    ContentBox = File.ReadAllText(path);
                    ExplorerMode = false;
                } else { // saveMode
                    //Message("Такой файл уже существует: "); Посмотрел Ваш ролик... перезапись нужна ;'-}
                    File.WriteAllText(path, contentBox);
                    ExplorerMode = false;
                }
            }
        }

        string contentBox = "";
        string fileBox = "";
        FileItem selectedItem = new(FILE, "?");

        public string ContentBox { get => contentBox; set => this.RaiseAndSetIfChanged(ref contentBox, value); }
        public string FileBox { get => fileBox; set => this.RaiseAndSetIfChanged(ref fileBox, value); }
        public FileItem SelectedItem { get => selectedItem; set { selectedItem = value; SelectItem(value); } }

        public MainWindowViewModel() {
            Open = ReactiveCommand.Create<Unit, Unit>(_ => { FuncOpen(); return new Unit(); });
            Save = ReactiveCommand.Create<Unit, Unit>(_ => { FuncSave(); return new Unit(); });
            Ok = ReactiveCommand.Create<Unit, Unit>(_ => { FuncOk(); return new Unit(); });
            Cancel = ReactiveCommand.Create<Unit, Unit>(_ => { FuncCancel(); return new Unit(); });
        }

        public ReactiveCommand<Unit, Unit> Open { get; }
        public ReactiveCommand<Unit, Unit> Save { get; }
        public ReactiveCommand<Unit, Unit> Ok { get; }
        public ReactiveCommand<Unit, Unit> Cancel { get; }
    }
}