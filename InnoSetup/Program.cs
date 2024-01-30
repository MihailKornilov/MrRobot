using System.Diagnostics;
using System.IO;
using static System.Console;

namespace InnoSetup
{
    internal class Program
    {
        static void Main()
        {
            MainDir = Directory.GetCurrentDirectory();
            FileSrc = $"{MainDir}\\src.iss";
            FileIss = $"{MainDir}\\dst.iss";

            string WorkDir = $"{MainDir}\\Debug";
            if (!Directory.Exists(WorkDir))
            {
                WriteLine($"Каталога {WorkDir} не существует.");
                ReadLine();
            }

            if(!File.Exists(FileSrc))
            {
                WriteLine($"Не найден файл {Path.GetFileName(FileSrc)}.");
                ReadLine();
            }

            File.Copy(FileSrc, FileIss, true);
            InnoDirs(WorkDir);

            Process.Start(FileIss);
        }

        static string MainDir;
        static string FileSrc;
        static string FileIss;

        static void InnoDirs(string path, string directory = "")
        {
            InnoFiles(path, directory);
            var dirs = new DirectoryInfo(path).GetDirectories();
            foreach (var dir in dirs)
                InnoDirs($"{path}\\{dir.Name}", $"{directory}\\{dir.Name}");
        }
        static void InnoFiles(string path, string dir)
        {
            var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return;

            var write = new StreamWriter(FileIss, true);
            foreach (var file in files)
                write.WriteLine($"Source: \"{file}\"; DestDir: \"{{app}}{dir}\"; Flags: ignoreversion");
            write.Close();
        }
    }
}
