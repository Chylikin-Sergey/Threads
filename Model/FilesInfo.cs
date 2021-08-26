using DevExpress.Mvvm;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threads.Model
{
    public class FilesInfo: ViewModelBase
    {
        public string url { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public decimal size
        {
            get => siz;
            set
            {
                value = value / 1024m;
                if(value > 1000)
                {
                    value = value / 1024m;
                }
                siz = decimal.Round(value,2);
            }
        }

        private decimal siz;
    }

}
