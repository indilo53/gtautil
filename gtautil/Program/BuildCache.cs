using System;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RageLib.GTA5.ResourceWrappers.PC.Meta;
using RageLib.GTA5.Utilities;
using RageLib.Hash;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.Resources.GTA5.PC.Meta;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleBuildCacheOptions(string[] args)
        {
            CommandLine.Parse<BuildCacheOptions>(args, (opts, gOpts) =>
            {
                Init(args);

                dynamic cache = new JObject();

                cache["ymap"] = new JArray();
                cache["ytyp"] = new JArray();

                var names = FileUtilities.GetAllFileNamesWithoutExtension(Settings.Default.GTAFolder);

                foreach(var name in names)
                    Jenkins.Ensure(name.ToLowerInvariant());

                ArchiveUtilities.ForEachFile(Settings.Default.GTAFolder, (fullFileName, file, encryption) =>
                {
                    Console.WriteLine(fullFileName);

                    string fileNameWithoutExtension = FileUtilities.RemoveExtension(file.Name);

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
                    foreach (var kvp in Jenkins.Index)
                    {
                        writer.Write(kvp.Value + "\n");
                    }
                }
            });
        }
    }
}
