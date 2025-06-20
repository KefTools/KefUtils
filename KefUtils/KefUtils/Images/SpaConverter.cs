using FreeImageAPI;

using KefUtils.Images.DXT;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KefUtils.Images
{
    public static class SpaConverter
    {
        public static Bitmap SpaToBitmap(SpaImage Image, int FrameIndex = 0, bool Compressed = true) {
            if (Image.Header == null || Image.FrameHeaders == null || FrameIndex >= Image.FrameHeaders.Length)
                throw new InvalidOperationException("SpaImage not fully loaded or invalid frame index.");

            var frame = Image.FrameHeaders[FrameIndex];
            int frameWidth = frame.Width;
            int frameHeight = frame.Height;

            if(Compressed)
                return DXTToBitmap(Image.SegmentData, frameWidth, frameHeight, frame.SegmentHeaders, frame.HasAlpha == 0);

            return RawToBitmap(Image.SegmentData, frameWidth, frameHeight, frame.SegmentHeaders);
        }
        private static Bitmap RawToBitmap(byte[] data, int width, int height, SpaImageSegmentHeader[] Segments) {
            using MemoryStream ms = new MemoryStream();
            foreach (var segment in Segments)
            {
                if (segment.Length == 0) continue;
                ms.Write(data, (int)segment.Offset, (int)segment.Length);
            }
            byte[] combined = ms.ToArray();

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int numBytes = bmpData.Height * bmpData.Stride;

            for (int y = 0; y < bmpData.Height; y++)
            {
                int srcRow = (bmpData.Height - 1 - y) * bmpData.Stride;
                int dstRow = y * bmpData.Stride;
                Marshal.Copy(combined, srcRow, bmpData.Scan0 + dstRow, bmpData.Stride);
            }

            bmp.UnlockBits(bmpData);

            return bmp;

        }
        private static Bitmap DXTToBitmap(byte[] segmentData, int frameWidth, int frameHeight, SpaImageSegmentHeader[] segments, bool hasAlpha)
        {
            Bitmap final = new Bitmap(frameWidth, frameHeight, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(final);
            g.Clear(Color.Transparent);

            foreach (var segment in segments)
            {
                if (segment.Length == 0) continue;

                // 1. Extract compressed DXT segment
                byte[] dxt = new byte[segment.Length];
                Array.Copy(segmentData, segment.Offset, dxt, 0, segment.Length);

                // 2. Build DDS header for segment
                byte[] ddsBytes = new DDSImage(new DDSImageHeader
                {
                    Width = (uint)segment.Width,
                    Height = (uint)segment.Height,
                    PixelFormat = new DDSPixelFormat() { 
                        FourCC = hasAlpha ? (uint)DDSFourCC.DXT1 : (uint)DDSFourCC.DXT5,
                    }, // Default to DXT5
                    Caps = new DDSCaps(),
                    PitchOrLinearSize = (uint)segment.Length
                }, dxt).GetBytes();

                // 3. Load segment with FreeImage
                GCHandle handle = GCHandle.Alloc(ddsBytes, GCHandleType.Pinned);
                IntPtr ptr = handle.AddrOfPinnedObject();

                FIMEMORY mem = FreeImage.OpenMemory(ptr, (uint)ddsBytes.Length);
                FREE_IMAGE_FORMAT fif = FreeImage.GetFileTypeFromMemory(mem, 0);
                if (fif == FREE_IMAGE_FORMAT.FIF_UNKNOWN)
                {
                    handle.Free();
                    throw new Exception("Unknown DDS format in segment.");
                }

                FIBITMAP fiBitmap = FreeImage.LoadFromMemory(fif, mem, FREE_IMAGE_LOAD_FLAGS.DEFAULT);
                FreeImage.CloseMemory(mem);
                handle.Free();

                if (fiBitmap.IsNull)
                    throw new Exception("FreeImage failed to decode segment.");

                FIBITMAP rgba = FreeImage.ConvertTo32Bits(fiBitmap);
                FreeImage.Unload(fiBitmap);

                int segW = (int)FreeImage.GetWidth(rgba);
                int segH = (int)FreeImage.GetHeight(rgba);
                Bitmap tile = new Bitmap(segW, segH, PixelFormat.Format32bppArgb);

                // 4. Copy bits to .NET bitmap
                BitmapData data = tile.LockBits(
                    new Rectangle(0, 0, segW, segH),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                IntPtr srcBits = FreeImage.GetBits(rgba);
                int numBytes = segH * data.Stride;
                byte[] buffer = new byte[numBytes];
                Marshal.Copy(srcBits, buffer, 0, buffer.Length);

                // Flip Y axis (FreeImage's origin is bottom-left)
                for (int y = 0; y < segH; y++)
                {
                    int srcRow = (segH - 1 - y) * data.Stride;
                    int dstRow = y * data.Stride;
                    Marshal.Copy(buffer, srcRow, data.Scan0 + dstRow, data.Stride);
                }

                tile.UnlockBits(data);
                FreeImage.Unload(rgba);

                // 5. Composite segment into final image at X/Y offset
                g.DrawImage(tile, segment.XOffset, segment.YOffset);
                tile.Dispose();
            }

            return final;
        }
    }
}
