using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.ArchiveWrappers;
using RageLib.GTA5.Utilities;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleExtractArchiveOptions(string[] args)
        {
            CommandLine.Parse<ExtractArchiveOptions>(args, (opts, gOpts) =>
            {
                EnsurePath();
                EnsureKeys();

                if (opts.InputFile == null)
                {
                    Console.WriteLine("Please provide input archive with -i --input");
                    return;
                }

                if (opts.OutputFolder == null)
                {
                    Console.WriteLine("Please provide output folder with -o --output");
                    return;
                }

                var fileInfo = new FileInfo(opts.InputFile);
                var fileStream = new FileStream(opts.InputFile, FileMode.Open);

                var inputArchive = RageArchiveWrapper7.Open(fileStream, fileInfo.Name);

                var queue = new List<Tuple<string, RageArchiveWrapper7, bool>>() { new Tuple<string, RageArchiveWrapper7, bool>(fileInfo.FullName, inputArchive, false) };

                while(queue.Count > 0)
                {
                    var fullPath    = queue[0].Item1;
                    var rpf         = queue[0].Item2;
                    var isTmpStream = queue[0].Item3;

                    queue.RemoveAt(0);

                    ArchiveUtilities.ForEachFile(fullPath.Replace(fileInfo.FullName, ""), rpf.Root, rpf.archive_.Encryption, (string fullFileName, IArchiveFile file, RageArchiveEncryption7 encryption) =>
                    {
                        string path = opts.OutputFolder + fullFileName;
                        string dir = Path.GetDirectoryName(path);

                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        Console.WriteLine(fullFileName);

                        if (file.Name.EndsWith(".rpf"))
                        {
                            try
                            {
                                var tmpStream = new FileStream(Path.GetTempFileName(), FileMode.Open);

                                file.Export(tmpStream);
                                RageArchiveWrapper7 archive = RageArchiveWrapper7.Open(tmpStream, file.Name);
                                queue.Add(new Tuple<string, RageArchiveWrapper7, bool>(fullFileName, archive, true));
                            }
                            catch(Exception e)
                            {
                                Console.Error.WriteLine(e.Message);
                            }

                        }
                        else
                        {
                            if(file.Name.EndsWith(".xml") || file.Name.EndsWith(".meta"))
                            {
                                byte[] data = Utils.GetBinaryFileData((IArchiveBinaryFile)file, encryption);
                                string xml;

                                if (data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)  // Detect BOM
                                {
                                    xml = Encoding.UTF8.GetString(data, 3, data.Length - 3);
                                }
                                else
                                {
                                    xml = Encoding.UTF8.GetString(data);
                                }

                                File.WriteAllText(path, xml, Encoding.UTF8);
                            }
                            else
                            {
                                file.Export(path);
                            }
                        }
                    });

                    var stream = (FileStream)rpf.archive_.BaseStream;
                    string fileName = stream.Name;

                    rpf.Dispose();

                    if (isTmpStream)
                    {
                        File.Delete(fileName);
                    }
                }

            });
        }
    }
}
