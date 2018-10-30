
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.Utilities;
using RageLib.Hash;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.Drawables;
using RageLib.Resources.GTA5.PC.GameFiles;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleGenericOptions(string[] args)
        {
            CommandLine.Parse<GenericOptions>(args, (opts, gOpts) =>
            {
                int level = DLCList.Length - 1;

                if(opts.DlcLevel != null)
                {
                    level = Array.IndexOf(DLCList, opts.DlcLevel.ToLowerInvariant());

                    if (level == -1)
                        level = DLCList.Length - 1;
                }

                EnsureFiles(level);
                // EnsureArchetypes(level);

                if(opts.Mods != null)
                {
                    Console.WriteLine("Loading mods");

                    var infos = Utils.Expand(opts.Mods);

                    for(int i=0; i<infos.Length; i++)
                    {
                        var name = Path.GetFileNameWithoutExtension(infos[i].Name);
                        var ext = Path.GetExtension(infos[i].Name);
                        var hash = Jenkins.Hash(name.ToLowerInvariant());

                        switch (ext)
                        {
                            case ".ydr":
                                {
                                    var ydr = new YdrFile();
                                    ydr.Load(infos[i].FullName);
                                    DrawableCache[hash] = ydr.Drawable;
                                    break;
                                }

                            default: break;
                        }
                    }
                }

                return;
            });
        }
    }
}
