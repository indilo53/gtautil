
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RageLib.Archives;
using RageLib.Resources.GTA5.PC.Drawables;
using RageLib.Resources.GTA5.PC.GameFiles;
using SharpDX;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleMergeYmapOptionsOptions(string[] args)
        {
            CommandLine.Parse<MergeYmapOptions>(args, (opts, gOpts) =>
            {
                if (opts.OutputDirectory == null)
                {
                    Console.WriteLine("Plase provide output directory with --output");
                    return;
                }

                if (opts.Name == null)
                {
                    Console.WriteLine("Plase provide name with --name");
                    return;
                }

                if (opts.Ymap == null)
                {
                    Console.WriteLine("Please provide source ymap files with --ymap");
                    return;
                }

                // Init(args);

                var ymapInfos = Utils.Expand(opts.Ymap);
                var bbs = new List<Tuple<Vector3, Vector3>>();
                var ymap = new YmapFile();

                for(int i=0; i< ymapInfos.Length; i++)
                {
                    var _ymap = new YmapFile();

                    _ymap.Load(ymapInfos[i].FullName);

                    ymap.CMapData.AddEntities(_ymap.CMapData.Entities);
                }

                for(int i=0; i<ymap.CMapData.Entities.Count; i++)
                {
                    var entity = ymap.CMapData.Entities[i];

                    var random = new Random();

                    do
                    {
                        entity.Guid = (uint)random.Next(1000000, Int32.MaxValue);
                    }
                    while (ymap.CMapData.Entities.Count(e => e.Guid == entity.Guid) > 1);

                    Console.WriteLine("[" + i + "] Setting random GUID => " + entity.Guid);
                }

                var extents = Utils.CalcExtents(ymap.CMapData.Entities);

                ymap.CMapData.EntitiesExtentsMin = extents[0][0];
                ymap.CMapData.EntitiesExtentsMax = extents[0][1];
                ymap.CMapData.StreamingExtentsMin = extents[1][0];
                ymap.CMapData.StreamingExtentsMax = extents[1][1];

                Directory.CreateDirectory(opts.OutputDirectory);

                ymap.Save(opts.OutputDirectory + "\\" + opts.Name + ".ymap");
            });
        }
    }
}
