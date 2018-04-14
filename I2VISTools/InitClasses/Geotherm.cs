using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Runtime.CompilerServices;
using I2VISTools.Subclasses;

namespace I2VISTools.InitClasses
{
    public class Geotherm : IBox, INotifyPropertyChanged
    {
        public Geotherm()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void FirePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        //0       0        0        0        m800000  1        0       1       m800000  1750   2100     1750   2100 
        //0        0        0        m20000   1        0       1       m20000   273    273      273    273  
        //0       0        m8000    0        m220000 m1800000 m8000   m1800000 m220000 273    1810.0   273    1810.0
        //6       m1800000 m8000    m1800000 m220000 m1880000 m12500  m1880000 m220000 273    1810.0   273    1810.0  4e+7 4e+7 1e-6

        /// <summary>
        /// Тип геотермы (0 - линейная; 4 - океаническая; 6 - слева линейная, справа океаническая; 5 - слева океаническая, справа линейная)
        /// </summary>
        public int GeothermType { get; set; }

        public string Name { get; set; }

        public List<ModPoint> Apexes
        {
            get
            {
                return new List<ModPoint> { Apex0, Apex1, Apex3, Apex2 };
            }
        } 

        private ModPoint _apex0;
        /// <summary>
        /// Вершина левого верхнего угла
        /// </summary>
        public ModPoint Apex0
        {
            get {return _apex0;}
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
            get {return _apex2;}
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

        public bool FreezeLogging { get; set; }
        public bool MultipleLogging { get; set; }

        private double _t0;

        /// <summary>
        /// Температура в левом верхнем углу (в Кельвинах)
        /// </summary>
        public double T0
        {
            get { return _t0; }
            set
            {
                _t0 = value;
                FirePropertyChanged("T0");
            }
        }

        private double _t1;
        /// <summary>
        /// Температура в левом нижнем углу (в Кельвинах)
        /// </summary>
        public double T1 
        {
            get { return _t1; }
            set
            {
                _t1 = value;
                FirePropertyChanged("T1");
            } 
        }

        private double _t2;
        /// <summary>
        /// Температура в правом верхнем углу (в Кельвинах)
        /// </summary>
        public double T2
        {
            get { return _t2; }
            set
            {
                _t2 = value;
                FirePropertyChanged("T2");
            } 
        }

        private double _t3;
        /// <summary>
        /// Температура в левом верхнем углу (в Кельвинах)
        /// </summary>
        public double T3
        {
            get { return _t3; }
            set
            {
                _t3 = value;
                FirePropertyChanged("T3");
            }
        }

        // TODO назвать иначе и сделать сеттер/геттер зависящий от типа геотермы
        public double LeftOceanicAge { get; set; }
        public double RightOceanicAge { get; set; }

        public double ThermalDiffusivity { get; set; }
    }
}
