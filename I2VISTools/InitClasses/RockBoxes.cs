using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Windows;
using I2VISTools.Subclasses;

namespace I2VISTools.InitClasses
{
    public class RockBox : IBox, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public bool FreezeLogging { get; set; }
        public bool MultipleLogging { get; set; }

        private void FirePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        //Type__Rock__x0____________y0____________x1___________y1_____________x2_____________y2_______________x3_______________y3
        public int RockType { get; set; }

        private int _rockId;

        public int RockId
        {
            get
            {
                return _rockId;
            }
            set
            {
                _rockId = value;
                FirePropertyChanged("RockId");
            }
        }


        public List<ModPoint> Apexes
        {
            get
            {
                return new List<ModPoint> {Apex0, Apex1, Apex3, Apex2};
            }
        } 

        private ModPoint _apex0;
        /// <summary>
        /// Вершина левого верхнего угла
        /// </summary>
        public ModPoint Apex0
        {
            get { return _apex0; }
            set
            {
                _apex0 = value;
                FirePropertyChanged("Apex0");
                Apex0.PropertyChanged += (s, e) =>
                {
                    FirePropertyChanged("Apex0");
                };
            }
        }

        private ModPoint _apex1;
        /// <summary>
        /// Вершина левого нижнего угла
        /// </summary>
        public ModPoint Apex1
        {
            get { return _apex1; }
            set
            {
                _apex1 = value;
                FirePropertyChanged("Apex1");
                Apex1.PropertyChanged += (s, e) =>
                {
                    FirePropertyChanged("Apex1");
                };
            }
        }

        private ModPoint _apex2;
        /// <summary>
        /// Вершина правого верхнего угла
        /// </summary>
        public ModPoint Apex2
        {
            get { return _apex2; }
            set
            {
                _apex2 = value;
                FirePropertyChanged("Apex2");
                Apex2.PropertyChanged += (s, e) =>
                {
                    FirePropertyChanged("Apex2");
                };
            }
        }

        private ModPoint _apex3;
        /// <summary>
        /// Вершина правого нижнего угла
        /// </summary>
        public ModPoint Apex3
        {
            get { return _apex3; }
            set
            {
                _apex3 = value;
                FirePropertyChanged("Apex3");
                Apex3.PropertyChanged += (s, e) =>
                {
                    FirePropertyChanged("Apex3");
                };
            }
        }

        public string Name { get; set; }

    }
}
