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

namespace BoxFileEditor
{
    /// <summary>
    /// Interaction logic for SelectedBoxWnd.xaml
    /// </summary>
    public partial class SelectedBoxWnd : Window
    {
        public SelectedBoxWnd()
        {
            InitializeComponent();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (btnApplyAndAdvance.IsEnabled)
                    Dispatcher.BeginInvoke(new RoutedEventHandler(btnApplyAndAdvance_Click), new object[] { this, new RoutedEventArgs() });
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
        
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            var mainWnd = Owner as MainWindow;
            mainWnd.ApplyValueToSelectedBoxes(textBoxValue.Text, false);
        }

        private void btnApplyAndAdvance_Click(object sender, RoutedEventArgs e)
        {
            var mainWnd = Owner as MainWindow;
            mainWnd.ApplyValueToSelectedBoxes(textBoxValue.Text, true);
        }

        public void SelectAndFocusValue()
        {
            Dispatcher.BeginInvoke(new Action(AsyncFocusTextBoxValue));
        }

        private void AsyncFocusTextBoxValue()
        {
            textBoxValue.Focus();
            textBoxValue.SelectAll();
        }

    }
}
