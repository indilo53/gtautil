using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using RageLib.GTA5.ResourceWrappers.PC.Meta.Structures;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.Resources.GTA5.PC.Meta;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RageLib.Hash;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleExtractEntitiesOptions(string[] args)
        {
            CommandLine.Parse<ExtractEntitiesOptions>(args, (opts, gOpts) =>
            {
                if (opts.Ytyp == null)
                {
                    Console.WriteLine("Please provide source ytyp file with --ytyp");
                    return;
                }

                if (opts.Position == null || opts.Position.Count() != 3)
                {
                    Console.WriteLine("Please provide a correct position ex: --position 120.5,1370.312,769.2");
                    return;
                }

                if (opts.Rotation == null || opts.Rotation.Count() != 4)
                {
                    Console.WriteLine("Plase provide a correct rotation ex: --rotation 0,0,0,1");
                    return;
                }

                if (opts.Name == null)
                {
                    Console.WriteLine("Plase provide new generated ymap name with --name");
                    return;
                }

                Init(args);

                var position = new Vector3(opts.Position.ElementAt(0), opts.Position.ElementAt(1), opts.Position.ElementAt(2));
                var rotation = new Quaternion(opts.Rotation.ElementAt(0), opts.Rotation.ElementAt(1), opts.Rotation.ElementAt(2), opts.Rotation.ElementAt(3));

                var ytyp = new YtypFile();

                ytyp.Load(opts.Ytyp);

                if(!File.Exists(opts.Name + ".original.ytyp"))
                    File.Copy(opts.Ytyp, opts.Name + ".original.ytyp");

                MCMloArchetypeDef mlo = null;

                for (int i = 0; i < ytyp.CMapTypes.MloArchetypes.Count; i++)
                {
                   if(opts.MloName == null)
                   {
                        mlo = ytyp.CMapTypes.MloArchetypes[i];
                        break;
                   }
                   else
                   {
                        uint mloNameHash = Jenkins.Hash(opts.MloName.ToLowerInvariant());

                        if (mloNameHash == ytyp.CMapTypes.MloArchetypes[i].Name)
                        {
                            Console.Error.WriteLine("Found MLO => " + opts.MloName);
                            mlo = ytyp.CMapTypes.MloArchetypes[i];
                            break;
                        }
                   }
                }

                if (mlo == null)
                {
                    Console.WriteLine("MLO archetype not found");
                    return;
                }

                for(int roomId = 0; roomId < mlo.Rooms.Count; roomId++)
                {
                    var room = mlo.Rooms[roomId];
                    var ymap = new YmapFile();
                    var ymapEntities = new List<MCEntityDef>();

                    Console.WriteLine("Room => " + room.Name + " (" + room.AttachedObjects.Count + " entities)");

                    for (int i = 0; i < room.AttachedObjects.Count; i++)
                    {
                        int idx = (int) room.AttachedObjects[i];

                        if (idx >= mlo.Entities.Count)
                            continue;

                        var entity = mlo.Entities[idx];
                        var entityRotation = new Quaternion(entity.Rotation.X, entity.Rotation.Y, entity.Rotation.Z, entity.Rotation.W);

                        Utils.Mlo2World(entity, mlo, position, rotation);

                        entity.LodLevel = Unk_1264241711.LODTYPES_DEPTH_HD;

                        if (entity.Guid == 0)
                        {
                            var random = new Random();

                            do
                            {
                                entity.Guid = (uint)random.Next(1000000, Int32.MaxValue);
                            }
                            while (mlo.Entities.Count(e => e.Guid == entity.Guid) == 1);

                            Console.WriteLine("[" + i + "] Setting random GUID => " + entity.Guid);
                        }

                        ymapEntities.Add(entity);
                    }

                    ymap.CMapData.Entities = ymapEntities;

                    var extents = Utils.CalcExtents(ymap.CMapData.Entities);

                    ymap.CMapData.EntitiesExtentsMin  = extents[0][0];
                    ymap.CMapData.EntitiesExtentsMax  = extents[0][1];
                    ymap.CMapData.StreamingExtentsMin = extents[1][0];
                    ymap.CMapData.StreamingExtentsMax = extents[1][1];

                    Console.WriteLine(extents[0][0].X + " " + extents[0][0].Y + " " + extents[0][0].Z);
                    Console.WriteLine(extents[0][1].X + " " + extents[0][1].Y + " " + extents[0][1].Z);

                    Directory.CreateDirectory(opts.Name);

                    ymap.Save(opts.Name + "\\" + room.Name + ".ymap");
                }

                if(mlo.EntitySets != null)
                {
                    for (int i = 0; i < mlo.EntitySets.Count; i++)
                    {
                        var entitySet = mlo.EntitySets[i];

                        Directory.CreateDirectory(opts.Name + "\\entitysets\\" + entitySet.Name);

                        for (int roomId = 0; roomId < mlo.Rooms.Count; roomId++)
                        {
                            var room = mlo.Rooms[roomId];
                            var ymap = new YmapFile();
                            var ymapEntities = new List<MCEntityDef>();

                            Console.WriteLine("EntitySet => " + entitySet.Name + " [" + room.Name + "] (" + entitySet.Entities.Count + " entities)");

                            for (int j = 0; j < entitySet.Entities.Count; j++)
                            {
                                int targetRoom = (int) entitySet.Locations[j];

                                if (targetRoom != roomId)
                                    continue;

                                var entity = entitySet.Entities[j];
                                var entityRotation = new Quaternion(entity.Rotation.X, entity.Rotation.Y, entity.Rotation.Z, entity.Rotation.W);

                                Utils.Mlo2World(entity, mlo, position, rotation);

                                entity.LodLevel = Unk_1264241711.LODTYPES_DEPTH_HD;

                                if (entity.Guid == 0)
                                {
                                    var random = new Random();

                                    do
                                    {
                                        entity.Guid = (uint)random.Next(1000000, Int32.MaxValue);
                                    }
                                    while (mlo.Entities.Count(e => e.Guid == entity.Guid) == 1);

                                    Console.WriteLine("[" + i + "] Setting random GUID => " + entity.Guid);
                                }

                                ymapEntities.Add(entity);
                            }

                            ymap.CMapData.Entities = ymapEntities;

                            var extents = Utils.CalcExtents(ymap.CMapData.Entities);

                            ymap.CMapData.EntitiesExtentsMin = extents[0][0];
                            ymap.CMapData.EntitiesExtentsMax = extents[0][1];
                            ymap.CMapData.StreamingExtentsMin = extents[1][0];
                            ymap.CMapData.StreamingExtentsMax = extents[1][1];

                            Console.WriteLine(extents[0][0].X + " " + extents[0][0].Y + " " + extents[0][0].Z);
                            Console.WriteLine(extents[0][1].X + " " + extents[0][1].Y + " " + extents[0][1].Z);

                            ymap.Save(opts.Name + "\\entitysets\\" + entitySet.Name + "\\entityset_" + entitySet.Name + "_" + room.Name + ".ymap");
                        }
                    }
                }

                /*
                for(int portalId=0; portalId < mlo.Portals.Count; portalId++)
                {
                    var portal = mlo.Portals[portalId];
                    var ymap = new YmapFile();
                    var ymapEntities = new List<MCEntityDef>();
                    var entitiesExtents = new List<Tuple<Vector3, Vector3>>();
                    var streamingExtents = new List<Tuple<Vector3, Vector3>>();

                    Console.WriteLine("Portal => " + portalId + " (" + portal.AttachedObjects.Count + " entities)");

                    for (int i = 0; i < portal.AttachedObjects.Count; i++)
                    {
                        int idx = (int)portal.AttachedObjects[i];

                        if (idx >= mlo.Entities.Count)
                            continue;

                        var entity = mlo.Entities[idx];
                        var entityRotation = new Quaternion(entity.Rotation.X, entity.Rotation.Y, entity.Rotation.Z, entity.Rotation.W);

                        Utils.Mlo2World(entity, mlo, position, rotation);

                        entity.LodLevel = Unk_1264241711.LODTYPES_DEPTH_HD;

                        if (entity.Guid == 0)
                        {
                            var random = new Random();

                            do
                            {
                                entity.Guid = (uint)random.Next(1000000, Int32.MaxValue);
                            }
                            while (mlo.Entities.Count(e => e.Guid == entity.Guid) == 1);

                            Console.WriteLine("[" + i + "] Setting random GUID => " + entity.Guid);
                        }

                        ymapEntities.Add(entity);
                    }

                    ymap.CMapData.Entities = ymapEntities;

                    var extents = Utils.CalcExtents(ymap.CMapData.Entities);

                    ymap.CMapData.EntitiesExtentsMin = extents[0][0];
                    ymap.CMapData.EntitiesExtentsMax = extents[0][1];
                    ymap.CMapData.StreamingExtentsMin = extents[1][0];
                    ymap.CMapData.StreamingExtentsMax = extents[1][1];

                    Console.WriteLine(extents[0][0].X + " " + extents[0][0].Y + " " + extents[0][0].Z);
                    Console.WriteLine(extents[0][1].X + " " + extents[0][1].Y + " " + extents[0][1].Z);

                    Directory.CreateDirectory(opts.Name);

                    ymap.Save(opts.Name + "\\portal_" + portalId.ToString().PadLeft(3, '0') + ".ymap");

                    var data = new JObject()
                    {
                        ["corners"] = new JArray()
                        {
                            new JObject() { ["x"] = portal.Corners[0][0], ["y"] = portal.Corners[0][1], ["z"] = portal.Corners[0][2] },
                            new JObject() { ["x"] = portal.Corners[1][0], ["y"] = portal.Corners[1][1], ["z"] = portal.Corners[1][2] },
                            new JObject() { ["x"] = portal.Corners[2][0], ["y"] = portal.Corners[2][1], ["z"] = portal.Corners[2][2] },
                            new JObject() { ["x"] = portal.Corners[3][0], ["y"] = portal.Corners[3][1], ["z"] = portal.Corners[3][2] },
                        },
                        ["flags"] = portal.Flags,
                        ["mirrorPriority"] = portal.MirrorPriority,
                        ["opacity"] = portal.Opacity,
                        ["roomFrom"] = portal.RoomFrom,
                        ["roomTo"] = portal.RoomTo,
                    };

                    var jsonString = JsonConvert.SerializeObject(data, new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.Indented });

                    File.WriteAllText(opts.Name + "\\portal_" + portalId.ToString().PadLeft(3, '0') + ".json", jsonString);
                }
                */
            });
        }
    }
}
