using System;
using System.Collections.Generic;
using SharpDX;
using RageLib.Hash;
using RageLib.Resources.GTA5.PC.Meta;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.GTA5.ResourceWrappers.PC.Meta.Structures;

namespace GTAUtil
{
    partial class Program
    {
        static Dictionary<MetaName, MCBaseArchetypeDef> Archetypes = new Dictionary<MetaName, MCBaseArchetypeDef>();

        static void HandleGenPropDefsOptions(string[] args)
        {
            CommandLine.Parse<GenPropDefsOptions>(args, (opts, gOpts) =>
            {
                if (opts.InputFiles != null)
                {
                    var inputFiles = Utils.Expand(opts.InputFiles);
                    var ytyp = new YtypFile();

                    if(opts.Ytyp != null)
                    {
                        var inputYtyps = Utils.Expand(opts.Ytyp);
                        
                        for(int i=0; i<inputYtyps.Length; i++)
                        {
                            var ytyp2 = new YtypFile();
                            ytyp2.Load(inputYtyps[i].FullName);

                            for(int j=0; j<ytyp2.CMapTypes.Archetypes.Count; j++)
                            {
                                var archetype = ytyp2.CMapTypes.Archetypes[j];

                                if (Archetypes.TryGetValue(archetype.Name, out MCBaseArchetypeDef arch))
                                    Archetypes[archetype.Name] = archetype;
                                else
                                    Archetypes.Add(archetype.Name, archetype);
                            }
                        }
                    }

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
                                        var nameHash = (MetaName)Jenkins.Hash(name);

                                        var ydr = new YdrFile();
                                        ydr.Load(fileInfo.FullName);

                                        if (Archetypes.TryGetValue(nameHash, out MCBaseArchetypeDef arch))
                                        {
                                            arch.BbMin = (Vector3)(Vector4)ydr.Drawable.BoundingBoxMin;
                                            arch.BbMax = (Vector3)(Vector4)ydr.Drawable.BoundingBoxMax;
                                            arch.BsCentre = (Vector3)ydr.Drawable.BoundingCenter;
                                            arch.BsRadius = ydr.Drawable.BoundingSphereRadius;
                                        }
                                        else
                                        {
                                            arch = new MCBaseArchetypeDef();

                                            arch.Name = nameHash;
                                            arch.AssetName = nameHash;
                                            arch.TextureDictionary = nameHash;
                                            arch.PhysicsDictionary = (MetaName)Jenkins.Hash("prop_" + name);
                                            arch.Flags = 32;
                                            arch.AssetType = Unk_1991964615.ASSET_TYPE_DRAWABLE;
                                            arch.BbMin = (Vector3)(Vector4)ydr.Drawable.BoundingBoxMin;
                                            arch.BbMax = (Vector3)(Vector4)ydr.Drawable.BoundingBoxMax;
                                            arch.BsCentre = (Vector3)ydr.Drawable.BoundingCenter;
                                            arch.BsRadius = ydr.Drawable.BoundingSphereRadius;
                                            arch.LodDist = 500f;
                                            arch.HdTextureDist = 5;
                                        }

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

                    string path = (opts.OutputDirectory == null) ? @".\props.ytyp" : opts.OutputDirectory + @"\props.ytyp";

                    ytyp.Save(path);
                }
            });
        }
    }
}
