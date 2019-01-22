using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filesender
{
    public class ProgressBar : ViewModelBase
    {
        private int progress;
        public int Progress
        {
            get { return progress; }
            set
            {
                if (value != progress)
                {
                    progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }
    }
}
