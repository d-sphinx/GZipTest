using System;
using System.Collections.Generic;
using System.IO;

namespace GZipTest
{
    class Program
    {
        static FileStream inFile, outFile;

        static Queue<Worker> wrkQueue = new Queue<Worker>();
        static int MAX_THREADS = Environment.ProcessorCount;

        static long bytesTotal = 0;
        static bool isPack;

        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Wrong number of arguments");
                return 1;
            }

            if (args[0] == "compress")
            {
                isPack = true;
            }
            else if (args[0] == "decompress")
            {
                isPack = false;
            }
            else
            {
                Console.WriteLine("Bad command");
                return 1;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine("Input doesn't exist");
                return 1;
            }

            if (new FileInfo(args[1]).Length == 0)
            {
                Console.WriteLine("File is empty");
                return 1;
            }

            try
            {
                inFile = new FileStream(args[1], FileMode.Open);
                outFile = new FileStream(args[2], FileMode.Create);

                while (inFile.Position < inFile.Length)
                {
                    wrkQueue.Enqueue(new Worker(inFile, isPack));

                    if (wrkQueue.Count == MAX_THREADS)
                    {
                        bytesTotal += wrkQueue.Dequeue().Flush(outFile);
                    }

                    Update();
                }

                foreach (Worker worker in wrkQueue)
                {
                    bytesTotal += worker.Flush(outFile);

                    Update();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
                return 1;
            }

            inFile.Close();
            outFile.Close();

            Console.WriteLine(", done.");
            return 0;
        }

        static void Update()
        {
            Console.Write("\r{0}: {1}% complete", isPack ? "Packing" : "Unpacking", bytesTotal * 100 / inFile.Length);
        }
    }
}
