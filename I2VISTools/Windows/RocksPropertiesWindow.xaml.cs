using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using I2VISTools.InitClasses;

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for RocksPropertiesWindow.xaml
    /// </summary>
    public partial class RocksPropertiesWindow : Window
    {
        public RocksPropertiesWindow(InitConfig init)
        {
            InitializeComponent();
            _init = init;
        }

        private InitConfig _init;
        private List<RockDescription> _rocks;
        

        private void ApplyRocksButton_Click(object sender, RoutedEventArgs e)
        {
            _init.Rocks = _rocks;
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _rocks = CopyRocksList(_init.Rocks);

            RocksDataGrid.ItemsSource = _rocks;

        }

        private List<RockDescription> CopyRocksList(List<RockDescription> rocks)
        {
            if (rocks == null || rocks.Count == 0) return null;

            return rocks.Select(rock => new RockDescription()
            {
                a0 = rock.a0, a1 = rock.a1, aRo = rock.aRo, b0 = rock.b0, b1 = rock.b1, 
                bRo = rock.bRo, Cp = rock.Cp, De = rock.De, dh = rock.dh, Dv = rock.Dv, 
                Ht = rock.Ht, Kt = rock.Ht, Ll = rock.Ll, Mm = rock.Mm, Nu = rock.Nu, 
                Ro = rock.Ro, RockName = rock.RockName, RockNum = rock.RockNum, 
                Ss = rock.Ss, e0 = rock.e0, e1 = rock.e1, kf = rock.kf, kp = rock.Cp, 
                n0 = rock.n0, n1 = rock.n1, s0 = rock.s0, s1 = rock.s1
            }).ToList();
        }

        private void RocksDataGrid_OnKeyDown(object sender, KeyEventArgs e)
        {

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V))
            {
                MessageBox.Show("d");
                e.Handled = false;
            }
            
        }

        private RockDescription _bufferRock;

        private void RocksDataGrid_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.OriginalSource.GetType() != typeof(DataGridCell)) return;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.C))
            {
                var rock = RocksDataGrid.SelectedItem as RockDescription;
                if (rock == null) return;
                _bufferRock = rock;
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V))
            {
                if (_bufferRock == null) return;
                var targetRock = RocksDataGrid.SelectedItem as RockDescription;
                if (targetRock == null) return;
                var rock = _bufferRock;

                targetRock.a0 = rock.a0;
                targetRock.a1 = rock.a1;
                targetRock.aRo = rock.aRo;
                targetRock.b0 = rock.b0;
                targetRock.b1 = rock.b1;
                targetRock.bRo = rock.bRo;
                targetRock.Cp = rock.Cp;
                targetRock.De = rock.De;
                targetRock.dh = rock.dh;
                targetRock.Dv = rock.Dv;
                targetRock.Ht = rock.Ht;
                targetRock.Kt = rock.Ht;
                targetRock.Ll = rock.Ll;
                targetRock.Mm = rock.Mm;
                targetRock.Nu = rock.Nu;
                targetRock.Ro = rock.Ro;
                targetRock.Ss = rock.Ss;
                targetRock.e0 = rock.e0;
                targetRock.e1 = rock.e1;
                targetRock.kf = rock.kf;
                targetRock.kp = rock.Cp;
                targetRock.n0 = rock.n0;
                targetRock.n1 = rock.n1;
                targetRock.s0 = rock.s0;
                targetRock.s1 = rock.s1;

                RocksDataGrid.Items.Refresh();
            }
        }
    }
}
