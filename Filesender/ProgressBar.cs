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
