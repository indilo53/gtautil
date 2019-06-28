
using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using RageLib.GTA5.Utilities;
using RageLib.Resources.GTA5.PC.GameFiles;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleFindPropsOptions(string[] args)
        {
            CommandLine.Parse<FindPropsOptions>(args, (opts, gOpts) =>
            {
                if(opts.Position == null || opts.Position.Count != 3)
                {
                    Console.Error.WriteLine("Please provide position with --position x,y,z");
                    return;
                }

                var position   = new Vector3(opts.Position[0], opts.Position[1], opts.Position[2]);
                var inputFiles = Utils.Expand(opts.InputFiles);

                for (int i = 0; i < inputFiles.Length; i++)
                {
                    var fileInfo = inputFiles[i];

                    if (fileInfo.Name.EndsWith(".ymap"))
                    {
                        var ymap = new YmapFile();

                        ymap.Load(fileInfo.FullName);

                        for (int j = 0; j < ymap.CMapData.Entities.Count; j++)
                        {
                            var entity = ymap.CMapData.Entities[j];
                            float distance = Vector3.Distance(position, entity.Position);

                            if (distance <= opts.Radius)
                            {
                                Console.WriteLine(fileInfo.Name + " => " + entity.Guid);
                            }
                        }
                    }
                }
            });
        }
    }
}
