using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace VoxelPlay
{

    public class SaveGameCustomDataWriter
    {

        public struct DataChunk
        {
            public string tag;
            public byte [] compressedData;
        }

        List<DataChunk> dataContainer = new List<DataChunk> ();

        public void Clear () => dataContainer.Clear ();

        public void Add (string tag, byte [] data)
        {
            data = Compress (data);
            DataChunk d = new DataChunk ();
            d.tag = tag;
            d.compressedData = data;
            dataContainer.Add (d);
        }

        public void Flush (BinaryWriter bw)
        {
            bw.Write ((Int16)dataContainer.Count);
            foreach (DataChunk d in dataContainer) {
                bw.Write (d.tag);
                bw.Write (d.compressedData.Length);
                bw.Write (d.compressedData);
            }
            dataContainer.Clear ();
        }


        public static byte [] Compress (byte [] data)
        {
            MemoryStream output = new MemoryStream ();
            using (DeflateStream dstream = new DeflateStream (output, CompressionLevel.Optimal)) {
                dstream.Write (data, 0, data.Length);
            }
            return output.ToArray ();
        }

        public static byte [] Decompress (byte [] data)
        {
            MemoryStream input = new MemoryStream (data);
            MemoryStream output = new MemoryStream ();
            using (DeflateStream dstream = new DeflateStream (input, CompressionMode.Decompress)) {
                dstream.CopyTo (output);
            }
            return output.ToArray ();
        }
    }

}