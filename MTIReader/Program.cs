using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Mono.Options;
using WamWooWam.StructReader;

namespace MTIReader
{
    class Program
    {
        static void Main(string[] args)
        {
            string inPath = null;
            string outPath = null;
            bool showedHelp = false;

            var set = new OptionSet()
            {
                { "in=", (s) => inPath = s },
                { "out=", (s) => outPath = s },
                { "help", (s) => { WriteHelp(); showedHelp = true; }  },
                { "?", (s) => { WriteHelp(); showedHelp = true; }  }
            };

            set.Parse(args);

            if (showedHelp)
                return;

            if (string.IsNullOrWhiteSpace(inPath))
            {
                WriteHelp();
                return;
            }

            var isMti = false;
            var ext = Path.GetExtension(inPath).ToLowerInvariant();
            if (ext == ".mti")
            {
                isMti = true;
            }
            else if (ext != ".xml")
            {
                WriteHelp();
                Console.WriteLine();
                Console.WriteLine("Unable to determine format of input file!");
                return;
            }

            if (string.IsNullOrWhiteSpace(outPath))
            {
                var newExt = isMti ? ".xml" : ".mti";
                outPath = Path.ChangeExtension(inPath, newExt);
            }

            if (isMti)
            {
                MtiToXml(inPath, outPath);
            }
            else
            {
                XmlToMti(inPath, outPath);
            }
        }

        private static void MtiToXml(string inPath, string outPath)
        {
            var data = File.ReadAllBytes(inPath);
            var file = new MtiFile();
            DataLoader.Load(ref file, data);

            var doc = new XDocument(
                new XElement("MtiFile",
                    new XAttribute("InstanceCount", file.header.instance_count),
                    file.instances.Select(i => new XElement("MtiInstance",
                        new XAttribute("type", i.type),
                        new XElement("Position",
                            new XElement("x", i.x),
                            new XElement("y", i.y),
                            new XElement("z", i.z)
                        )
                    ))
                )
            );

            File.WriteAllText(outPath, doc.ToString());
        }

        private static void XmlToMti(string inPath, string outPath) => throw new NotImplementedException();

        private static void WriteHelp()
        {
            Console.WriteLine("MTIReader - Converter for Sonic Generations/Unleashed .mti files.");
            Console.WriteLine();
            Console.WriteLine("Usage: sharpfuck.exe");
            Console.WriteLine("    --help, -?");
            Console.WriteLine("        Show this help text.");
            Console.WriteLine();
            Console.WriteLine("    --in <filename>");
            Console.WriteLine("        Specifies an input XML or MTI file to be converted.");
            Console.WriteLine();
            Console.WriteLine("    --out <filename>");
            Console.WriteLine("        Specifies the output for a converted file. Defaults to the name of the input");
            Console.WriteLine("        file with the file extension changed.");
            Console.WriteLine();
        }
    }

    struct MtiHeader
    {
        public uint magic; // "MTI "
        public uint unk_1; // generally 1
        public uint instance_count;
        public uint unk_2;
        public uint unk_3; // generally 0
        public uint unk_4; // generally 0
        public uint unk_5; // generally 0
        public uint data_offset; // generally 32
    }

    struct MtiInstance
    {
        public float x;
        public float y;
        public float z;
        public byte type;

        // something here probably contains in/out ranges for the specific instance
        public byte unk_1; // probably flags and additional type data
        public byte unk_2; // ^^
        public byte unk_3; // ^^
        public uint unk_4; // ^^
        public uint unk_5; // ^^
    }

    [Endianness(Endianness.Big)]
    struct MtiFile
    {
        public MtiHeader header;

        [OffsetRef("header.data_offset")]
        [ArraySizeRef("header.instance_count")]
        public MtiInstance[] instances;
    }

}
