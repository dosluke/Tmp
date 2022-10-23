using Extensions.String.Manipulation;
using System.IO;

namespace ProrimorGUI
{
    public class FileFolderModel
    {
        private bool? _isdir = null;
        private bool? _isfile = null;
        private bool? _isroot = null;

        public string Path { get; set; }
        //[OLVColumn(Width = 100, FillsFreeSpace = true, IsEditable = true, IsVisible =true)]
        public string Name { get; set; }
        //this simple caching system reduces disk reads by only checking the disk if its called, and caching the value
        public bool IsDirectory
        {
            get
            {
                if (_isdir != null) return _isdir.Value;

                _isdir = Directory.Exists(Path);
                return IsDirectory;
            }
            private set { _isdir = value; }
        }
        public bool IsFile
        {
            get
            {
                if (_isfile != null) return _isfile.Value;

                _isfile = File.Exists(Path);
                return IsFile;
            }
            private set { _isfile = value; }
        }
        public bool Exists { get { return IsDirectory || IsFile; } }

        public bool IsRoot
        {
            get
            {
                if (_isroot.HasValue) return _isroot.Value;

                _isroot = new DirectoryInfo(Path).Parent == null;
                return IsRoot;
            }
            private set { _isroot = value; }
        }

        public FileFolderModel(string path, bool? isfold = null)
        {
            Path = path;
            Name = Path.SubstringAfterLast("\\");

            if (isfold != null)
            {
                IsDirectory = isfold.Value;
                IsFile = !IsDirectory;
            }
        }

        public FileInfo AsFileInfo() { return new FileInfo(Path); }
        public DirectoryInfo AsDirectoryInfo() { return new DirectoryInfo(Path); }
    }
}
