using System;
using System.Xml;
using RageLib.GTA5.ResourceWrappers.PC.Meta;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.Meta;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleImportMetaOptions(string[] args)
        {
            CommandLine.Parse<ImportMetaOptions>(args, (opts, gOpts) =>
            {
                Init(args);

                if (opts.InputFiles != null)
                {
                    var inputFiles = Utils.Expand(opts.InputFiles);

                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        var fileInfo = inputFiles[i];

                        Console.WriteLine(fileInfo.FullName);

                        var doc = new XmlDocument();

                        doc.Load(fileInfo.FullName);

                        var res = new ResourceFile_GTA5_pc<MetaFile>();
                        res.Version = 2;
                        res.ResourceData = XmlMeta.GetMeta(doc); ;

                        string fileName = fileInfo.FullName.Replace(".xml", "");

                        res.Save(fileName);
                    }
                }
            });
        }
    }
}
