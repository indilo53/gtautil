using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using RageLib.GTA5.ResourceWrappers.PC.Meta.Structures;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.Resources.GTA5.PC.Meta;
using RageLib.Resources.GTA5.PC.Bounds;
using System.IO;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleInjectEntitiesOptions(string[] args)
        {
            CommandLine.Parse<InjectEntitiesOptions>(args, (opts, gOpts) =>
            {
                if (opts.Ymap == null)
                {
                    Console.WriteLine("Please provide source ymap file with --ymap");
                    return;
                }

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
                    Console.WriteLine("Plase provide new generated ytyp name with --name");
                    return;
                }

                Init(args);

                var ymapInfos = Utils.Expand(opts.Ymap);
                var ymapNames = ymapInfos.Select(e => Path.GetFileNameWithoutExtension(e.Name)).ToArray();

                var position = new Vector3(opts.Position.ElementAt(0), opts.Position.ElementAt(1), opts.Position.ElementAt(2));
                var rotation = new Quaternion(opts.Rotation.ElementAt(0), opts.Rotation.ElementAt(1), opts.Rotation.ElementAt(2), opts.Rotation.ElementAt(3));

                var ytyp = new YtypFile();

                ytyp.Load(opts.Ytyp);

                MCMloArchetypeDef mlo = null;

                for (int i = 0; i < ytyp.CMapTypes.MloArchetypes.Count; i++)
                {
                    mlo = ytyp.CMapTypes.MloArchetypes[i];
                    break;
                }

                if (mlo == null)
                {
                    Console.WriteLine("MLO archetype not found");
                    return;
                }

                var ymaps = new List<YmapFile>();

                for (int i = 0; i < ymapInfos.Length; i++)
                {
                    var ymap = new YmapFile();

                    ymap.Load(ymapInfos[i].FullName);

                    ymaps.Add(ymap);
                }

                var missingYmap = new YmapFile();
                int missingCount = 0;

                Console.WriteLine("Calculating rooms extents");

                var roomExtents = new Vector3[mlo.Rooms.Count][];

                for (int i = 0; i < mlo.Rooms.Count; i++)
                {
                    var room = mlo.Rooms[i];
                    var entities = new List<MCEntityDef>();

                    for(int j=0; j<room.AttachedObjects.Count; j++)
                    {
                        int idx = (int)room.AttachedObjects[j];

                        if (idx >= mlo.Entities.Count)
                            continue;

                        entities.Add(mlo.Entities[idx]);
                    }

                    var extents = Utils.CalcExtents(entities);

                    roomExtents[i] = extents[0];
                }

                for (int i=0; i<ymaps.Count; i++)
                {
                    var ymap = ymaps[i];
                    var name = ymapNames[i];

                    if (name.StartsWith("portal_"))
                        continue;

                    var roomIdx = mlo.Rooms.FindIndex(e => e.Name == name);
                    MCMloRoomDef currRoom = null;

                    if (roomIdx != -1)
                        currRoom = mlo.Rooms[roomIdx];

                    for (int j = 0; j < ymap.CMapData.Entities.Count; j++)
                    {
                        var entity = ymap.CMapData.Entities[j];
                        var idx = mlo.Entities.FindIndex(e => e.Guid == entity.Guid);
                        var room = currRoom;
                        var originalPosition = entity.Position;
                        var originalRotation = entity.Rotation;

                        Console.WriteLine(name + " => " + j + " (" + idx + "|" + mlo.Entities.Count + ") => " + Utils.HashString((MetaName)entity.ArchetypeName));

                        Utils.World2Mlo(entity, mlo, position, rotation);

                        if (opts.Static && idx == -1)
                        {
                            if ((entity.Flags & 32) == 0)
                            {
                                Console.WriteLine("  Setting static flag (32)");
                                entity.Flags = entity.Flags | 32;
                            }
                        }

                        entity.LodLevel = Unk_1264241711.LODTYPES_DEPTH_ORPHANHD;

                        if (entity.Guid == 0)
                        {
                            var random = new Random();

                            do
                            {
                                entity.Guid = (uint)random.Next(1000000, Int32.MaxValue);
                            }
                            while (mlo.Entities.Count(e => e.Guid == entity.Guid) > 0);

                            Console.WriteLine("  Setting random GUID => " + entity.Guid);
                        }

                        if (idx == -1)
                        {
                            idx = mlo.AddEntity(entity);
                        }
                        else
                        {
                            Console.WriteLine("  Found matching GUID => Overriding " + idx);
                            mlo.Entities[idx] = entity;
                        }


                        Console.WriteLine(j + " " + Utils.HashString((MetaName)entity.ArchetypeName));

                        if (room == null)
                            room = GetRoomForEntity(mlo, roomExtents, entity);

                        if(room == null)
                        {
                            entity.Position = originalPosition;
                            entity.Rotation = originalRotation;
                            entity.LodLevel = Unk_1264241711.LODTYPES_DEPTH_HD;

                            missingYmap.CMapData.Entities.Add(entity);

                            missingCount++;

                            continue;
                        }

                        uint id = (uint)idx;

                        if (room.AttachedObjects.IndexOf(id) == -1)
                        {
                            room.AttachedObjects.Add(id);
                        }

                        Console.WriteLine("  Room => " + room.Name);

                    }
                }

                if(opts.DeleteMissing)
                {
                    for (int i = mlo.Entities.Count - 1; i >= 0; i--)
                    {
                        bool found = false;

                        for(int j=0; j<ymaps.Count; j++)
                        {
                            var ymap = ymaps[j];

                            if (ymap.CMapData.Entities.FindIndex(e => e.Guid == mlo.Entities[i].Guid) != -1)
                            {
                                found = true;
                                break;
                            }
                        }

                        if(!found)
                        {
                            Console.WriteLine("DELETE " + i);

                            for (int j = 0; j < mlo.Rooms.Count; j++)
                                for (int k = mlo.Rooms[j].AttachedObjects.Count - 1; k >= 0; k--)
                                    if (mlo.Rooms[j].AttachedObjects[k] == (uint)i)
                                        mlo.Rooms[j].AttachedObjects.RemoveAt(k);
                        }
                    }
                }

                ytyp.Save(opts.Name + ".ytyp");

                if(missingCount > 0)
                {
                    var extents = Utils.CalcExtents(missingYmap.CMapData.Entities);

                    missingYmap.CMapData.EntitiesExtentsMin = extents[0][0];
                    missingYmap.CMapData.EntitiesExtentsMax = extents[0][1];
                    missingYmap.CMapData.StreamingExtentsMin = extents[1][0];
                    missingYmap.CMapData.StreamingExtentsMax = extents[1][1];

                    missingYmap.Save(opts.Name + "_exterior.ymap");
                }
            });
        }

        public static MCMloRoomDef GetRoomForEntity(MCMloArchetypeDef mlo, Vector3[][] roomExtents, MCEntityDef entity)
        {
            for(int i=0; i < roomExtents.Length; i++)
            {
                if (mlo.Rooms[i].Name == "limbo")
                    continue;

                var extents = roomExtents[i];
                var emin = extents[0];
                var emax = extents[1];

                var drawable = Program.GetDrawable(entity.ArchetypeName);

                if (drawable == null)
                {
                    if(
                        entity.Position.X >= emin.X && entity.Position.Y >= emin.Y && entity.Position.Z >= emin.Z &&
                        entity.Position.X <= emax.X && entity.Position.Y <= emax.Y && entity.Position.Z <= emax.Z
                    )
                    {
                        return mlo.Rooms[i];
                    }
                }
                else
                {
                    Quaternion orientation = new Quaternion(entity.Rotation);

                    Vector3 dcenter = (Vector3)drawable.BoundingCenter;
                    Vector3 dbbmin = (Vector3)(Vector4)drawable.BoundingBoxMin - dcenter;
                    Vector3 dbbmax = (Vector3)(Vector4)drawable.BoundingBoxMax - dcenter;
                    Vector3 center = entity.Position + dcenter;

                    Vector3 bbmin = entity.Position + dbbmin;
                    Vector3 bbmax = entity.Position + dbbmax;

                    Vector3 c1 = Utils.RotateTransform(orientation, bbmin, entity.Position);
                    Vector3 c2 = Utils.RotateTransform(orientation, bbmax, entity.Position);

                    bbmin = Vector3.Min(c1, c2);
                    bbmax = Vector3.Max(c1, c2);

                    if (
                        center.X >= emin.X && center.Y >= emin.Y && center.Z >= emin.Z &&
                        center.X <= emax.X && center.Y <= emax.Y && center.Z <= emax.Z
                    )
                    {
                        return mlo.Rooms[i];
                    }
                }
            }

            return null;
        }
    }
}
