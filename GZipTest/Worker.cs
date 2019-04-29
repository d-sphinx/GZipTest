using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    class Worker
    {
        Thread wrkThread;
        Exception wrkEx;

        byte[] inBuffer, outBuffer;
        int bytesRead;
        int bytesUnpacked;
        int outBufLength;

        MemoryStream memStream = new MemoryStream();

        bool isPack;

        public Worker(Stream input, bool isPack)
        {
            this.isPack = isPack;

            if (isPack)
            {
                int bufLength = 1048576;

                inBuffer = new byte[bufLength];
                bytesRead = input.Read(inBuffer, 0, bufLength);

                wrkThread = new Thread(Pack);
            }
            else
            {
                int blockLength, hdrLength = 8;

                inBuffer = new byte[hdrLength];
                input.Read(inBuffer, 0, hdrLength);
                input.Seek(-hdrLength, SeekOrigin.Current);
                blockLength = BitConverter.ToInt32(inBuffer, 4);
                inBuffer = new byte[blockLength];
                bytesRead = input.Read(inBuffer, 0, blockLength);
                outBufLength = BitConverter.ToInt32(inBuffer, inBuffer.Length - 4);
                outBuffer = new byte[outBufLength];

                wrkThread = new Thread(Unpack);
            }

            wrkThread.Start();
        }

        public int Flush(Stream output)
        {
            wrkThread.Join();

            if (wrkEx != null)
                throw wrkEx;

            if (isPack)
                memStream.CopyTo(output);
            else
                output.Write(outBuffer, 0, bytesUnpacked);

            return bytesRead;
        }

        private void Pack()
        {
            try
            {
                using (GZipStream packStream = new GZipStream(memStream, CompressionMode.Compress, true))
                {
                    packStream.Write(inBuffer, 0, bytesRead);
                }
                memStream.Position = 4;
                memStream.Write(BitConverter.GetBytes((int)memStream.Length), 0, 4);
                memStream.Position = 0;
            }
            catch (Exception e)
            {
                wrkEx = e;
            }
        }

        private void Unpack()
        {
            try
            {
                using (Stream stream = new MemoryStream(inBuffer))
                using (GZipStream unpackStream = new GZipStream(stream, CompressionMode.Decompress, true))
                {
                    bytesUnpacked = unpackStream.Read(outBuffer, 0, outBufLength);
                }
            }
            catch (Exception e)
            {
                wrkEx = e;
            }
        }
    }
}
