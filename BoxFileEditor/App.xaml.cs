using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;

namespace BoxFileEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            //using (var img = (System.Drawing.Bitmap)Image.FromFile(@"D:\ScottDev\TesseractOcr\Training\ocr.e13b.exp0.tif"))
            //{
            //    var blobDetector = new BlobDetector();
            //    var blobs = blobDetector.LocateBlobs(img);
            //    Debug.WriteLine("{0} blobs found!", blobs.Length);
            //}
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            
        }
    }
}
