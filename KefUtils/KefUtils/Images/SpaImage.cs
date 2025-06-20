using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using FreeImageAPI;

using KefUtils.Images.DXT;

namespace KefUtils.Images
{
    public class SpaImage {
        public SpaImage(byte[] Spa)
        {
            using var ms = new MemoryStream(Spa);
            using var reader = new BinaryReader(ms);

            Header = new SpaImageHeader
            {
                Code = reader.ReadBytes(2),
                Version = reader.ReadInt16(),
                NumFrames = reader.ReadInt16(),
                Unused = reader.ReadInt16(),
                Flags = reader.ReadUInt32(),
                FramesPerSecond = reader.ReadSingle(),
                DataStartOffset = reader.ReadInt32()
            };

            var frames = new List<SpaImageFrameHeader>();

            for (int i = 0; i < Header.NumFrames; i++)
            {
                var frame = new SpaImageFrameHeader
                {
                    Mode = reader.ReadInt16(),
                    HasAlpha = reader.ReadInt16(),
                    Width = reader.ReadInt16(),
                    Height = reader.ReadInt16(),
                    HotSpotX = reader.ReadInt16(),
                    HotSpotY = reader.ReadInt16(),
                    Unused2 = reader.ReadInt16(),
                    NumSegments = reader.ReadInt16()
                };

                frame.SegmentHeaders = new SpaImageSegmentHeader[frame.NumSegments];

                for (int j = 0; j < frame.NumSegments; j++)
                {
                    frame.SegmentHeaders[j] = new SpaImageSegmentHeader
                    {
                        Flags = reader.ReadInt16(),
                        NumMipMaps = reader.ReadInt16(),
                        Width = reader.ReadInt16(),
                        Height = reader.ReadInt16(),
                        XOffset = reader.ReadInt16(),
                        YOffset = reader.ReadInt16(),
                        Length = reader.ReadInt32(),
                        Offset = reader.ReadInt32()
                    };
                }

                frames.Add(frame);
            }

            FrameHeaders = frames.ToArray();


            ms.Position = Header.DataStartOffset;
            SegmentData = reader.ReadBytes((int)(ms.Length - ms.Position));
        }
        public SpaImageHeader? Header { get; private set; }
        public SpaImageFrameHeader[]? FrameHeaders { get; private set; }
        public byte[] SegmentData { get; private set; }
    }
    public class SpaImageHeader { 
        public byte[] Code { get; set; } = new byte[2];
        public short Version { get; set; }
        public short NumFrames { get; set; }
        //Padding
        public short Unused { get; set; }
        public uint Flags { get; set; }
        public float FramesPerSecond { get; set; }
        public int DataStartOffset { get; set; }
    }
    public class SpaImageFrameHeader {
        public short Mode { get; set; }
        public short HasAlpha { get; set; }
        public short Width { get; set; }
        public short Height { get; set; }
        public short HotSpotX { get; set; }
        public short HotSpotY { get; set; }
        //Padding
        public short Unused2 { get; set; }
        public short NumSegments { get; set; }
        public SpaImageSegmentHeader[]? SegmentHeaders { get; set; }
    }
    public class SpaImageSegmentHeader {
        public short Flags { get; set; }
        public short NumMipMaps { get; set; }
        public short Width { get; set; }
        public short Height { get; set; }
        public short XOffset { get; set; }
        public short YOffset { get; set; }
        public int Length { get; set; }
        public int Offset { get; set; }
    }

    [Flags]
    public enum SpaFlags : uint
    {
        AUTOREVERSE = 0x00000001,
        AUTOLOOP = 0x00000002,
        AUTODIE = 0x00000004,
        BLEND = 0x00000010,
        PTC = 0x00000020
    }

    [Flags]
    public enum SpaSegmentFlags : short
    {
        ONEBITALPHA = 1 << 3,
        HASMIPDATA = 1 << 6
    }
}
