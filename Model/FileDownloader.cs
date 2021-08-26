using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threads.Model
{
    class FileDownloader
    {
        //ссылка на файл
        public string url;
        public long start;
        public long count;
        public FileDownloader(string url, long start, long count)
        {
            this.url = url;
            this.start = start;
            this.count = count;
        }
    }
}
