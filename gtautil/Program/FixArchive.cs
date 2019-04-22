using System;
using System.Xml;
using RageLib.GTA5.ArchiveWrappers;
using RageLib.GTA5.ResourceWrappers.PC.Meta;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.Meta;

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

                        using (RageArchiveWrapper7 rageArchiveWrapper = RageArchiveWrapper7.Open(fileInfo.FullName))
                        {

                            if (rageArchiveWrapper.archive_.Encryption != RageLib.GTA5.Archives.RageArchiveEncryption7.None)
                            {
                                Console.WriteLine("File is already encrypted, nothing to do");
                                return;
                            }

                            rageArchiveWrapper.archive_.Encryption = RageLib.GTA5.Archives.RageArchiveEncryption7.NG;
                            rageArchiveWrapper.Flush();
                            rageArchiveWrapper.Dispose();

                            Console.WriteLine("File encrypted");

                        }
                    }
                }
            });
        }
    }
}
