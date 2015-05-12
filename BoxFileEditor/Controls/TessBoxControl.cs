using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BoxFileEditor
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:BoxFileEditor"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:BoxFileEditor;assembly=BoxFileEditor"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:TessBoxControl/>
    ///
    /// </summary>
    public class TessBoxControl : Control, INotifyPropertyChanged
    {
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof (bool), typeof (TessBoxControl), new PropertyMetadata(default(bool), (d, a) => ((TessBoxControl)d).OnIsSelectedChanged((bool)a.NewValue)));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            throw new NotImplementedException();
        }

        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof (string), typeof (TessBoxControl), new PropertyMetadata(default(string)));

        public string Value
        {
            get { return (string) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public double Left
        {
            get { return Canvas.GetLeft(this); }
            set
            {
                Canvas.SetLeft(this, value);
                NotifyPropertyChanged("Left");
            }
        }

        public double Top
        {
            get { return Canvas.GetTop(this); }
            set
            {
                Canvas.SetTop(this, value);
                NotifyPropertyChanged("Top");
            }
        }

        private Border _normalBorder = null;
        private Border _selBorder = null;

        static TessBoxControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TessBoxControl), new FrameworkPropertyMetadata(typeof(TessBoxControl)));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TessBoxControl()
        {

        }

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if(handler != null)
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _normalBorder = GetTemplateChild("normalBorder") as Border;
            _selBorder = GetTemplateChild("selBorder") as Border;

        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            NotifyPropertyChanged("RenderSize");
        }

        protected void OnIsSelectedChanged(bool isSelected)
        {
            if (isSelected)
            {
                _normalBorder.Opacity = 0;
                _selBorder.Opacity = 1;
            }
            else
            {
                _normalBorder.Opacity = 1;
                _selBorder.Opacity = 0;
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            //return base.HitTestCore(hitTestParameters);
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

    }
}
