
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.Utilities;
using RageLib.Resources.GTA5.PC.GameFiles;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleCompileGxt2Options(string[] args)
        {
            CommandLine.Parse<CompileGxt2Optionns>(args, (opts, gOpts) =>
            {
                Init(args);

                if (opts.OutputDirectory == null)
                {
                    Console.WriteLine("Please provide output directory with -o --output");
                    return;
                }

                var entries = new Dictionary<uint, string>();

                ArchiveUtilities.ForEachBinaryFile(Settings.Default.GTAFolder, (string fullFileName, IArchiveBinaryFile file, RageArchiveEncryption7 encryption) =>
                {
                    if(fullFileName.EndsWith(".gxt2") && fullFileName.ToLowerInvariant().Contains(opts.Lang.ToLowerInvariant()))
                    {
                        Console.WriteLine(fullFileName);

                        var gxt2 = new Gxt2File();
                        byte[] data = Utils.GetBinaryFileData(file, encryption);

                        gxt2.Load(data);

                        for(int i=0; i<gxt2.TextEntries.Count; i++)
                        {
                            entries[gxt2.TextEntries[i].Hash] = gxt2.TextEntries[i].Text;
                        }
                    }
                });

                var sb = new StringBuilder();

                foreach(var entry in entries)
                {
                    sb.AppendLine(entry.Key + " = " + entry.Value);
                }

                File.WriteAllText(opts.OutputDirectory + "\\" + opts.Lang + ".gxt2.txt", sb.ToString());
            });
        }
    }
}
