using System;
using System.Collections.Generic;
using System.IO;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.ArchiveWrappers;
using RageLib.GTA5.Resources.PC;


namespace GTAUtil
{
    partial class Program
    {
        static void HandleCreateArchiveOptions(string[] args)
        {
            CommandLine.Parse<CreateArchiveOptions>(args, (opts, gOpts) =>
            {
                EnsurePath();
                EnsureKeys();

                if (opts.InputFolder == null)
                {
                    Console.WriteLine("Please provide input folder with -i --input");
                    return;
                }

                if (opts.OutputFolder == null)
                {
                    Console.WriteLine("Please provide output folder with -o --output");
                    return;
                }

                if (opts.Name == null)
                {
                    Console.WriteLine("Please provide rpf name with -n --name");
                    return;
                }

                string rpfPath = opts.OutputFolder + "\\" + opts.Name + ".rpf";

                using (RageArchiveWrapper7 rpf = RageArchiveWrapper7.Create(rpfPath))
                {
                    var queue = new List<Tuple<string, IArchiveDirectory, RageArchiveWrapper7>>() {
                        new Tuple<string, IArchiveDirectory, RageArchiveWrapper7>(opts.InputFolder, rpf.Root, rpf)
                    };

                    var subRpfs = new List<Tuple<DirectoryInfo, IArchiveDirectory, RageArchiveWrapper7>>();

                    rpf.archive_.Encryption = RageArchiveEncryption7.NG;

                    var rpfs = new List<RageArchiveWrapper7>();

                    while (queue.Count > 0)
                    {
                        var folder  = queue[0].Item1;
                        var curr    = queue[0].Item2;
                        var currRpf = queue[0].Item3;

                        if (rpfs.IndexOf(currRpf) == -1)
                            rpfs.Add(currRpf);

                        Console.WriteLine(folder);

                        queue.RemoveAt(0);

                        string              newFolder  = null;
                        IArchiveDirectory   newCurr    = null;
                        RageArchiveWrapper7 newCurrRpf = null;

                        string[] folders = Directory.GetDirectories(folder);
                        string[] files   = Directory.GetFiles(folder);

                        for (int i = 0; i < folders.Length; i++)
                        {
                            var folderInfo = new DirectoryInfo(folders[i]);

                            newFolder = folders[i];

                            if (folders[i].EndsWith(".rpf"))
                            {
                                var tmpStream = new FileStream(Path.GetTempFileName(), FileMode.Open);
                                var subRpf = RageArchiveWrapper7.Create(tmpStream, folderInfo.Name);

                                subRpf.archive_.Encryption = RageArchiveEncryption7.NG;

                                subRpfs.Add(new Tuple<DirectoryInfo, IArchiveDirectory, RageArchiveWrapper7>(folderInfo, curr, subRpf));

                                newCurr = subRpf.Root;
                                newCurrRpf = subRpf;
                            }
                            else
                            {
                                var directory = curr.CreateDirectory();
                                directory.Name = folderInfo.Name;

                                newCurr    = directory;
                                newCurrRpf = currRpf;
                            }

                            queue.Add(new Tuple<string, IArchiveDirectory, RageArchiveWrapper7>(newFolder, newCurr, newCurrRpf));

                        }

                        if(folders.Length + files.Length == 0)
                        {
                            Console.WriteLine("  .\\.empty");
                            var binFile = curr.CreateBinaryFile();
                            binFile.Name = ".empty";

                            var ms = new MemoryStream(1);

                            ms.WriteByte(0);

                            ms.Flush();

                            binFile.Import(ms);
                        }

                        for (int i = 0; i < files.Length; i++)
                        {
                            string file = files[i];
                            var fileInfo = new FileInfo(file);

                            bool isResource = false;

                            for (int j = 0; j < ResourceFileTypes_GTA5_pc.AllTypes.Count; j++)
                            {
                                var type = ResourceFileTypes_GTA5_pc.AllTypes[j];

                                if (file.EndsWith(type.Extension))
                                {
                                    Console.WriteLine("  " + file);

                                    isResource = true;

                                    var resource = curr.CreateResourceFile();
                                    resource.Name = fileInfo.Name;
                                    resource.Import(file);

                                    break;
                                }
                            }

                            if (!isResource)
                            {
                                Console.WriteLine("  " + file);
                                var binFile = curr.CreateBinaryFile();
                                binFile.Name = fileInfo.Name;
                                binFile.Import(file);
                            }
                        }

                    }

                    rpfs.Reverse();

                    for(int i=0; i < subRpfs.Count; i++)
                    {
                        var subRpf = subRpfs[i];

                        var file = subRpf.Item2.CreateBinaryFile();
                        file.Name = subRpf.Item1.Name;

                        subRpf.Item3.Flush();

                        file.Import(subRpf.Item3.archive_.BaseStream);
                    }

                    for (int i = 0; i < rpfs.Count; i++)
                    {
                        rpfs[i].Flush();

                        if(i + 1 < rpfs.Count)
                        {
                            var stream = (FileStream)rpfs[i].archive_.BaseStream;
                            string fileName = stream.Name;

                            rpfs[i].Dispose();

                            File.Delete(fileName);
                        }
                    }
                }
            });
        }
    }
}
