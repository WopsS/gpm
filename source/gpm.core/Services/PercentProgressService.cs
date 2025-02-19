using System;
using System.ComponentModel;
using System.Globalization;

namespace gpm.core.Services
{
    public class PercentProgressService : IProgressService<double>
    {
        private int _maxPercent;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public PercentProgressService()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {

        }

        public void Reset() => _maxPercent = 0;

        public void Report(double percValue)
        {
            var perc = (int)(percValue * 100);

            if (perc == 0)
            {
                Reset();
                return;
            }

            if (perc > _maxPercent)
            {
                _maxPercent = perc;
            }

            Console.Write("\r{0}%", _maxPercent.ToString(CultureInfo.InvariantCulture));

            if (perc == 100)
            {
                Console.WriteLine();
            }
        }

#pragma warning disable CS0067

        public event EventHandler<double> ProgressChanged;

        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

        public bool IsIndeterminate { get; set; }
    }
}
