using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KefUtils.IO
{
    public class Blockfile
    {
        public Blockfile() {

        }

        public Blockfile(byte[] data) { 
            MemoryStream stream = new MemoryStream(data);
            BinaryReader br = new BinaryReader(stream);

            //Make Header
            Header = new BlockfileHeader()
            {
                Version = br.ReadUInt32(),
                Size = br.ReadUInt32(),
                PathLength = br.ReadUInt32(),
                Magic = br.ReadUInt32(),
            };

            //Parse Segments
            List<BlockfileSegment> segments = new List<BlockfileSegment>();

            for (int i = 0; i < Header.Size; i++) {
                BlockfileSegment segment = new BlockfileSegment()
                {
                    AssetGuid = new Guid(br.ReadBytes(16)),
                    FilePath = BitConverter.ToString(br.ReadBytes((int)Header.PathLength)),
                    Offset = br.ReadUInt32(),
                    Length = br.ReadUInt32(),
                    Magic = br.ReadUInt32(),
                };
                segments.Add(segment);
            }

            Segments = segments.ToArray();

            long bytesLeft = stream.Length - stream.Position;
            Data = br.ReadBytes((int)bytesLeft);
        }

        public BlockfileHeader? Header { get; set; }
        public BlockfileSegment?[] Segments { get; set; }
        public byte[] Data { get; set; }

        public void WriteToFile(string path) {
            string tempPath = path + ".tmp";
            using (FileStream fileStream = File.Create(tempPath))
            using (BinaryWriter bw = new BinaryWriter(fileStream)) { 

                bw.Write(Header.Version);
                bw.Write(Header.Size);
                bw.Write(Header.PathLength);
                bw.Write(Header.Magic);

                foreach (BlockfileSegment segment in Segments)
                {
                    bw.Write(segment.AssetGuid.ToByteArray());

                    byte[] buffer = new byte[Header.PathLength];
                    byte[] pathdata = Encoding.ASCII.GetBytes(segment.FilePath);

                    int length = Math.Min(pathdata.Length, (int)Header.PathLength);
                    Array.Copy(pathdata, buffer, length);
                    bw.Write(buffer);

                    bw.Write(segment.Offset);
                    bw.Write(segment.Length);
                    bw.Write(segment.Magic);
                }

                bw.Write(Data);

                bw.Close();
                fileStream.Close();
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
    }
    public class BlockfileHeader { 
        public uint Version { get; set; }
        public uint Size { get; set; }
        public uint PathLength { get; set; }
        public uint Magic { get; set; }
    }
    public class BlockfileSegment { 
        public Guid AssetGuid { get; set; }
        public string FilePath { get; set; }
        public uint Offset { get; set; }
        public uint Length { get; set; }
        public uint Magic { get; set; }
    }
}
