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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI
{
    /// <summary>
    /// Interaction logic for GameOver.xaml
    /// </summary>
    public partial class GameOver : CustomChromeWindow
    {
        private bool mbIsOk;

        public bool IsOk { get { return mbIsOk; } }

        public GameOver()
        {
            mbIsOk = false;
            InitializeComponent();
        }
        public void Init()
        {
            mText.Text = string.Format("游戏结束了，重新开始？");
        }
        void Ok(object sender, RoutedEventArgs e)
        {
            Close();
            mbIsOk = true;
        }
        void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
