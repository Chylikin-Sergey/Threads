using Microsoft.VisualBasic.FileIO;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Threads
{
    static class ControlFile
    {
        public static void OpenFolder (string pathFile)
        {
            if(Directory.Exists(pathFile))
                _ = Process.Start("explorer.exe",pathFile);
        }
        public static bool Move (string oldPath, string newPath)
        {

                Delete(newPath);
                File.Move(oldPath, newPath);

                return true;
        }
        public static bool Rename (string oldName, string newName)
        {
            try
            {
                if (File.Exists(oldName))
                    FileSystem.RenameFile(oldName, newName);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }

        }
        public static void Delete (string pathFile)
        {
            if (File.Exists(pathFile))
                File.Delete(pathFile);
        }
    }
}
