using System;
using System.Collections.Generic;
using System.IO;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.ArchiveWrappers;
using RageLib.GTA5.Resources.PC.GameFiles;
using RageLib.GTA5.Utilities;
using RageLib.Hash;
using RageLib.Resources.GTA5.PC.Drawables;
using RageLib.Resources.GTA5.PC.GameFiles;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleCompileDrawableShadersOptions(string[] args)
        {
            CommandLine.Parse<CompileDrawableShadersOptions>(args, (opts, gOpts) =>
            {
                EnsurePath();
                EnsureKeys();

                var shaderNames             = new Dictionary<uint, string>();
                var shaderParameterSetNames = new Dictionary<uint, string>();
                var fxcShaders              = new Dictionary<uint, FxcFile>();

                ArchiveUtilities.ForEachBinaryFile(Settings.Default.GTAFolder, (fullFileName, binaryFile, encryption) =>
                {
                    if (fullFileName.EndsWith(".fxc"))
                    {
                        string nameLower = binaryFile.Name.ToLowerInvariant().Replace(".fxc", "");
                        var hash = Jenkins.Hash(nameLower);

                        if (!shaderNames.ContainsKey(hash))
                            shaderNames.Add(hash, nameLower);

                        byte[] data = Utils.GetBinaryFileData((IArchiveBinaryFile)binaryFile, encryption);
                        var fxc     = new FxcFile();

                        fxc.Load(data, nameLower);

                        if(!fxcShaders.ContainsKey(hash))
                            fxcShaders.Add(hash, fxc);
                    }
                    else if (fullFileName.EndsWith(".sps"))
                    {
                        string nameLower = binaryFile.Name.ToLowerInvariant();
                        var hash = Jenkins.Hash(nameLower);

                        if (!shaderParameterSetNames.ContainsKey(hash))
                            shaderParameterSetNames.Add(hash, nameLower);
                    }
                });

                ArchiveUtilities.ForEachResourceFile(Settings.Default.GTAFolder, (fullFileName, resourceFile, encryption) =>
                {
                    if (fullFileName.EndsWith(".ydr"))
                    {
                        var ms = new MemoryStream();

                        resourceFile.Export(ms);

                        var ydr = new YdrFile();

                        ydr.Load(ms);

                        Console.WriteLine(fullFileName.Replace(Settings.Default.GTAFolder, ""));

                        CompileDrawableShaders_ProcessDrawable(ydr.Drawable, shaderNames, fxcShaders);

                        Console.WriteLine("");
                    }
                    else if (fullFileName.EndsWith(".ydd"))
                    {
                        var ms = new MemoryStream();

                        resourceFile.Export(ms);

                        var ydd = new YddFile();

                        ydd.Load(ms);

                        Console.WriteLine(fullFileName.Replace(Settings.Default.GTAFolder, ""));

                        var drawables = ydd.DrawableDictionary.Drawables;

                        for(int d=0; d<drawables.Count; d++)
                        {
                            var drawable = drawables[d];

                            Console.WriteLine("  " + drawable.Name.Value);

                            CompileDrawableShaders_ProcessDrawable(drawable, shaderNames, fxcShaders);
                        }

                        Console.WriteLine("");
                    }

                });

            });
        }

        static void CompileDrawableShaders_ProcessDrawable(Drawable drawable, Dictionary<uint, string> shaderNames, Dictionary<uint, FxcFile> shaders)
        {
            for (int i = 0; i < drawable.ShaderGroup.Shaders.Count; i++)
            {
                var shader = drawable.ShaderGroup.Shaders[i];
                var fxc = shaders[shader.NameHash];
                uint hash = shader.NameHash;

                if (shaderNames.TryGetValue(hash, out string name))
                {
                    Console.WriteLine("    " + name);
                }
                else
                {
                    Console.WriteLine("    " + hash);
                }

                for (int j = 0; j < shader.ParametersList.Parameters.Count; j++)
                {
                    var param = shader.ParametersList.Parameters[j];
                    var paramHash = shader.ParametersList.Hashes[j];
    
                }
            }

            if (drawable.ShaderGroup.TextureDictionary != null)
            {

            }
        }
    }
}
