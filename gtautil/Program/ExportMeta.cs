using System;
using System.Collections.Generic;
using System.IO;
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
        static void HandleExportMetaOptions(string[] args)
        {
            CommandLine.Parse<ExportMetaOptions>(args, (opts, gOpts) =>
            {
                if(opts.Metadata)
                {
                    Init(args);
                }
                else
                {
                    EnsurePath();
                    EnsureKeys();
                    EnsureCache();
                }

                if (opts.InputFiles == null)
                {
                    Console.WriteLine("Please provide input files with -i --input");
                    return;
                }
                else
                {
                    var inputFiles = Utils.Expand(opts.InputFiles);

                    for (int i = 0; i < inputFiles.Length; i++)
                    {
                        var fileInfo = inputFiles[i];

                        Console.WriteLine(fileInfo.FullName);

                        MetaFile meta = null;

                        if(fileInfo.Name.EndsWith(".ymap") && opts.Metadata)
                        {
                            var ymap = new YmapFile();

                            ymap.Load(fileInfo.FullName);

                            meta = ymap.ResourceFile.ResourceData;

                            var basePath      = Path.GetDirectoryName(fileInfo.FullName);
                            var topParent     = ImportMeta_GetTopYmapParent((uint)ymap.CMapData.Name);
                            var topParentHash = (uint)topParent["hash"];
                            var topParentName = (string)topParent["name"];
                            var topParentPath = (string)topParent["path"];
                            var topParentYmap = new YmapFile();

                            Console.WriteLine("Top parent is " + topParentName);

                            if (File.Exists(basePath + "\\" + topParentName + ".ymap"))
                            {
                                topParentYmap.Load(basePath + "\\" + topParentName + ".ymap");
                            }
                            else
                            {
                                ArchiveUtilities.ForEachResourceFile(Settings.Default.GTAFolder, (fullFileName, file, encryption) =>
                                {
                                    if (fullFileName == topParentPath)
                                    {
                                        var ms = new MemoryStream();
                                        file.Export(ms);
                                        topParentYmap.Load(ms);
                                    }
                                });
                            }

                            var children   = ImportMeta_GetYmapChildrens(topParent);
                            var ymaps      = new List<YmapFile>() { topParentYmap };
                            var nameHashes = new Dictionary<uint, string>();

                            nameHashes.Add((uint)topParent["hash"], (string)topParent["name"]);

                            for (int j = 0; j < children.Count; j++)
                            {
                                var cYmap = new YmapFile();
                                var child = children[j];
                                var hash = (uint)child["hash"];
                                var name = (string)child["name"];
                                var path = (string)child["path"];

                                nameHashes.Add(hash, name);

                                if (File.Exists(basePath + "\\" + name + ".ymap"))
                                {
                                    cYmap.Load(basePath + "\\" + name + ".ymap");
                                }
                                else
                                {
                                    Console.WriteLine("Grabbing missing " + name + " from install directory (very slowly, needs optimization)");

                                    ArchiveUtilities.ForEachResourceFile(Settings.Default.GTAFolder, (fullFileName, file, encryption) =>
                                    {
                                        if (fullFileName == path)
                                        {
                                            var ms = new MemoryStream();

                                            file.Export(ms);

                                            cYmap.Load(ms);
                                        }
                                    });
                                }

                                ymaps.Add(cYmap);
                            }

                            ymaps[ymaps.FindIndex(e => e.CMapData.Name == ymap.CMapData.Name)] = ymap;

                            for (int j = 0; j < ymaps.Count; j++)
                            {
                                ymaps[j].CMapData.ParentMapData = ymaps.Find(e => e.CMapData.Name == ymaps[j].CMapData.Parent)?.CMapData;
                            }

                            for (int j = 0; j < ymaps.Count; j++)
                            {
                                var ymap2   = ymaps[j];
                                var mapping = new Dictionary<uint, int>();
                                var name    = nameHashes[(uint)ymap2.CMapData.Name];

                                ymap2.Save(basePath + "\\" + name + ".ymap");

                                var data = new JObject
                                {
                                    ["mapping"] = new JArray()
                                };

                                var dataMapping = (JArray)(data["mapping"]);

                                for (int k = 0; k < ymap2.CMapData.Entities.Count; k++)
                                {
                                    var entity = ymap2.CMapData.Entities[k];

                                    if (mapping.ContainsKey(entity.Guid))
                                    {
                                        Console.WriteLine("Duplicate GUID found => " + entity.Guid + " at index " + j + " ABORTING");
                                        return;

                                    }
                                    else
                                    {
                                        mapping.Add(entity.Guid, k);

                                        var entry = new JObject()
                                        {
                                            ["guid"] = entity.Guid,
                                            ["hasParent"] = entity.ParentIndex != -1,
                                        };

                                        if(entity.ParentIndex != -1)
                                        {
                                            entry["parent"]     = entity.ParentEntity.Guid;
                                            entry["parentName"] = MetaXml.HashString((MetaName)entity.ParentEntity.Guid);
                                            entry["parentYmap"] = nameHashes[(uint)entity.ParentEntity.Parent.Name];
                                        }

                                        dataMapping.Add(entry);
                                    }
                                }

                                var jsonString = JsonConvert.SerializeObject(data, new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.Indented });

                                File.WriteAllText(basePath + "\\" + name + ".ymap.json", jsonString);
                            }
                        }
                        else
                        {
                            var res = new ResourceFile_GTA5_pc<MetaFile>();
                            res.Load(fileInfo.FullName);
                            meta = res.ResourceData;
                        }

                        var xml = MetaXml.GetXml(meta);

                        string fileName = fileInfo.FullName + ".xml";

                        File.WriteAllText(fileName, xml);
                    }
                }
            });
        }
    }
}
