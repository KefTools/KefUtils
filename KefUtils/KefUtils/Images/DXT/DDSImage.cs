using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KefUtils.Images.DXT
{
    public class DDSImage { 
        public DDSImage(DDSImageHeader Header, byte[] Data) {
            this.Header = Header;
            this.Data = Data;
        }
        public DDSImageHeader? Header { get; set; }
        private byte[] Data { get; set; }

        public byte[] GetBytes() {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            if (Header == null) {
                throw new NullReferenceException("Header does not exist or is not valid in 'DDSImage'");
            }

            if (Header.PixelFormat == null) {
                throw new NullReferenceException("Header does not contain valid Pixel Format data!");
            }

            if (Header.Caps == null)
            {
                throw new NullReferenceException("Header does not contain Caps data!");
            }

            //Header
            bw.Write(Encoding.ASCII.GetBytes(Header.Code));
            bw.Write(Header.Size);
            bw.Write(Header.Flags);
            bw.Write(Header.Height);
            bw.Write(Header.Width);
            bw.Write(Header.PitchOrLinearSize);
            bw.Write(Header.Depth);
            bw.Write(Header.MipMapCount);

            //Reserved
            byte[] res1 = new byte[Header.Reserved1.Length * sizeof(uint)];
            Buffer.BlockCopy(Header.Reserved1, 0, res1, 0, res1.Length);
            bw.Write(res1);

            //Pixel Format
            bw.Write(Header.PixelFormat.Size);
            bw.Write(Header.PixelFormat.Flags);
            bw.Write(Header.PixelFormat.FourCC);
            bw.Write(Header.PixelFormat.RGBBitCount);
            bw.Write(Header.PixelFormat.RBitMask);
            bw.Write(Header.PixelFormat.GBitMask);
            bw.Write(Header.PixelFormat.BBitMask);
            bw.Write(Header.PixelFormat.ABitMask);

            //DDS Caps
            bw.Write(Header.Caps.Caps1);
            bw.Write(Header.Caps.Caps2);
            bw.Write(Header.Caps.Caps3);
            bw.Write(Header.Caps.Caps4);

            //Reserved 2
            bw.Write(Header.Reserved2);

            //Data
            bw.Write(Data);

            return ms.ToArray();
        }
    }
    public class DDSImageHeader {
        public string Code { get; } = "DDS ";
        public uint Size { get; } = (uint)124;
        public uint Flags { get; } = (uint)(
            DDSHeaderFlags.DDSD_CAPS        |
            DDSHeaderFlags.DDSD_HEIGHT      |
            DDSHeaderFlags.DDSD_WIDTH       |
            DDSHeaderFlags.DDSD_PIXELFORMAT |
            DDSHeaderFlags.DDSD_LINEARSIZE
        );
        public uint Height { get; set; }
        public uint Width { get; set; }
        public uint PitchOrLinearSize { get; set; }
        public uint Depth { get; set; } = 0u;
        public uint MipMapCount { get; set; } = 1u;

        //Reserved Data
        public uint[] Reserved1 { get; } = new uint[11];

        public DDSPixelFormat? PixelFormat { get; set; }
        public DDSCaps? Caps { get; set; }

        public uint Reserved2 { get; set; } = 0u;
    }

    public class DDSPixelFormat {
        public uint Size { get; } = (uint)32;
        public uint Flags { get; } = (uint)DDSPixelFormatFlags.DDPF_FOURCC;
        public uint FourCC { get; set; } = (uint)DDSFourCC.DXT5;
        public uint RGBBitCount { get; set; }
        public uint RBitMask { get; set; }
        public uint GBitMask { get; set; }
        public uint BBitMask { get; set; }
        public uint ABitMask { get; set; }
    }

    public class DDSCaps {
        public uint Caps1 { get; set; } = (uint)(0x1000);
        public uint Caps2 { get; set; }
        public uint Caps3 { get; set; }
        public uint Caps4 { get; set; }
    }

    [Flags]
    public enum DDSHeaderFlags : uint
    {
        None = 0x00000000,
        DDSD_CAPS = 0x00000001,   // Required
        DDSD_HEIGHT = 0x00000002,   // Required
        DDSD_WIDTH = 0x00000004,   // Required
        DDSD_PITCH = 0x00000008,   // Used when pitch is provided
        DDSD_PIXELFORMAT = 0x00001000,   // Required
        DDSD_MIPMAPCOUNT = 0x00020000,   // Valid mipmap count
        DDSD_LINEARSIZE = 0x00080000,   // Used for compressed formats
        DDSD_DEPTH = 0x00800000    // Used for volume textures
    }
    [Flags]
    public enum DDSPixelFormatFlags : uint
    {
        None = 0x00000000,
        DDPF_ALPHAPIXELS = 0x00000001, // Texture contains alpha data; dwABitMask is valid
        DDPF_ALPHA = 0x00000002, // Used for alpha-only uncompressed formats
        DDPF_FOURCC = 0x00000004, // Texture is compressed (e.g., DXT1–DXT5)
        DDPF_RGB = 0x00000040, // Texture is uncompressed RGB data
        DDPF_YUV = 0x00000200, // Texture contains YUV data (rare)
        DDPF_LUMINANCE = 0x00020000  // Texture is grayscale (luminance)
    }
    public enum DDSFourCC : uint
    {
        DXT1 = 0x31545844, // 'DXT1' - BC1: 1-bit alpha or none
        DXT3 = 0x33545844, // 'DXT3' - BC2: explicit alpha
        DXT5 = 0x35545844, // 'DXT5' - BC3: interpolated alpha
        RXGB = 0x42475852, // 'RXGB' - variation of DXT5 used in some engines
        ATI1 = 0x31495441, // 'ATI1' - BC4: single-channel (grayscale or red)
        ATI2 = 0x32495441, // 'ATI2' - BC5: two-channel (e.g. normal maps)
    }
}
