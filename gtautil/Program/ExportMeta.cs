using System;
using System.IO;
using RageLib.GTA5.ResourceWrappers.PC.Meta;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.Meta;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleExportMetaOptions(string[] args)
        {
            CommandLine.Parse<ExportMetaOptions>(args, (opts, gOpts) =>
            {
                EnsurePath();
                EnsureKeys();
                EnsureCache();

                if (opts.InputFiles == null)
                {
                    Console.WriteLine("Please provide input files with -i --input");
                    return;
                }
                else
                {
                    var inputFiles = Utils.Expand(opts.InputFiles);

                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        var fileInfo = inputFiles[i];

                        Console.WriteLine(fileInfo.FullName);

                        var res = new ResourceFile_GTA5_pc<MetaFile>();
                        res.Load(fileInfo.FullName);

                        var xml = MetaXml.GetXml(res.ResourceData);

                        string fileName = fileInfo.FullName + ".xml";

                        File.WriteAllText(fileName, xml);
                    }
                }
            });
        }
    }
}
