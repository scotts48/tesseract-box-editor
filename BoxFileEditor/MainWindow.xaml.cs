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
using Microsoft.Win32;

namespace BoxFileEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SelectedBoxWnd _boxWnd = null;

        private MainViewModel _viewModel = null;

        private bool _suppressEventHandlers = false;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            //System.Windows.Interop.HwndSource.DefaultAcquireHwndFocusInMenuMode = false;
            //Keyboard.DefaultRestoreFocusMode = RestoreFocusMode.None;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _boxWnd = new SelectedBoxWnd();
            _boxWnd.Owner = this;
            _boxWnd.ShowActivated = false;
            _boxWnd.DataContext = _viewModel;
            _boxWnd.Show();
            /*
            _zoomToolWindow = new Window();
            _zoomToolWindow.Owner = this;
            _zoomToolWindow.WindowStyle = WindowStyle.ToolWindow;
            _zoomToolWindow.Background = SystemColors.AppWorkspaceBrush;
            _zoomToolWindow.ShowInTaskbar = false;
            _zoomToolWindow.Width = 200;
            _zoomToolWindow.Height = 200;
            _zoomToolWindow.DataContext = DataContext;
            _zoomToolWindow.Title = "Selected Box View";

            var img = new Image();
            img.Margin = new Thickness(10);
            img.Stretch = Stretch.Uniform;
            img.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
            img.SetBinding(Image.SourceProperty, new Binding("SelectedBoxImage"));
            _zoomToolWindow.Content = img;
            
            _zoomToolWindow.Show();
            */
        }

        private void boxView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_suppressEventHandlers)
            {
                _suppressEventHandlers = true;
                foreach (var removedItem in e.RemovedItems)
                    boxList.SelectedItems.Remove(removedItem);
                foreach (var addedItem in e.AddedItems)
                    boxList.SelectedItems.Add(addedItem);
                _viewModel.SelectedBoxes = boxView.SelectedItems.Cast<TessBoxControl>();
                _suppressEventHandlers = false;
            }
           if(_viewModel.SelectedItem != null)   
                boxList.ScrollIntoView(_viewModel.SelectedItem);
        }

        private void boxList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_suppressEventHandlers)
            {
                _suppressEventHandlers = true;
                foreach (var removedItem in e.RemovedItems)
                    boxView.SelectedItems.Remove(removedItem);
                foreach (var addedItem in e.AddedItems)
                    boxView.SelectedItems.Add(addedItem);
                _viewModel.SelectedBoxes = boxView.SelectedItems.Cast<TessBoxControl>();
                _suppressEventHandlers = false;
            }
            if (_viewModel.SelectedItem != null)
                boxView.ScrollIntoView(_viewModel.SelectedItem);
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.MergeSelectedBoxes(boxView.SelectedItems.Cast<TessBoxControl>());
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteSelectedBoxes(boxView.SelectedItems.Cast<TessBoxControl>());
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.PageUp)
            {
                if (_viewModel.SelPageIndex < _viewModel.MaxPageIndex)
                    _viewModel.SelPageIndex++;
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                if (_viewModel.SelPageIndex > 0)
                    _viewModel.SelPageIndex--;
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        protected internal void ApplyValueToSelectedBoxes(string value, bool advance)
        {
            _viewModel.SelectedItemValue = value;
            if (advance)
            {
                var selectedItem = boxView.SelectedItem as TessBoxControl;
                if (selectedItem != null)
                {
                    var index = boxView.ItemContainerGenerator.IndexFromContainer(selectedItem);
                    index++;
                    selectedItem = boxView.ItemContainerGenerator.ContainerFromIndex(index) as TessBoxControl;
                    boxView.SelectedItem = selectedItem;
                    _boxWnd.SelectAndFocusValue();
                }
            }
        }

        private void menuLoad_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog();
            openDlg.Filter = "TIFF Image Files (*.tiff, *.tif)|*.tiff;*.tif|PNG Image Files (*.png)|*.png|All Files (*.*)|*.*";
            openDlg.Multiselect = false;
            openDlg.CheckPathExists = true;
            if (openDlg.ShowDialog() == true)
            {
                try
                {
                    _viewModel.Load(openDlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Unable to load '{0}', {1}", openDlg.FileName, ex.GetBaseException().Message), MainViewModel.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void menuSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to save box file, {0}", ex.GetBaseException().Message), MainViewModel.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void boxView_CreateBox(object sender, Rect bounds)
        {
            boxView.SelectedItems.Clear();

            var box = new TessBoxControl();
            box.Value = "?";
            box.Width = bounds.Width;
            box.Height = bounds.Height;
            Canvas.SetLeft(box, bounds.Left);
            Canvas.SetTop(box, bounds.Top);
            _viewModel.Boxes.Add(box);
            
        }

        private void boxView_MergeSelected(object sender, EventArgs e)
        {
            _viewModel.MergeSelectedBoxes(boxView.SelectedItems.Cast<TessBoxControl>());
        }

        private void boxView_DeleteSelected(object sender, EventArgs e)
        {
            _viewModel.DeleteSelectedBoxes(boxView.SelectedItems.Cast<TessBoxControl>());
        }

    }
}
