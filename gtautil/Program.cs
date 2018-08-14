using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using SharpDX;
using System.Reflection;
using Newtonsoft.Json.Linq;
using RageLib.Hash;
using RageLib.GTA5.Cryptography;
using RageLib.GTA5.Cryptography.Helpers;
using RageLib.Resources.GTA5.PC.Meta;
using RageLib.GTA5.ResourceWrappers.PC.Meta;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.GTA5.Utilities;
using Newtonsoft.Json;
using System.Globalization;

namespace GTAUtil
{
    class Program
    {
        static dynamic Cache = null;

        static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        static void Main(string[] args)
        {
            EnsurePath();
            EnsureKeys();
            EnsureCache();

            CommandLine.Parse<GenPropDefsOptions>(args, opts =>
            {
                if (opts.InputFiles != null)
                {
                    var inputFiles = Utils.Expand(opts.InputFiles);
                    var ytyp = new YtypFile();

                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        var fileInfo = inputFiles[i];

                        string name = "";
                        var split = fileInfo.Name.Split('.');

                        for (int j = 0; j < split.Length; j++)
                        {
                            if (j < split.Length - 1)
                            {
                                if (j > 0)
                                    name += ".";

                                name += split[j];
                            }
                        }

                        Console.WriteLine(name);

                        try
                        {
                            switch (fileInfo.Extension)
                            {
                                case ".ydr":
                                    {
                                        var ydr = new YdrFile();
                                        var arch = new RageLib.GTA5.ResourceWrappers.PC.Meta.Structures.CBaseArchetypeDef();
                                        var nameHash = (MetaName)Jenkins.Hash(name);

                                        ydr.Load(fileInfo.FullName);

                                        arch.Name = nameHash;
                                        arch.AssetName = nameHash;
                                        arch.TextureDictionary = nameHash;
                                        arch.PhysicsDictionary = (MetaName)Jenkins.Hash("prop_" + name);
                                        arch.Flags = 32;
                                        arch.AssetType = Unk_1991964615.ASSET_TYPE_DRAWABLE;
                                        arch.BbMin = (Vector3)(Vector4)ydr.Drawable.BoundingBoxMin;
                                        arch.BbMax = (Vector3)(Vector4)ydr.Drawable.BoundingBoxMax;
                                        arch.BsCentre = (Vector3) ydr.Drawable.BoundingCenter;
                                        arch.BsRadius = ydr.Drawable.BoundingSphereRadius;
                                        arch.LodDist = 500f;
                                        arch.HdTextureDist = 5;

                                        ytyp.CMapTypes.Archetypes.Add(arch);

                                        break;
                                    }

                                case ".ydd": // TODO
                                    {
                                        break;
                                    }

                                default: break;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine("ERROR => " + e.Message);
                        }

                    }

                    string path = (opts.Directory == null) ? @".\props.ytyp" : opts.Directory + @"\props.ytyp";

                    ytyp.Save(path);
                }
            });

            CommandLine.Parse<ImportMetaOptions>(args, opts =>
            {
                if (opts.InputFiles != null && opts.Directory != null)
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

            CommandLine.Parse<ExportMetaOptions>(args, opts =>
            {
                if (opts.InputFiles != null && opts.Directory != null)
                {
                    var inputFiles = Utils.Expand(opts.InputFiles);

                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        var fileInfo = inputFiles[i];

                        Console.WriteLine(fileInfo.FullName);

                        var res = new ResourceFile_GTA5_pc<MetaFile>();
                        res.Load(fileInfo.FullName);

                        var xml = MetaXml.GetXml(res.ResourceData);

                        File.WriteAllText(opts.Directory + "\\" + fileInfo.Name + ".xml", xml);
                    }
                }
            });

            CommandLine.Parse<InjectEntitiesOptions>(args, opts =>
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

                if (opts.Room == null)
                {
                    Console.WriteLine("Please provide mlo room name with --room");
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

                var position = new Vector3(opts.Position.ElementAt(0), opts.Position.ElementAt(1), opts.Position.ElementAt(2));
                var rotation = new Quaternion(opts.Rotation.ElementAt(0), opts.Rotation.ElementAt(1), opts.Rotation.ElementAt(2), opts.Rotation.ElementAt(3));

                var ymap = new YmapFile();
                var ytyp = new YtypFile();

                ymap.Load(opts.Ymap);
                ytyp.Load(opts.Ytyp);

                RageLib.GTA5.ResourceWrappers.PC.Meta.Structures.CMloArchetypeDef mlo = null;

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

                RageLib.GTA5.ResourceWrappers.PC.Meta.Structures.CMloRoomDef room = null;

                for (int i = 0; i < mlo.Rooms.Count; i++)
                {
                    if (mlo.Rooms[i].Name == opts.Room)
                    {
                        room = mlo.Rooms[i];
                        break;
                    }
                }

                if (room == null)
                {
                    Console.WriteLine("MLO room not found");
                    return;
                }

                var mloEntities = new List<RageLib.GTA5.ResourceWrappers.PC.Meta.Structures.CEntityDef>();
                var attachedObjects = new List<uint>();

                mloEntities.AddRange(mlo.Entities);
                attachedObjects.AddRange(room.AttachedObjects);

                for (int i = 0; i < ymap.CMapData.Entities.Count; i++)
                {
                    var entity = ymap.CMapData.Entities[i];
                    var mloRot = rotation;
                    var objRot = new Quaternion(entity.Rotation.X, entity.Rotation.Y, entity.Rotation.Z, entity.Rotation.W);
                    var rotationDiff = objRot * mloRot;    // Multiply initial entity rotation by mlo rotation 

                    entity.Position -= position;    // Substract mlo world coords from entity world coords
                    entity.Position = Utils.RotateTransform(Quaternion.Conjugate(mloRot), entity.Position, Vector3.Zero);   // Rotate entity around center of mlo instance (mlo entities rotations in space are inverted)
                    entity.Rotation = new Vector4(rotationDiff.X, rotationDiff.Y, rotationDiff.Z, rotationDiff.W);

                    mloEntities.Add(entity);
                    attachedObjects.Add((uint)mloEntities.IndexOf(entity));
                }

                mlo.Entities = mloEntities;
                room.AttachedObjects = attachedObjects;

                ytyp.Save(opts.Name.EndsWith(".ytyp") ? opts.Name : opts.Name + ".ytyp");
            });

            CommandLine.Parse<FindOptions>(args, opts =>
            {
                if(opts.Position == null || opts.Position.Count != 3)
                {
                    Console.Error.WriteLine("Please specify position with -p --position");
                    return;
                }

                if(Cache == null)
                {
                    Console.Error.WriteLine("Please build cache first with buildcache");
                    return;
                }

                var c = CultureInfo.InvariantCulture;
                            
                for(int i=0; i<Cache["ymap"].Count; i++)
                {
                    var cYmap = Cache["ymap"][i];

                    var entitiesExtentsMin = new Vector3((float)cYmap["entitiesExtentsMin"]["x"], (float)cYmap["entitiesExtentsMin"]["y"], (float)cYmap["entitiesExtentsMin"]["z"]);
                    var entitiesExtentsMax = new Vector3((float)cYmap["entitiesExtentsMax"]["x"], (float)cYmap["entitiesExtentsMax"]["y"], (float)cYmap["entitiesExtentsMax"]["z"]);

                    if (
                        opts.Position[0] >= entitiesExtentsMin.X && opts.Position[0] <= entitiesExtentsMax.X &&
                        opts.Position[1] >= entitiesExtentsMin.Y && opts.Position[1] <= entitiesExtentsMax.Y &&
                        opts.Position[2] >= entitiesExtentsMin.Z && opts.Position[2] <= entitiesExtentsMax.Z
                    )
                    {
                        Console.WriteLine("ymap: " + ((string) cYmap["path"]).Split('\\').Last());

                        for(int j=0; j< cYmap["mloInstances"].Count; j++)
                        {
                            var cMloInstance = cYmap["mloInstances"][j];
                            var cMloInstanceHash = (uint)cMloInstance["name"];

                            var instancePos = new Vector3((float)cMloInstance["position"]["x"], (float)cMloInstance["position"]["y"], (float)cMloInstance["position"]["z"]);
                            var instanceRot = new Quaternion((float)cMloInstance["rotation"]["x"], (float)cMloInstance["rotation"]["y"], (float)cMloInstance["rotation"]["z"], (float)cMloInstance["rotation"]["w"]);

                            for (int k=0; k<Cache["ytyp"].Count; k++)
                            {
                                var cYtyp     = Cache["ytyp"][k];
                                var cYtypHash = (uint)cYtyp["hash"];

                                for(int l=0; l<cYtyp["mloArchetypes"].Count; l++)
                                {
                                    var cMloArch = cYtyp["mloArchetypes"][l];
                                    var cMloArchHash = (uint)cMloArch["name"];

                                    if(cMloInstanceHash == cMloArchHash)
                                    {
                                        Console.WriteLine("  ytyp => " + ((string)cYtyp["path"]).Split('\\').Last());
                                        Console.WriteLine("    mlo => " + Jenkins.GetString(cMloArchHash));
                                        Console.WriteLine("    position => " + instancePos.X.ToString(c) + "," + instancePos.Y.ToString(c) + "," + instancePos.Z.ToString(c));
                                        Console.WriteLine("    rotation => " + instanceRot.X.ToString(c) + "," + instanceRot.Y.ToString(c) + "," + instanceRot.Z.ToString(c) + "," + instanceRot.W.ToString(c));

                                        for (int m = 0; m < cMloArch["rooms"].Count; m++)
                                        {
                                            var cMloRoom = cMloArch["rooms"][m];

                                            var roomBbMin = new Vector3((float)cMloRoom["bbMin"]["x"], (float)cMloRoom["bbMin"]["y"], (float)cMloRoom["bbMin"]["z"]);
                                            var roomBbMax = new Vector3((float)cMloRoom["bbMax"]["x"], (float)cMloRoom["bbMax"]["y"], (float)cMloRoom["bbMax"]["z"]);

                                            var roomBbMinWorld = instancePos + roomBbMin;
                                            var roomBbMaxWorld = instancePos + roomBbMax;

                                            roomBbMinWorld = Utils.RotateTransform(Quaternion.Conjugate(instanceRot), roomBbMinWorld, Vector3.Zero);
                                            roomBbMaxWorld = Utils.RotateTransform(Quaternion.Conjugate(instanceRot), roomBbMaxWorld, Vector3.Zero);

                                            if (
                                                opts.Position[0] >= roomBbMinWorld.X && opts.Position[0] <= roomBbMaxWorld.X &&
                                                opts.Position[1] >= roomBbMinWorld.Y && opts.Position[1] <= roomBbMaxWorld.Y &&
                                                opts.Position[2] >= roomBbMinWorld.Z && opts.Position[2] <= roomBbMaxWorld.Z
                                            )
                                            {
                                                Console.WriteLine("      room => " + cMloRoom["name"]);
                                            }
                                        }

                                    }

                                }
                            }
                        }

                        Console.WriteLine("");
                    }
                }

            });

            CommandLine.Parse<BuildCacheOptions>(args, opts =>
            {

                dynamic cache = new JObject();

                cache["ymap"] = new JArray();
                cache["ytyp"] = new JArray();

                ArchiveUtilities.ForEachFile(Settings.Default.GTAFolder, (fullFileName, file, encryption) =>
                {
                    Console.WriteLine(fullFileName);

                    string fileNameWithoutExtension = file.Name.Split('.').First();

                    Jenkins.Ensure(fileNameWithoutExtension);

                    if (file.Name.EndsWith(".ymap"))
                    {
                        var ymap = new YmapFile();

                        using (MemoryStream ms = new MemoryStream())
                        {
                            file.Export(ms);
                            ymap.Load(ms);
                        }

                        dynamic entry = new JObject()
                        {
                            ["name"] = fileNameWithoutExtension,
                            ["path"] = fullFileName,
                            ["hash"] = Jenkins.Hash(fileNameWithoutExtension),
                            ["entitiesExtentsMin"] = new JObject()
                            {
                                ["x"] = ymap.CMapData.EntitiesExtentsMin.X,
                                ["y"] = ymap.CMapData.EntitiesExtentsMin.Y,
                                ["z"] = ymap.CMapData.EntitiesExtentsMin.Z,
                            },
                            ["entitiesExtentsMax"] = new JObject()
                            {
                                ["x"] = ymap.CMapData.EntitiesExtentsMax.X,
                                ["y"] = ymap.CMapData.EntitiesExtentsMax.Y,
                                ["z"] = ymap.CMapData.EntitiesExtentsMax.Z,
                            },
                            ["mloInstances"] = new JArray(),
                        };

                        if (ymap.CMapData.MloInstances != null)
                        {
                            for (int i = 0; i < ymap.CMapData.MloInstances.Count; i++)
                            {
                                var mloInstance = ymap.CMapData.MloInstances[i];

                                var mloInstanceEntry = new JObject()
                                {
                                    ["name"] = ymap.CMapData.MloInstances[i].ArchetypeName,
                                    ["position"] = new JObject()
                                    {
                                        ["x"] = mloInstance.Position.X,
                                        ["y"] = mloInstance.Position.Y,
                                        ["z"] = mloInstance.Position.Z,
                                    },
                                    ["rotation"] = new JObject()
                                    {
                                        ["x"] = mloInstance.Rotation.X,
                                        ["y"] = mloInstance.Rotation.Y,
                                        ["z"] = mloInstance.Rotation.Z,
                                        ["w"] = mloInstance.Rotation.W,
                                    }
                                };

                                entry["mloInstances"].Add(mloInstanceEntry);
                            }
                        }

                        cache["ymap"].Add(entry);

                    }
                    else if (file.Name.EndsWith(".ytyp"))
                    {
                        var ytyp = new YtypFile();

                        using (MemoryStream ms = new MemoryStream())
                        {
                            file.Export(ms);
                            ytyp.Load(ms);
                        }

                        dynamic entry = new JObject()
                        {
                            ["name"] = fileNameWithoutExtension,
                            ["path"] = fullFileName,
                            ["hash"] = Jenkins.Hash(fileNameWithoutExtension),
                            ["mloArchetypes"] = new JArray(),
                        };

                        if (ytyp.CMapTypes.MloArchetypes != null)
                        {
                            for (int i = 0; i < ytyp.CMapTypes.MloArchetypes.Count; i++)
                            {
                                var archetype = ytyp.CMapTypes.MloArchetypes[i];
                                var mloEntry = new JObject
                                {
                                    ["name"] = archetype.Name,
                                    ["rooms"] = new JArray(),
                                };

                                if (archetype.Rooms != null)
                                {
                                    for (int j = 0; j < archetype.Rooms.Count; j++)
                                    {
                                        var room = archetype.Rooms[j];
                                        var roomEntry = new JObject
                                        {
                                            ["name"] = room.Name,
                                            ["bbMin"] = new JObject()
                                            {
                                                ["x"] = room.BbMin.X,
                                                ["y"] = room.BbMin.Y,
                                                ["z"] = room.BbMin.Z,
                                            },
                                            ["bbMax"] = new JObject()
                                            {
                                                ["x"] = room.BbMax.X,
                                                ["y"] = room.BbMax.Y,
                                                ["z"] = room.BbMax.Z,
                                            }
                                        };

                                        ((JArray)mloEntry["rooms"]).Add(roomEntry);

                                    }
                                }

                                entry["mloArchetypes"].Add(mloEntry);
                            }
                        }

                        cache["ytyp"].Add(entry);
                    }

                });

                var jsonString = JsonConvert.SerializeObject(cache, new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.None });

                File.WriteAllText(AssemblyDirectory + "\\cache.json", jsonString);

                using (StreamWriter writer = new StreamWriter(AssemblyDirectory + "\\strings.txt"))
                {
                    foreach(var kvp in Jenkins.Index)
                    {
                        writer.Write(kvp.Value + "\n");
                    }
                }
            });

            if (args.Length == 0 || args[0] == "help")
            {
                Console.Error.Write(CommandLine.GenHelp());
                return;
            }
        }

        public static void EnsurePath()
        {
            if (!File.Exists(Settings.Default.GTAFolder + "\\GTA5.exe"))
            {
                string path = Settings.Default.GTAFolder;

                while (!File.Exists(path + "\\GTA5.exe"))
                {
                    Console.Error.Write("GTAV folder : ");

                    path = Console.ReadLine();

                }

                Settings.Default.GTAFolder = path;

                Settings.Default.Save();
            }
        }

        public static void EnsureKeys()
        {
            GTA5Constants.PC_AES_KEY = Resource.gtav_aes_key;
            GTA5Constants.PC_NG_KEYS = CryptoIO.ReadNgKeys(Resource.gtav_ng_key);
            GTA5Constants.PC_NG_DECRYPT_TABLES = CryptoIO.ReadNgTables(Resource.gtav_ng_decrypt_tables);
            GTA5Constants.PC_LUT = Resource.gtav_hash_lut;
        }

        public static void EnsureCache()
        {
            if (File.Exists(AssemblyDirectory + "\\strings.txt"))
            {
                using (StreamReader reader = new StreamReader(AssemblyDirectory + "\\strings.txt"))
                {
                    string line;

                    while((line = reader.ReadLine()) != null)
                    {
                        Jenkins.Ensure(line);
                    }
                }
            }

            if (File.Exists(AssemblyDirectory + "\\cache.json"))
            {
                Cache = JObject.Parse(File.ReadAllText(AssemblyDirectory + "\\cache.json"));
            }
        }

    }
}
