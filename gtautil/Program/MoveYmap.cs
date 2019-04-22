
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RageLib.Archives;
using RageLib.GTA5.ResourceWrappers.PC.Meta.Structures;
using RageLib.Resources.GTA5.PC.Drawables;
using RageLib.Resources.GTA5.PC.GameFiles;
using SharpDX;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleMoveYmapOptionsOptions(string[] args)
        {
            CommandLine.Parse<MoveYmapOptions>(args, (opts, gOpts) =>
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

                if (opts.Position == null)
                {
                    Console.WriteLine("Please provide position with --position");
                    return;
                }

                if (opts.Rotation == null)
                {
                    Console.WriteLine("Please provide rotation with --rotation");
                    return;
                }

                // Init(args);

                var position = new Vector3(opts.Position.ElementAt(0), opts.Position.ElementAt(1), opts.Position.ElementAt(2));
                var rotation = new Quaternion(opts.Rotation.ElementAt(0), opts.Rotation.ElementAt(1), opts.Rotation.ElementAt(2), opts.Rotation.ElementAt(3));

                var ymap = new YmapFile();

                ymap.Load(opts.Ymap);

                if(ymap.CMapData.Entities == null)
                {
                    ymap.CMapData.Entities = new List<MCEntityDef>();
                }
                else
                {
                    for (int i = 0; i < ymap.CMapData.Entities.Count; i++)
                    {
                        var entity = ymap.CMapData.Entities[i];
                        var placement = Utils.World2Mlo(entity.Position, entity.Rotation, Vector3.Zero, Quaternion.Identity);
                        var entityRotation = new Quaternion(placement.Item2.X, placement.Item2.Y, placement.Item2.Z, placement.Item2.W);

                        entity.Position = placement.Item1;
                        entity.Rotation = placement.Item2;

                        entity.Position += position;
                        entity.Position = Utils.RotateTransform(rotation, entity.Position, Vector3.Zero);

                        var newPlacement = Utils.Mlo2World(entity.Position, entity.Rotation, Vector3.Zero, Quaternion.Identity);

                        entity.Position = newPlacement.Item1;
                        entity.Rotation = newPlacement.Item2;
                    }
                }

                if (ymap.CMapData.MloInstances == null)
                {
                    ymap.CMapData.MloInstances = new List<MCMloInstanceDef>();
                }
                else
                {
                    for (int i = 0; i < ymap.CMapData.MloInstances.Count; i++)
                    {
                        var mlo = ymap.CMapData.MloInstances[i];
                        mlo.Position += position;
                    }
                }

                ymap.CMapData.Block = new MCBlockDesc();

                var extents = Utils.CalcExtents(ymap.CMapData.Entities);;

                ymap.CMapData.EntitiesExtentsMin = extents[0][0];
                ymap.CMapData.EntitiesExtentsMax = extents[0][1];
                ymap.CMapData.StreamingExtentsMin = extents[1][0];
                ymap.CMapData.StreamingExtentsMax = extents[1][1];

                ymap.Save(opts.OutputDirectory + "\\" + opts.Name + ".ymap");
            });
        }
    }
}
