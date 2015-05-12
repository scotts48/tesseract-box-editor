using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace BoxFileEditor
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public const string AppTitle = "Tesseract Box Editor";

        private string _imageFileName = null;
        private string _boxFileName = null;

        private FileStream _fileStream = null;
        private TiffBitmapDecoder _tiffDecoder = null;

        private Dictionary<int, ObservableCollection<TessBoxControl>> _pageBoxMap = new Dictionary<int, ObservableCollection<TessBoxControl>>();
        private Dictionary<int, BitmapSource> _pageImageMap = new Dictionary<int, BitmapSource>();

        public string WindowTitle
        {
            get
            {
                if (string.IsNullOrEmpty(_imageFileName))
                    return AppTitle;
                return string.Format("{0} - {1}", Path.GetFileName(_imageFileName), AppTitle);
            }
        }

        private int _maxPageIndex = 0;
        public int MaxPageIndex
        {
            get { return _maxPageIndex; }
            private set
            {
                if (_maxPageIndex == value) return;
                _maxPageIndex = value;
                NotifyPropertyChanged("MaxPageIndex");
            }
        }

        private int _selPageIndex = 0;
        public int SelPageIndex
        {
            get { return _selPageIndex; }
            set
            {
                if (_selPageIndex == value) return;
                _selPageIndex = value;
                SelectPage(value);
                NotifyPropertyChanged("SelPageIndex");
            }
        }

        private BitmapSource _image;
        public BitmapSource Image
        {
            get { return _image; }
            set
            {
                if (_image == value) return;
                _image = value;
                NotifyPropertyChanged("Image");
            }
        }

        private CroppedBitmap _selectedBoxImage = null;
        public CroppedBitmap SelectedBoxImage
        {
            get { return _selectedBoxImage; }
            set
            {
                if (_selectedBoxImage == value) return;
                _selectedBoxImage = value;
                NotifyPropertyChanged("SelectedBoxImage");
            }
        }

        private ObservableCollection<TessBoxControl> _boxes = null;
        public ObservableCollection<TessBoxControl> Boxes
        {
            get { return _boxes; }
            set
            {
                if(_boxes == value)return;
                _boxes = value;
                NotifyPropertyChanged("Boxes");
            }
        }

        private List<TessBoxControl> _selectedBoxes = new List<TessBoxControl>();
        public IEnumerable<TessBoxControl> SelectedBoxes
        {
            get { return _selectedBoxes; }
            set
            {
                var selectedItem = SelectedItem;
                if(selectedItem != null)
                    selectedItem.PropertyChanged -= selectedItem_PropertyChanged;

                _selectedBoxes.Clear();
                _selectedBoxes.AddRange(value);
                NotifyPropertyChanged("SelectedBoxes");
                NotifyPropertyChanged("SelectedItemValue");
                NotifyPropertyChanged("SelectedItem");
                NotifyPropertyChanged("CanEditSingleBox");

                selectedItem = SelectedItem;
                if(selectedItem != null)
                    selectedItem.PropertyChanged += selectedItem_PropertyChanged;

                UpdateCroppedImage();
            }
        }

        public bool CanEditSingleBox
        {
            get { return _selectedBoxes.Count == 1; }
        }

        public TessBoxControl SelectedItem
        {
            get { return _selectedBoxes.Count == 1 ? _selectedBoxes[0] : null; }
        }

        public string SelectedItemValue
        {
            get
            {
                if (_selectedBoxes.Count == 1)
                    return _selectedBoxes[0].Value;
                else
                    return string.Empty;
            }
            set
            {
                foreach (var box in _selectedBoxes)
                {
                    box.Value = value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if(handler != null)
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel()
        {
        }

        private void SelectPage(int pageIndex)
        {
            ObservableCollection<TessBoxControl> boxes = null;
            if (_pageBoxMap.ContainsKey(pageIndex))
            {
                boxes = _pageBoxMap[pageIndex];
            }
            else
            {
                //if there isn't any boxes for the page, make a new list
                boxes = new ObservableCollection<TessBoxControl>();
                _pageBoxMap[pageIndex] = boxes;
            }
            Image = GetPageImage(pageIndex);
            Boxes = boxes;
        }

        private void UpdateCroppedImage()
        {
            var selectedItem = SelectedItem;
            if (selectedItem == null)
            {
                SelectedBoxImage = null;
            }
            else
            {
                SelectedBoxImage = new CroppedBitmap(Image, new Int32Rect((int)selectedItem.Left, (int)selectedItem.Top, (int)selectedItem.Width, (int)selectedItem.Height));
            }
        }

        void selectedItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "RenderSize" || e.PropertyName == "Left" || e.PropertyName == "Top")
                UpdateCroppedImage();
        }

        private BitmapSource GetPageImage(int pageIndex)
        {
            BitmapSource bmp = null;

            if (!_pageImageMap.TryGetValue(pageIndex, out bmp))
            {
                if (_tiffDecoder == null)
                {
                    var srcBmp = new BitmapImage();
                    srcBmp.BeginInit();
                    srcBmp.StreamSource = _fileStream;
                    srcBmp.EndInit();
                    srcBmp.Freeze();
                    bmp = srcBmp;
                }
                else
                {
                    bmp = _tiffDecoder.Frames[pageIndex];
                }

                bmp = LoadImageAtWpfDPI(bmp);
                _pageImageMap[pageIndex] = bmp;
            }
            return bmp;
        }

        private BitmapSource LoadImageAtWpfDPI(BitmapSource srcBmp)
        {
            WriteableBitmap bmp = null;
            {
                //we need the bitmap at WPF dpi so that all our position stuff works out.  It's a lot easier
                //to do this, than try and make controls that handle pixel stuff
                bmp = new WriteableBitmap(srcBmp.PixelWidth, srcBmp.PixelHeight, 96, 96, srcBmp.Format, srcBmp.Palette);
                int stride = BitmapHelper.GetBitmapDataStride(srcBmp.Format.BitsPerPixel, srcBmp.PixelWidth);
                int byteCount = BitmapHelper.GetBitmapDataByteCount(srcBmp.Format.BitsPerPixel, srcBmp.PixelWidth, srcBmp.PixelHeight);
                var pixels = new byte[byteCount];
                srcBmp.CopyPixels(pixels, stride, 0);
                bmp.WritePixels(new Int32Rect(0, 0, srcBmp.PixelWidth, srcBmp.PixelHeight), pixels, stride, 0);
                bmp.Freeze();
            }
            return bmp;
        }

        public void Close()
        {
            _tiffDecoder = null;
            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }
            Image = null;
            Boxes = null;
            _imageFileName = null;
            _boxFileName = null;
            _maxPageIndex = 0;
            _selPageIndex = 0;
            _pageBoxMap.Clear();
            _pageImageMap.Clear();
            NotifyPropertyChanged("WindowTitle");
        }

        public void Load(string imageFileName)
        {
            var fi = new FileInfo(imageFileName);
            var boxFileName = Path.Combine(fi.DirectoryName, Path.GetFileNameWithoutExtension(fi.Name) + ".box");

            if (!File.Exists(boxFileName))
                throw new ApplicationException(string.Format("Box file '{0}' was not found", Path.GetFileName(boxFileName)));

            Close();

            _fileStream = new FileStream(imageFileName, FileMode.Open, FileAccess.Read);
            if (TiffHelper.IsTiffFile(_fileStream))
            {
                _tiffDecoder = new TiffBitmapDecoder(_fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                MaxPageIndex = _tiffDecoder.Frames.Count-1;
            }
            else
            {
                MaxPageIndex = 0;
            }


            int recordIndex = 0;
            using (var reader = new StreamReader(boxFileName, Encoding.UTF8))
            {
                ObservableCollection<TessBoxControl> boxes = null;
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;
                    var fields = line.Split(' ');
                    if (fields.Length != 6)
                        throw new ApplicationException(string.Format("Invalid box file record at line {0}", recordIndex));

                    var boxValue = fields[0];
                    var pageIndex = int.Parse(fields[5]);
                    var img = GetPageImage(pageIndex);

                    //Box file coordinates are with an origin at the bottom left on the image
                    var left = int.Parse(fields[1]);
                    var bottom = img.PixelHeight - int.Parse(fields[2]);
                    var right = int.Parse(fields[3]);
                    var top = img.PixelHeight - int.Parse(fields[4]);

                    var width = right - left;
                    var height = bottom - top;
                    
                    var box = new TessBoxControl();
                    box.Value = boxValue;
                    box.Width = width;
                    box.Height = height;
                    Canvas.SetLeft(box, left);
                    Canvas.SetTop(box, top);

                    if (!_pageBoxMap.TryGetValue(pageIndex, out boxes))
                    {
                        boxes = new ObservableCollection<TessBoxControl>();
                        _pageBoxMap[pageIndex] = boxes;
                    }

                    boxes.Add(box);
                    recordIndex++;
                }
            }

            _imageFileName = imageFileName;
            _boxFileName = boxFileName;
            NotifyPropertyChanged("WindowTitle");
            
            //set manually to force SelPageIndex to accept the change
            _selPageIndex = -1;
            SelPageIndex = 0;
        }
        
        public void Save()
        {
            using (var writer = new StreamWriter(_boxFileName, false, new UTF8Encoding(false)))
            {
                var orderPages = _pageBoxMap.OrderBy(n => n.Key);
                foreach (var pageNode in orderPages)
                {
                    var boxes = pageNode.Value;
                    foreach (var box in boxes)
                    {
                        var left = box.Left;
                        var top = box.Top;
                        var right = box.Left + box.Width;
                        var bottom = box.Top + box.Height;

                        var img = GetPageImage(pageNode.Key);

                        var line = string.Format("{0} {1} {2} {3} {4} {5}", box.Value, left, img.PixelHeight - bottom, right, img.PixelHeight - top, pageNode.Key);
                        writer.WriteLine(line);
                    }
                }
            }
        }

        public void MergeSelectedBoxes(IEnumerable<TessBoxControl> selectedBoxes)
        {
            var bounds = Rect.Empty;
            var remove = new List<TessBoxControl>();
            foreach (var box in selectedBoxes)
            {
                var boxBounds = new Rect(box.Left, box.Top, box.Width, box.Height);
                if (bounds.IsEmpty)
                    bounds = boxBounds;
                else
                    bounds.Union(boxBounds);
                remove.Add(box);
            }
            var keep = remove[remove.Count - 1];
            remove.RemoveAt(remove.Count - 1);
            foreach (var box in remove)
                Boxes.Remove(box);

            keep.Left = bounds.Left;
            keep.Top = bounds.Top;
            keep.Width = bounds.Width;
            keep.Height = bounds.Height;
        }

        public void DeleteSelectedBoxes(IEnumerable<TessBoxControl> selectedBoxes)
        {
            var remove = new List<TessBoxControl>(selectedBoxes);
            foreach (var box in remove)
                Boxes.Remove(box);
        }

    }
}
