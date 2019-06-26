using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RageLib.GTA5.ResourceWrappers.PC.Meta;
using RageLib.GTA5.ResourceWrappers.PC.Meta.Structures;
using RageLib.GTA5.Utilities;
using RageLib.Hash;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.Resources.GTA5.PC.Meta;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleImportMetaOptions(string[] args)
        {
            CommandLine.Parse<ImportMetaOptions>(args, (opts, gOpts) =>
            {
                if (opts.Metadata)
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

                        var strings = MetaUtilities.GetAllStringsFromXml(fileInfo.FullName);

                        foreach (var str in strings)
                            Utils.Hash(str.ToLowerInvariant());

                        var doc = new XmlDocument();

                        doc.Load(fileInfo.FullName);

                        var res          = new ResourceFile_GTA5_pc<MetaFile>();
                        res.Version      = 2;
                        res.ResourceData = XmlMeta.GetMeta(doc);

                        if (fileInfo.Name.EndsWith(".ymap.xml") && opts.Metadata)
                        {
                            var toDelete = opts.Delete?.Select(e => Convert.ToUInt32(e)).ToList() ?? new List<uint>();

                            var mappings   = new Dictionary<uint, Dictionary<uint, JObject>>();
                            var basePath   = Path.GetDirectoryName(fileInfo.FullName);
                            var ymaps      = new List<YmapFile>();
                            var ymap       = new YmapFile();
                            var nameHashes = new Dictionary<uint, string>();

                            ymap.ResourceFile = res;

                            ymap.Parse();

                            var topParent     = ImportMeta_GetTopYmapParent((uint)ymap.CMapData.Name);
                            var topParentHash = (uint)topParent["hash"];
                            var topParentName = (string)topParent["name"];
                            var topParentPath = (string)topParent["path"];
                            var topParentYmap = new YmapFile();

                            Console.WriteLine("Top parent is " + topParentName);

                            var entries = new List<JObject>() { topParent };

                            entries.AddRange(ImportMeta_GetYmapChildrens(topParent));

                            for (int j = 0; j < entries.Count; j++)
                            {
                                var entry        = entries[j];
                                var entryHash    = (uint)entry["hash"];
                                var entryName    = (string)entry["name"];
                                var entryPath    = (string)entry["path"];
                                var ymapPath     = basePath + "\\" + entryName + ".ymap";
                                var metadataPath = ymapPath + ".json";

                                nameHashes.Add(entryHash, entryName);

                                if (!File.Exists(ymapPath))
                                {
                                    Console.WriteLine("ERROR => File not found : " + entryName + ".ymap");
                                    return;
                                }

                                if (!File.Exists(metadataPath))
                                {
                                    Console.WriteLine("ERROR => Metadata not found for " + entryName);
                                    return;
                                }

                                var metadataMapping = (JArray) JObject.Parse(File.ReadAllText(metadataPath))["mapping"];
                                var mapping         = new Dictionary<uint, JObject>();

                                for(int k=0; k<metadataMapping.Count; k++)
                                {
                                    mapping.Add((uint)metadataMapping[k]["guid"], (JObject)metadataMapping[k]);
                                }

                                mappings.Add(entryHash, mapping);

                                if(entryHash == (uint) ymap.CMapData.Name)
                                {
                                    ymaps.Add(ymap);
                                }
                                else
                                {
                                    var ymap2 = new YmapFile();
                                    ymap2.Load(ymapPath);
                                    ymaps.Add(ymap2);
                                }
                            }

                            for(int j=0; j<ymaps.Count; j++)
                            {
                                var ymap2 = ymaps[j];

                                if (ymap2.CMapData.Parent != 0)
                                {
                                    ymap2.CMapData.ParentMapData = ymaps.Find(e => e.CMapData.Name == ymap2.CMapData.Parent).CMapData;
                                }
                            }

                            bool modified;

                            do
                            {
                                modified = false;

                                for (int j = 0; j < ymaps.Count; j++)
                                {
                                    var ymap2 = ymaps[j];

                                    Console.WriteLine(nameHashes[(uint)ymap2.CMapData.Name]);

                                    var toRemove = new List<MCEntityDef>();
                                    var toSet = new List<Tuple<MCEntityDef, MCEntityDef>>();
                                    bool currModified = false;

                                    for (int k = 0; k < ymap2.CMapData.Entities.Count; k++)
                                    {
                                        var entity = ymap2.CMapData.Entities[k];
                                        var oldHasParent = (bool)mappings[(uint)ymap2.CMapData.Name][entity.Guid]["hasParent"];
                                        var currHasParent = entity.ParentIndex != -1;

                                        if (oldHasParent)
                                        {
                                            var oldParent = (uint)mappings[(uint)ymap2.CMapData.Name][entity.Guid]["parent"];
                                            var oldParentYmapName = (string)mappings[(uint)ymap2.CMapData.Name][entity.Guid]["parentYmap"];
                                            var oldParentYmap = Utils.Hash(oldParentYmapName);

                                            if (currHasParent)
                                            {
                                                if (entity.ParentEntity == null || entity.ParentEntity.Guid != oldParent)
                                                {
                                                    var parentYmap = ymaps.Find(e => (uint)e.CMapData.Name == oldParentYmap);
                                                    var parentIdx = parentYmap.CMapData.Entities.FindIndex(e => e.Guid == oldParent);

                                                    if (parentIdx == -1)
                                                    {
                                                        Console.WriteLine("DELETE " + entity.Guid + " => Missing parent (" + oldParentYmapName + ")");
                                                        toRemove.Add(entity);
                                                        modified = true;
                                                        currModified = true;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("ASSIGN parent " + oldParent + " to " + entity.Guid);
                                                        var parent = parentYmap.CMapData.Entities[parentIdx];
                                                        toSet.Add(new Tuple<MCEntityDef, MCEntityDef>(entity, parent));
                                                        modified = true;
                                                        currModified = true;
                                                    }
                                                }
                                                else
                                                {
                                                    if (toDelete.IndexOf(oldParent) != -1 || (opts.DeleteScope == "full" && toDelete.IndexOf(entity.Guid) != -1))
                                                    {
                                                        Console.WriteLine("DELETE " + entity.Guid + " => Marked for deletion @" + opts.DeleteMode);
                                                        toRemove.Add(entity);
                                                        modified = true;
                                                        currModified = true;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (opts.DeleteMode == "dummy")
                                    {
                                        for (int k = 0; k < toRemove.Count; k++)
                                        {
                                            toRemove[k].ArchetypeName = Utils.Hash("gtautil_dummy");
                                        }
                                    }
                                    else
                                    {
                                        ymap2.CMapData.RemoveEntities(toRemove);
                                    }

                                    for (int k = 0; k < toSet.Count; k++)
                                    {
                                        toSet[k].Item1.ParentEntity = toSet[k].Item2;
                                    }

                                    if (currModified)
                                    {
                                        Console.WriteLine("MODIFIED");
                                    }
                                }

                            } while (modified && opts.DeleteMode != "dummy");

                            for(int j=0; j<ymaps.Count; j++)
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

                                        if (entity.ParentIndex != -1)
                                        {
                                            entry["parent"] = entity.ParentEntity.Guid;
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
                            string fileName = fileInfo.FullName.Replace(".xml", "");
                            res.Save(fileName);
                        }

                    }

                    using (StreamWriter writer = new StreamWriter(AssemblyDirectory + "\\strings.txt"))
                    {
                        foreach (var kvp in Jenkins.Index)
                        {
                            writer.Write(kvp.Value + "\n");
                        }
                    }
                }
            });
        }

        public static JObject ImportMeta_GetTopYmapParent(uint nameHash)
        {
            var     cYmapInfo      = ((JArray)Cache["ymap"]).Select(e => e).ToList();
            uint    topParentName  = nameHash;
            string  topParentPath  = (string) cYmapInfo.Find(e => (uint)e["hash"] == nameHash)["path"];
            uint    lastParentName = 0;
            JObject lastEntry      = null;

            do
            {
                int topParentLevel = -1;

                for (int i = 0; i < cYmapInfo.Count; i++)
                {
                    var entry = (JObject)cYmapInfo[i];

                    var hash = (uint)entry["hash"];
                    var name = (string)entry["name"];

                    if (hash == topParentName)
                    {
                        var parentName = (uint)entry["parent"];
                        var level = GetDLCLevel((string)entry["path"]);

                        if (level > topParentLevel)
                        {
                            topParentName = parentName;
                            topParentPath = (string)entry["path"];
                            topParentLevel = level;
                            lastParentName = parentName;

                            lastEntry = entry;
                        }
                    }
                }


            } while (lastParentName != 0);

            return lastEntry;
        }

        public static List<JObject> ImportMeta_GetYmapChildrens(JObject ymapEntry)
        {
            var cYmapInfo = ((JArray)Cache["ymap"]).Select(e => e).ToList();
            var entries   = new List<JObject>();
            var queue     = new List<JObject>() { ymapEntry };
            var hashes    = new HashSet<uint>();

            while(queue.Count > 0)
            {
                var curr = queue[0];

                if (curr != ymapEntry)
                    entries.Add(curr);

                queue.RemoveAt(0);

                var hash        = (uint)curr["hash"];
                var bestLevels  = new Dictionary<uint, int>();
                var bestEntries = new Dictionary<uint, JObject>();

                hashes.Add(hash);

                for (int i = 0; i < cYmapInfo.Count; i++)
                {
                    var entry = (JObject)cYmapInfo[i];

                    var cParent = (uint)entry["parent"];

                    if(hashes.Contains(cParent))
                    {
                        var cHash = (uint)  entry["hash"];
                        var cPath = (string)entry["path"];
                        var level = GetDLCLevel(cPath);

                        if(!bestLevels.TryGetValue(cHash, out int bestLevel) || level > bestLevel)
                        {
                            bestLevel = -1;

                            if (bestLevels.ContainsKey(cHash))
                            {
                                bestLevel = bestLevels[cHash];
                            }

                            if(level > bestLevel)
                            {
                                if (bestLevels.ContainsKey(cHash))
                                {
                                    bestLevels[cHash]  = bestLevel;
                                    bestEntries[cHash] = entry;
                                }
                                else
                                {
                                    bestLevels.Add(cHash, bestLevel);
                                    bestEntries.Add(cHash, entry);
                                }
                            }
                        }


                    }
                }

                foreach(var entry in bestEntries)
                {
                    if(entries.Find(e => (uint) e["hash"] == (uint) entry.Value["hash"]) == null && queue.Find(e => (uint)e["hash"] == (uint)entry.Value["hash"]) == null)
                    {
                        Console.WriteLine("Found child => " + (string)entry.Value["name"]);

                        queue.Add(entry.Value);
                    }
                }
            }

            return entries;
        }
    }

}
