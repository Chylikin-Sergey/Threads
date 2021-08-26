using DevExpress.Mvvm;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Threads.ViewModel
{
    public class MainViewModel : ViewModelBase
    {

        public string url { get; set; } = "https://klike.net/uploads/posts/2019-01/1547367999_1.jpg";
        public int countThreads { get; set; } = 1;
        public ObservableCollection<DownloadFile> download { get; set; }
        private DownloadFile newDownloadFile;
        private string oldText = "";
        public MainViewModel ()
        {
            download = new ObservableCollection<DownloadFile>();

        }


        public ICommand Download
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    SaveFileDialog saveFile = new SaveFileDialog();
                    saveFile.FileName = GetFileName();
                    if (saveFile.ShowDialog() == true)
                    {
                        var path = GetFilePath(saveFile.FileName);
                        newDownloadFile = new DownloadFile(url, countThreads, saveFile.SafeFileName, path);
                        download.Add(newDownloadFile);
                        newDownloadFile.Start();
                    }
                });
            }
        }

        private void MessageWarning ()
        {
            MessageBox.Show("Wait for the download to complete",
                "Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        public ICommand PreviewKeyBoardFocus
        {
            get
            {
                return new DelegateCommand<TextBox>((text) =>
                {
                    oldText = text.Text;
                });
            }
        }
        public ICommand ChangeName
        {
            get
            {
                return new DelegateCommand<TextBox>((text) =>
                {

                    text.IsReadOnly = true;
                    if (oldText != text.Text)
                    {
                        var downloadFile = (DownloadFile)text.GetBindingExpression(TextBox.TextProperty).DataItem;
                        if(!ControlFile.Rename(downloadFile.filesInfos.path + oldText, text.Text))
                        {
                            text.Undo();
                        }
                    }

                });
            }
        }
        public ICommand Run
        {
            get
            {
                return new DelegateCommand<DownloadFile>((obj) =>
                 {
                     if(obj.GetCurrentStateThread().Equals(DownloadFile.StatesThread.Stopped))
                     {
                         obj.Append();
                     }
                     else
                     {
                         obj.Run();
                     }
                 });
            }
        }


        public ICommand Pause
        {
            get
            {
                return new DelegateCommand<DownloadFile>((obj) =>
                {
                    obj.Pause();
                });
            }
        }
        public ICommand Stop
        {
            get
            {
                return new DelegateCommand<DownloadFile>((obj) =>
                {
                    obj.Stop();
                });
            }
        }
        public ICommand Remove
        {
            get
            {
                return new DelegateCommand<DownloadFile>((obj) =>
                {
                    obj.Stop();
                    download.Remove(obj);
                });
            }
        }
        public ICommand OpenFolder
        {
            get
            {
                return new DelegateCommand<DownloadFile>((obj) =>
                {
                    ControlFile.OpenFolder(obj.filesInfos.path);
                });
            }
        }
        public ICommand Move
        {
            get
            {
                return new DelegateCommand<DownloadFile>((obj) =>
                {
                    if (obj.GetCurrentStateThread().Equals(DownloadFile.StatesThread.Complete))
                    {
                        SaveFileDialog save = new SaveFileDialog();

                        save.FileName = obj.filesInfos.name;
                        if (save.ShowDialog() == true)
                        {
                            var newPath = GetFilePath(save.FileName);
                            if (ControlFile.Move(obj.filesInfos.path + obj.filesInfos.name, newPath + obj.filesInfos.name))
                                obj.filesInfos.path = newPath;
                        }
                    }
                    else
                    {
                        MessageWarning();
                    }
                });
            }
        }
        public ICommand Delete
        {
            get
            {
                return new DelegateCommand<DownloadFile>((obj) =>
                {
                    if (obj.GetCurrentStateThread().Equals(DownloadFile.StatesThread.Complete))
                    {
                        ControlFile.Delete(obj.filesInfos.path + obj.filesInfos.name);
                        obj.Stop();
                        download.Remove(obj);
                    }
                    else
                    {
                        MessageWarning();
                    }

                });
            }
        }

        private string GetFileName ()
        {
            Uri uri = new Uri(url);
            return System.IO.Path.GetFileName(uri.LocalPath);

        }
        private string GetFilePath (string fileName)
        {
            return fileName.Substring(0, fileName.LastIndexOf(@"\") + 1);
        }

    }
}
