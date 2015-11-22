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
    /// Interaction logic for NextLevel.xaml
    /// </summary>
    public partial class NextLevel : CustomChromeWindow
    {
        public NextLevel()
        {
            InitializeComponent();
        }
        public void Init(int level)
        {
            mText.Text = string.Format("欢迎进入第{0}关", level);
        }
    }
}
