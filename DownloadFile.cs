using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Threads.Extensions;
using Threads.Model;


namespace Threads
{
    public class DownloadFile : ViewModelBase, IDisposable
    {
        public FilesInfo filesInfos { get; set; }
        public int progression { get; set; }
        public string status { get; set; }
        public SolidColorBrush colorProgressbar { get; set; }
        public SolidColorBrush colorTextProgressBar { get; set; }

        private string url;
        private int countThreads;
        private decimal fileLength;
        //Состояния потока
        public enum StatesThread { Unstarted = -1, Complete, Running, Paused, Stopped, Failed }
        private StatesThread currentState = StatesThread.Unstarted;

        static readonly HttpClient client = new HttpClient();
        private FileStream fs;
        private List<FileDownloader> fileDownloaders;
        private Progress<int> progress;

        public DownloadFile (string url, int countThreads, string fileName, string pathToSave)
        {

            this.url = url;
            this.countThreads = countThreads;

            filesInfos = new FilesInfo();
            filesInfos.name = fileName;
            filesInfos.path = pathToSave;


            progress = new Progress<int>(percent =>
            {
                progression += percent;
                if (progression >= 100)
                {
                    currentState = StatesThread.Complete;
                }

                switch (currentState)
                {

                    case StatesThread.Complete:
                        {
                            ChangeProgressionColor(Brushes.White, "#86c43f");
                            status = "Complete";
                            break;
                        }
                    case StatesThread.Running:
                        {
                            ChangeProgressionColor(Brushes.Black, "#84c2ff");
                            ChangeProgresstionText("Run");
                            break;
                        }
                    case StatesThread.Paused:
                        {
                            ChangeProgressionColor(Brushes.Black, "#9da0a3");
                            ChangeProgresstionText("Pause");
                            break;
                        }
                    case StatesThread.Stopped:
                        {
                            ChangeProgressionColor(Brushes.Black, "#9da0a3");
                            ChangeProgresstionText("Stop");
                            break;
                        }
                    case StatesThread.Failed:
                        {
                            DonwloadFailed();
                            break;
                        }
                }

            });
        }


        public async void Start ()
        {
            Run();
            try
            {
                var fileDownLoaders = await CalculateSizeForTasks().ConfigureAwait(false);
                if (fileDownloaders != null)
                {
                    using (fs = new FileStream(filesInfos.path + filesInfos.name, FileMode.Create, FileAccess.Write))
                    {

                        foreach (var item in fileDownLoaders)
                        {
                            await Task.WhenAll(DoDownloads(item, progress)).ConfigureAwait(false);

                        }
                    }
                }
            }
            catch (Exception)
            {
                DonwloadFailed();
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        public async void Append ()
        {
            Run();
            try
            {
                if (File.Exists(filesInfos.path + filesInfos.name))
                {
                    var appendFileLegnth = new FileInfo(filesInfos.path + filesInfos.name).Length;
                    var fileDownLoaders = await CalculateSizeForTasks(appendFileLegnth).ConfigureAwait(false);

                    if (fileDownloaders != null)
                    {

                        using (fs = new FileStream(filesInfos.path + filesInfos.name, FileMode.Append, FileAccess.Write))
                        {

                            foreach (var item in fileDownLoaders)
                            {

                                await Task.WhenAll(DoDownloads(item, progress)).ConfigureAwait(false);
                            }
                        }

                    }
                }
                else
                {
                    throw new Exception("File is missing");
                }
            }
            catch (Exception e)
            {
                DonwloadFailed();
                MessageBox.Show(e.Message);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }
        public void Run ()
        {
            currentState = StatesThread.Running;
        }
        public void Pause ()
        {
            currentState = StatesThread.Paused;
        }
        public void Stop ()
        {
            currentState = StatesThread.Paused;
        }

        public StatesThread GetCurrentStateThread ()
        {
            return currentState;
        }

        public async Task DoDownloads (object data, IProgress<int> progress)
        {
            try
            {
                var download = data as FileDownloader;
                using (HttpRequestMessage request = new HttpRequestMessage { RequestUri = new Uri(download.url) })
                {
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(download.start, download.start + download.count - 1);
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {

                        if (response.IsSuccessStatusCode)
                        {
                            using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            {
                                var buffer = new byte[4096];
                                int bytesRead = 0;

                                do
                                {

                                    if (currentState.Equals(StatesThread.Running))
                                    {
                                        bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                                        await fs.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                                        await fs.FlushAsync().ConfigureAwait(false);
                                        progress.Report((int)Math.Round((bytesRead * 100.0m / fileLength), 0));
                                    }

                                }
                                while (bytesRead > 0 && !currentState.Equals(StatesThread.Stopped));
                                if (progression < 100)
                                {
                                    progress.Report(100 - progression);
                                }
                            }
                        }
                        else
                        {
                            _ = response.EnsureSuccessStatusCode();
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                DonwloadFailed();
            }
        }
        public void Dispose ()
        {
            client.Dispose();
        }

        private async Task<List<FileDownloader>> CalculateSizeForTasks (long appendFileLength = 0)
        {
            try
            {
                using (var resp = await client.GetAsync(url).ConfigureAwait(false))
                {
                    if (resp.Headers.AcceptRanges.Contains("bytes"))
                    {
                        long contentLength = (long)resp.Content.Headers.ContentLength - appendFileLength;
                        if (contentLength > 0)
                        {
                            filesInfos.size = contentLength;
                            fileLength = contentLength;
                            fileDownloaders = new List<FileDownloader>();

                            //размер каждой порции
                            var eachSize = contentLength / countThreads;
                            //размер последней порции
                            var lastPartSize = eachSize + (contentLength % countThreads);

                            for (int i = 0; i < countThreads - 1; i++)
                            {
                                fileDownloaders.Add(new FileDownloader(url, i * eachSize + appendFileLength, eachSize));
                            }
                            fileDownloaders.Add(new FileDownloader(url, (countThreads - 1) * eachSize, lastPartSize));

                            return fileDownloaders;
                        }
                    }
                }
            }
            catch (Exception)
            {
                DonwloadFailed();
            }
            return null;
        }
        private void DonwloadFailed()
        {
            currentState = StatesThread.Failed;
            ChangeProgressionColor(Brushes.Black, "#8c1c10");
            status = "Failed";
        }
        private void ChangeProgresstionText (string newStatus)
        {
            status = newStatus + " " + progression + " %";
        }
        private void ChangeProgressionColor (SolidColorBrush colorText, string hexProgressBar)
        {
            colorTextProgressBar = colorText;
            colorProgressbar = hexProgressBar.ToBrush();
        }
    }
}
