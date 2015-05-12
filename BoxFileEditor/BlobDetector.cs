using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace BoxFileEditor
{
    public class BlobDetector
    {
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }

        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        
        public int MinMass { get; set; }
        public int MaxMass { get; set; }

        public BlobDetector()
        {
            MinWidth = 2;
            MaxWidth = 50;

            MinHeight = 5;
            MaxHeight = 50;
            
            MinMass = 10;
            MaxMass = 300;
        }

        unsafe byte GetPixelColor(byte* pBits, int stride, int x, int y)
        {
            return *(pBits + (y * stride) + x);
        }

        unsafe void SetPixelColor(byte* pBits, int stride, int x, int y, byte newColor)
        {
            *(pBits + (y * stride) + x) = newColor;
        }

        unsafe private Rectangle GetBoundsRect(BitmapData inputData, Rectangle area, byte color)
        {
            byte* pBits = (byte*)inputData.Scan0.ToPointer();
            int stride = inputData.Stride;

            if (area == Rectangle.Empty)
                area = new Rectangle(0, 0, inputData.Width, inputData.Height);

            int top = -1;
            int bottom = -1;
            int left = -1;
            int right = -1;

            int leftMax = -1;

            for (int y = area.Y; y < area.Bottom; y++)
            {
                for (int x = area.X; x < area.Right; x++)
                {
                    if (GetPixelColor(pBits, stride, x, y) == color)
                    {
                        if (leftMax == -1 || x < leftMax)
                            leftMax = x;
                        top = y;
                        break;
                    }
                }
                if (top != -1)
                    break;
            }

            if (top != -1)
            {
                //search for the bottom, if we don't find a bottom
                //point, it will be the same as the top
                for (int y = area.Bottom - 1; y >= top; y--)
                {
                    for (int x = area.X; x < area.Right; x++)
                    {
                        if (GetPixelColor(pBits, stride, x, y) == color)
                        {
                            if (leftMax == -1 || x < leftMax)
                                leftMax = x;
                            bottom = y;
                            break;
                        }
                    }
                    if (bottom != -1)
                        break;
                }
            }

            //if we didn't find a top & bottom, just leave there is no
            //pixel of the specified color.
            if (top == -1 && bottom == -1)
                return Rectangle.Empty;

            for (int x = area.X; x <= leftMax; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    if (GetPixelColor(pBits, stride, x, y) == color)
                    {
                        left = x;
                        break;
                    }
                }
                if (left != -1)
                    break;
            }

            if (left != -1)
            {
                for (int x = area.Right - 1; x >= leftMax; x--)
                {
                    for (int y = top; y <= bottom; y++)
                    {
                        if (GetPixelColor(pBits, stride, x, y) == color)
                        {
                            right = x;
                            break;
                        }
                    }
                    if (right != -1)
                        break;
                }
            }

            //if we didn't find a left & right, just leave there is no
            //pixel of the specified color.
            if (left == -1 && right == -1)
                return Rectangle.Empty;

            return new Rectangle(left, top, (right - left) + 1, (bottom - top) + 1);
        }

        private void GrowRectangle(ref Rectangle r, int x, int y)
        {
            if (x < r.X)
            {
                r.Width += (r.X - x);
                r.X = x;
            }
            if (y < r.Y)
            {
                r.Height += (r.Y - y);
                r.Y = y;
            }
            if (x > r.Right - 1)
            {
                r.Width += (x - (r.Right - 1));
            }
            if (y > r.Bottom - 1)
            {
                r.Height += (y - (r.Bottom - 1));
            }
        }

        unsafe BlobInfo FillBlob(BitmapData inputData, Point pt, byte newColor)
        {
            byte* pBits = (byte*)inputData.Scan0.ToPointer();
            int stride = inputData.Stride;
            byte oldColor = GetPixelColor(pBits, stride, pt.X, pt.Y);
            var info = new BlobInfo();
            if (oldColor == newColor)
                return info;

            var s = new Stack<Point>(32);
            int h = inputData.Height;
            int w = inputData.Width;
            int y1 = 0;
            bool spanLeft = false;
            bool spanRight = false;

            info.Bounds = new Rectangle(pt, new Size(1, 1));
            s.Push(pt);

            long xCount = 0;
            long yCount = 0;

            while (s.Count != 0)
            {
                pt = s.Pop();
                y1 = pt.Y;
                while (y1 >= 0 && GetPixelColor(pBits, stride, pt.X, y1) == oldColor)
                    y1--;

                y1++;
                spanLeft = spanRight = false;
                while (y1 < h && GetPixelColor(pBits, stride, pt.X, y1) == oldColor)
                {
                    SetPixelColor(pBits, stride, pt.X, y1, newColor);
                    xCount += pt.X;
                    yCount += y1;
                    //keep track of info...
                    GrowRectangle(ref info._bounds, pt.X, y1);
                    info.Mass++;

                    if (!spanLeft && pt.X > 0 && GetPixelColor(pBits, stride, pt.X - 1, y1) == oldColor)
                    {
                        s.Push(new Point(pt.X - 1, y1));
                        spanLeft = true;
                    }
                    else if (spanLeft && pt.X > 0 && GetPixelColor(pBits, stride, pt.X - 1, y1) != oldColor)
                    {
                        spanLeft = false;
                    }
                    if (!spanRight && pt.X < w - 1 && GetPixelColor(pBits, stride, pt.X + 1, y1) == oldColor)
                    {
                        s.Push(new Point(pt.X + 1, y1));
                        spanRight = true;
                    }
                    else if (spanRight && pt.X < w - 1 && GetPixelColor(pBits, stride, pt.X + 1, y1) != oldColor)
                    {
                        spanRight = false;
                    }
                    y1++;
                }
            }

            info.CenterOfMass = new PointF((float)xCount/info.Mass, (float)yCount/info.Mass);

            return info;
        }

        public BlobInfo[] LocateBlobs(Bitmap image)
        {
            return LocateBlobs(image, new Rectangle(0, 0, image.Width, image.Height));
        }

        unsafe public BlobInfo[] LocateBlobs(Bitmap image, Rectangle area)
        {
            const byte clrSymbol = 0;
            const byte clrCompleted = 50;

            var working = (Bitmap)image.Clone();
            var inputData = working.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            try
            {
                var blobs = new List<BlobInfo>();
                var bits = (byte*)inputData.Scan0.ToPointer();
                int stride = inputData.Stride;

                var rLast = Rectangle.Empty;
                while (true)
                {
                    var rMain = GetBoundsRect(inputData, rLast, clrSymbol);
                    if (rMain.IsEmpty)
                        break;

                    rLast = rMain;

                    int x = rMain.X;
                    for (int y = rMain.Y; y < rMain.Bottom; y++)
                    {
                        if (GetPixelColor(bits, stride, x, y) == clrSymbol)
                        {
                            //fill blob to mark it.
                            var blob = FillBlob(inputData, new Point(x, y), clrCompleted);
                            //get the bounds of the marked blob...
                            if (blob.Bounds.Width >= MinWidth && blob.Bounds.Height >= MinHeight
                            && blob.Bounds.Width <= MaxWidth && blob.Bounds.Height <= MaxHeight)
                            {
                                blob.Bounds.Offset(area.Location);
                                blob.Bounds.Inflate(2, 2);
                                blobs.Add(blob);
                            }

                        }

                    }

                }
                //return the list in top to bottom, left to right order
                return blobs.OrderBy(b => b.Bounds.Y).ThenBy(b => b.Bounds.X).ToArray();
            }
            finally 
            {
                if(inputData != null)
                    working.UnlockBits(inputData);
            }

        }

    }

    public class BlobInfo
    {
        protected internal Rectangle _bounds;
        public Rectangle Bounds
        {
            get { return _bounds; }
            set { _bounds = value; }
        }

        public int Mass { get; protected internal set; }

        public float Fullness
        {
            get { return (float)Mass / (float)(_bounds.Width * _bounds.Height); }
        }

        public Point Center
        {
            get { return new Point(_bounds.X + (_bounds.Width/2), _bounds.Y + (_bounds.Height/2)); }
        }

        public PointF CenterOfMass { get; protected internal set; }
    }
}
