using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FileDownloaderLibrary
{
    public class FileDownloader : IFileDownloader
    {
        private ConcurrentQueue<MyFile> downloadingQueue = new ConcurrentQueue<MyFile>();
        private int DegreeOfParallelism = 4;
        private int CurrentThreadCount = 0;
        public event Action<string> OnDownloaded;
        public event Action<string, Exception> OnFailed;
        public void AddFileToDownloadingQueue(string fileId, string url, string pathToSave)
        {
            var file = new MyFile(fileId, url, pathToSave);
            downloadingQueue.Enqueue(file);
            DownloadFileAsync();
        }
        private async void DownloadFileAsync()
        {
            if (CurrentThreadCount < DegreeOfParallelism)
            {
                CurrentThreadCount++;
                if (downloadingQueue.TryDequeue(out MyFile file))
                {
                    await Task.Run(() => DownloadFile(file));
                }
                else
                {
                    CurrentThreadCount--;
                }
            }
        }
        private async void DownloadFile(MyFile file)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(file.Url);
                    using (var fs = new FileStream(file.PathToSave + $"{file.FileID}.jpg", FileMode.CreateNew))
                    {
                        await response.Result.Content.CopyToAsync(fs);
                    }
                    OnDownloaded?.Invoke($"Файл {file.FileID} загружен");
                }
            }
            catch (Exception e)
            {
                OnFailed?.Invoke($"Файл {file.FileID} не загружен", e);
            }
            if (downloadingQueue.TryDequeue(out file))
            {
                DownloadFile(file);
            }
            else
            {
                CurrentThreadCount--;
            }
        }       
        class MyFile
        {
            public string FileID { get; set; }
            public string Url { get; set; }
            public string PathToSave { get; set; }
            public MyFile(string fileId, string url, string pathToSave)
            {
                FileID = fileId;
                Url = url;
                PathToSave = pathToSave;
            }
        }
    }
}
