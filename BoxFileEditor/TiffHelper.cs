using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BoxFileEditor
{
    class TiffHelper
    {
        public const UInt16 TiffBigEndian = 0x4d4d;
        public const UInt16 TIffLittleEndian = 0x4949;
        public const UInt16 TiffVersion = 42;

        public static bool IsTiffFile(Stream inStream)
		{
			long savePos = inStream.Position;
			try
			{
				//DON'T dispose the reader, it'll close our base stream!
				var header = new byte[8];
			    inStream.Read(header, 0, header.Length);

				var tiffMagic = BitConverter.ToUInt16(header, 0);
			    var tiffVersion = BitConverter.ToUInt16(header, 2);
			    ulong firstIFD = BitConverter.ToUInt32(header, 4);
                //adjust our endian-ness based on the magic number...
                if (tiffMagic == TIffLittleEndian)
				{
				    //file is little endian
                }
				else if(tiffMagic == TiffBigEndian)
				{
				    tiffVersion = SwapUInt16(tiffVersion);
				    firstIFD = SwapUInt64(firstIFD);
				}
				else
				{
					//throw new Exception("Invalid TIFF Header (Byte Order)");
					return false;
				}

				//another check to make sure this is really a valid TIFF file...
				if(tiffVersion != TiffVersion)
				{
					//throw new Exception("Invalid TIFF Header (Version)");
					return false;
				}

				return true;
			}
			finally 
			{
				inStream.Position = savePos;
			}

		}

        public static UInt16 SwapUInt16(UInt16 val)
        {
            return (UInt16)
                    (
                        (UInt16)(val >> 8) |
                        (UInt16)(val << 8)
                    );
        }

        public static UInt32 SwapUInt32(UInt32 val)
        {
            return (
                        ((val & 0x000000ffU) << 24) |
                        ((val & 0x0000ff00U) << 8) |
                        ((val & 0x00ff0000U) >> 8) |
                        ((val & 0xff000000U) >> 24)
                    );
        }

        public static UInt64 SwapUInt64(UInt64 val)
        {
            return (
                        ((val & 0x00000000000000ffU) << 56) |
                        ((val & 0x000000000000ff00U) << 40) |
                        ((val & 0x0000000000ff0000U) << 24) |
                        ((val & 0x00000000ff000000U) << 8) |
                        ((val & 0x000000ff00000000U) >> 8) |
                        ((val & 0x0000ff0000000000U) >> 24) |
                        ((val & 0x00ff000000000000U) >> 40) |
                        ((val & 0xff00000000000000U) >> 56)
                    );

        }

    }

}
