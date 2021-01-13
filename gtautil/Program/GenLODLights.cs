
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
        static void HandleGenLODLightsOptions(string[] args)
        {
            CommandLine.Parse<GenLODLigthsOptions>(args, (opts, gOpts) =>
            {
                if (opts.CreateMode)
                {
                    if (opts.OutputDirectory == null)
                    {
                        Console.Error.WriteLine("Please provide output directory with --output");
                        return;
                    }

                    Init(args);

                    if (!Directory.Exists(opts.OutputDirectory))
                        Directory.CreateDirectory(opts.OutputDirectory);

                    var mapping = new Dictionary<string, int>();

                    ArchiveUtilities.ForEachResourceFile(Settings.Default.GTAFolder, (fullFileName, file, encryption) =>
                    {
                        if (file.Name.EndsWith(".ymap") && file.Name.Contains("lodlights"))
                        {
                            Console.WriteLine(file.Name);

                            int level = GetDLCLevel(fullFileName);
                            int oldLevel;

                            if (!mapping.TryGetValue(file.Name, out oldLevel))
                            {
                                oldLevel = -1;
                                mapping.Add(file.Name, level);
                            }

                            if (level > oldLevel)
                            {
                                file.Export(opts.OutputDirectory + "\\" + file.Name);
                            }
                        }
                    });

                }
                else if (opts.DeleteMode)
                {
                    Init(args);

                    if (opts.InputDirectory == null)
                    {
                        Console.Error.WriteLine("Please provide input directory with --input");
                        return;
                    }

                    if (opts.Position == null || opts.Position.Count != 3)
                    {
                        Console.Error.WriteLine("Please provide position with --position x,y,z");
                        return;
                    }

                    if (!Directory.Exists(opts.InputDirectory + "\\modified"))
                        Directory.CreateDirectory(opts.InputDirectory + "\\modified");

                    Vector3 position = new Vector3(opts.Position[0], opts.Position[1], opts.Position[2]);
                    string[] files = Directory.GetFiles(opts.InputDirectory, "*.ymap");

                    var ymaps = new Dictionary<string, YmapFile>();

                    for (int i = 0; i < files.Length; i++)
                    {
                        string path = files[i];
                        string name = files[i].Replace(".ymap", "");
                        var ymap    = new YmapFile();

                        Console.WriteLine("LOAD " + name);

                        ymap.Load(files[i]);
                        ymaps.Add(name, ymap);
                    }

                    var modified = new Dictionary<string, YmapFile>();

                    foreach (var item in ymaps)
                    {
                        string name = item.Key;
                        YmapFile ymap = item.Value;

                        for (int j = ymap.CMapData.DistantLODLightsSOA.Entries.Count - 1; j >= 0; j--)
                        {
                            var entry      = ymap.CMapData.DistantLODLightsSOA.Entries[j];
                            var children   = new Dictionary<string, YmapFile>();
                            float distance = Vector3.Distance(position, entry.Position);

                            foreach (var item2 in ymaps)
                            {
                                if(item2.Value.CMapData.Parent == ymap.CMapData.Name)
                                {
                                    children.Add(item2.Key, item2.Value);
                                }
                            }

                            if (distance <= opts.Radius)
                            {
                                Console.WriteLine("Found DistLODLight in " + name + " at index " + j);
                                Console.WriteLine("  Delete : " + name + "@" + j);

                                ymap.CMapData.DistantLODLightsSOA.Entries.RemoveAt(j);

                                if (!modified.ContainsValue(ymap))
                                {
                                    modified.Add(name, ymap);
                                }

                                foreach (var item2 in children)
                                {
                                    string name2 = item2.Key;
                                    YmapFile ymap2 = item2.Value;

                                    Console.WriteLine("  Delete : " + name2 + "@" + j);
                                    item2.Value.CMapData.LODLightsSOA.Entries.RemoveAt(j);

                                    if (!modified.ContainsValue(ymap2))
                                    {
                                        modified.Add(name2, ymap2);
                                    }
                                }
                            }
                        }
                    }

                    foreach (var item in modified)
                    {
                        var descendant = item.Key.Substring(item.Key.LastIndexOf("\\"));
                        item.Value.Save(opts.InputDirectory + "\\" + descendant + ".ymap");
                        item.Value.Save(opts.InputDirectory + "\\modified\\" + descendant + ".ymap");
                    }
                }

            });
        }
    }
}
