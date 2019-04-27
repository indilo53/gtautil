using System;
using System.Collections.Generic;
using System.IO;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.ArchiveWrappers;
using RageLib.GTA5.Utilities;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleFixArchiveOptions(string[] args)
        {
            CommandLine.Parse<FixArchiveOptions>(args, (opts, gOpts) =>
            {
                EnsurePath();
                EnsureKeys();

                if (opts.InputFiles != null)
                {
                    var inputFiles = Utils.Expand(opts.InputFiles);

                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        var fileInfo = inputFiles[i];

                        Console.WriteLine(fileInfo.FullName);

                        using (RageArchiveWrapper7 inputArchive = RageArchiveWrapper7.Open(fileInfo.FullName))
                        {
                            var rpfs = new List<Tuple<string, RageArchiveWrapper7>>();

                            if (opts.Recursive)
                            {
                                ArchiveUtilities.ForEachFile(fileInfo.FullName.Replace(Settings.Default.GTAFolder, ""), inputArchive.Root, inputArchive.archive_.Encryption, (string fullFileName, IArchiveFile file, RageArchiveEncryption7 encryption) =>
                                {
                                    if (fullFileName.EndsWith(".rpf", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var binFile = (RageArchiveBinaryFileWrapper7)file;
                                        var tmpStream = new FileStream(Path.GetTempFileName(), FileMode.Open);

                                        binFile.Export(tmpStream);
                                        RageArchiveWrapper7 archive = RageArchiveWrapper7.Open(tmpStream, file.Name);

                                        var wrapper = RageArchiveWrapper7.Open(tmpStream, binFile.Name);

                                        rpfs.Add(new Tuple<string, RageArchiveWrapper7>(fullFileName, wrapper));
                                    }
                                });

                                rpfs.Sort((a, b) =>
                                {
                                    return b.Item1.Replace('\\', '/').Split('/').Length - a.Item1.Replace('\\', '/').Split('/').Length;
                                });
                            }

                            bool found = false;

                            if (opts.Recursive)
                            {
                                for (int j = 0; j < rpfs.Count; j++)
                                {
                                    var fullName = rpfs[j].Item1;
                                    var wrapper = rpfs[j].Item2;

                                    if (wrapper.archive_.Encryption != RageArchiveEncryption7.None)
                                    {
                                        Console.WriteLine("SKIP " + fullName);
                                        continue;
                                    }

                                    found = true;

                                    wrapper.archive_.Encryption = RageArchiveEncryption7.NG;
                                    wrapper.Flush();
                                    wrapper.Dispose();

                                    Console.WriteLine("ENCRYPT " + fullName);
                                }
                            }

                            if (inputArchive.archive_.Encryption != RageArchiveEncryption7.None && !found)
                            {
                                Console.WriteLine("SKIP " + fileInfo.Name);
                                continue;
                            }

                            inputArchive.archive_.Encryption = RageArchiveEncryption7.NG;
                            inputArchive.Flush();
                            inputArchive.Dispose();

                            Console.WriteLine("ENCRYPT " + fileInfo.Name);

                            rpfs.Reverse();

                            for(int j=0; j<rpfs.Count; j++)
                            {
                                rpfs[j].Item2.Dispose();
                            }

                        }
                    }
                }
            });
        }
    }
}
