using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxFileEditor
{
    public static class BitmapHelper
    {
        public static int GetBitmapDataStride(int bitsPerPixel, int pixelWidth)
        {
            int bytesPerRow = 0;
            //raw bytes per row required
            if (bitsPerPixel == 1)
                bytesPerRow = (pixelWidth + 7) / 8;
            else if (bitsPerPixel == 8)
                bytesPerRow = pixelWidth;
            else if (bitsPerPixel == 32)
                bytesPerRow = pixelWidth * 4;
            //stride must be aligned on 4 byte boundary...
            int stride = ((bytesPerRow + 3) & ~3);
            return stride;
        }

        public static int GetBitmapDataByteCount(int bitsPerPixel, int pixelWidth, int pixelHeight)
        {
            return pixelHeight * GetBitmapDataStride(bitsPerPixel, pixelWidth);
        }

    }
}
