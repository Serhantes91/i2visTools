using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using I2VISTools.Annotations;

namespace I2VISTools.Subclasses
{
    public class ModPoint : INotifyPropertyChanged
    {

        public string Name { get; set; }
        public int Type { get; set; }

        public ModPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        private double _x;
        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                OnPropertyChanged("X");
            }
        }

        private double _y;

        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged("Y");
            }
        }

        public override string ToString()
        {
            return String.Format("{0} ({1:##.000} ; {2:##.000})", Type, X, Y);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
