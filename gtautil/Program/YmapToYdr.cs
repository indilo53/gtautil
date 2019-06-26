
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.Utilities;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.Resources.GTA5.PC.Drawables;
using RageLib.Resources.Common;
using SharpDX;
using RageLib.Resources;

namespace GTAUtil
{
    partial class Program
    {
        // WIP++
        static void HandlYmapToYdrOptions(string[] args)
        {
            CommandLine.Parse<YmapToYdrOptions>(args, (opts, gOpts) =>
            {
                Init(args);

                var files = new Dictionary<uint, string>();
                var required = new List<uint>();

                var ymap = new YmapFile();

                ymap.Load(opts.InputFile);

                for(int i=0; i<ymap.CMapData.Entities.Count; i++)
                {
                    var entity = ymap.CMapData.Entities[i];

                    if (required.IndexOf(entity.ArchetypeName) == -1)
                        required.Add(entity.ArchetypeName);
                }

                ArchiveUtilities.ForEachResourceFile(Settings.Default.GTAFolder, (fullFileName, file, encryption) =>
                {
                    if(file.Name.EndsWith(".ydr"))
                    {
                        uint hash = Utils.Hash(file.Name.ToLowerInvariant().Replace(".ydr", ""));

                        if (required.IndexOf(hash) != -1 && !files.ContainsKey(hash))
                        {
                            Console.WriteLine(file.Name);

                            string tmp = Path.GetTempFileName();
                            file.Export(tmp);
                            files.Add(hash, tmp);
                        }
                    }
                });

                YdrFile ydr = null;

                var bbMin = new Vector3(float.MaxValue);
                var bbMax = new Vector3(float.MinValue);
                var bsCenter = Vector3.Zero;

                foreach (var file in files)
                {
                    var ydr2 = new YdrFile();

                    ydr2.Load(file.Value);

                    bbMin = Vector3.Min(bbMin, (Vector3)(Vector4)ydr2.Drawable.BoundingBoxMin);
                    bbMax = Vector3.Max(bbMin, (Vector3)(Vector4)ydr2.Drawable.BoundingBoxMax);

                    if (ydr == null)
                    {
                        ydr = ydr2;
                        continue;
                    }

                    ydr.Drawable.BoundingSphereRadius = ydr2.Drawable.BoundingSphereRadius;

                    for (int i=0; i<ydr2.Drawable.DrawableModelsHigh.Entries.Count; i++)
                    {
                        var model = ydr2.Drawable.DrawableModelsHigh.Entries[i];

                        for(int j=0; j<model.Geometries.Count; j++)
                        {
                            ydr.Drawable.DrawableModelsHigh.Entries[i].Geometries.Add(model.Geometries[j]);
                        }
                    }

                    for (int i = 0; i < ydr2.Drawable.DrawableModelsX.Entries.Count; i++)
                    {
                        var model = ydr2.Drawable.DrawableModelsX.Entries[i];

                        for (int j = 0; j < model.Geometries.Count; j++)
                        {
                            ydr.Drawable.DrawableModelsX.Entries[i].Geometries.Add(model.Geometries[j]);
                        }
                    }
                }

                ydr.Drawable.BoundingBoxMin = (RAGE_Vector4) (Vector4) bbMin;
                ydr.Drawable.BoundingBoxMax = (RAGE_Vector4) (Vector4) bbMax;
                ydr.Drawable.BoundingCenter = (RAGE_Vector3) bsCenter;

                ydr.Save(opts.OutputFile);

            });
        }
    }
}
