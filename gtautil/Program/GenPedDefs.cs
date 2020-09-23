using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RageLib.GTA5.ResourceWrappers.PC.Meta;
using RageLib.GTA5.ResourceWrappers.PC.Meta.Structures;
using RageLib.GTA5.Utilities;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.Resources.GTA5.PC.Meta;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleGenPedDefsOptions(string[] args)
        {
            CommandLine.Parse<GenPedDefsOptions>(args, (opts, gOpts) =>
            {
                var ymtRegex = new Regex("mp_(m|f)_freemode_01.*\\.ymt$");
                var cYddRegex = new Regex("(head|berd|hair|uppr|lowr|hand|feet|teef|accs|task|decl|jbib)_(\\d\\d\\d)_u.ydd$");
                var pYddRegex = new Regex("p_(head|ears|mouth|lhand|rhand|lwrist|rwrist|hip|lfoot|rfoot)_(\\d\\d\\d).ydd$");

                var fileDlcLevels = new Dictionary<string, int>();
                var overrides = new Dictionary<string, List<string>>();

                if (opts.CreateMode)
                {
                    if (opts.OutputDirectory == null)
                    {
                        Console.WriteLine("Please provide input directory with --output");
                        return;
                    }

                    if (!Directory.Exists(opts.OutputDirectory))
                    {
                        Directory.CreateDirectory(opts.OutputDirectory);
                    }

                    Init(args);

                    int maxDLCLevel = (Array.IndexOf(DLCList, opts.DLCLevel) == -1) ? DLCList.Length - 1 : Array.IndexOf(DLCList, opts.DLCLevel);

                    var targets = opts.Targets?.ToList() ?? new List<string>();
                    var dlcpaths = new Dictionary<string, int>();
                    var dlcdirs = new Dictionary<string, int>();
                    var processed = new List<string>();

                    ArchiveUtilities.ForEachFile(Settings.Default.GTAFolder, (fullFileName, file, encryption) =>
                    {
                        string[] path = fullFileName.Split('\\');
                        string folder = path[path.Length - 2];
                        string fileName = path[path.Length - 1];
                        string name = fileName.Split('.').First();
                        string outPath;
                        bool isOverride = false;

                        var ymtMatch = ymtRegex.Match(fileName);
                        var cYddMatch = cYddRegex.Match(fileName);
                        var pYddMatch = pYddRegex.Match(fileName);

                        int dlcLevel = GetDLCLevel(fullFileName);

                        if (dlcLevel > maxDLCLevel)
                            return;

                        if(targets.Count > 0 && opts.Targets.Where(e => e == name || (e + "_p") == name || e == folder || (e + "_p") == folder).ToArray().Length == 0)
                        {
                            return;
                        }

                        // Found interesting entry
                        if (ymtMatch.Success || cYddMatch.Success || pYddMatch.Success)
                        {
                            string pathPart;

                            // Found definition file (.ymt)
                            if (ymtMatch.Success)
                            {
                                if (!path[path.Length - 1].Contains("_freemode_01"))
                                    return;

                                outPath = opts.OutputDirectory;
                                pathPart = path[path.Length - 1];

                                if (!dlcpaths.ContainsKey(pathPart))
                                {
                                    dlcpaths[pathPart] = dlcLevel;
                                    isOverride = true;
                                }
                                else if(dlcLevel > dlcpaths[pathPart])
                                {
                                    dlcpaths[pathPart] = dlcLevel;
                                    isOverride = true;
                                }

                            }
                            else  // Found model file (component or prop)
                            {
                                if (!path[path.Length - 2].Contains("_freemode_01"))
                                    return;

                                outPath = opts.OutputDirectory + "\\" + path[path.Length - 2];
                                pathPart = path[path.Length - 2] + "\\" + path[path.Length - 1];
                                string dir = path[path.Length - 2];
                                dlcLevel = GetDLCLevel(fullFileName);

                               // Console.WriteLine(pathPart);

                                if (!dlcpaths.ContainsKey(pathPart))
                                {
                                    dlcpaths[pathPart] = dlcLevel;

                                    Console.WriteLine(DLCList[dlcLevel] + " [" + dlcLevel + "] => " + pathPart);
                                    dlcdirs[dir] = dlcLevel;
                                    isOverride = true;

                                }
                                else if (dlcLevel > dlcdirs[dir])
                                {
                                    Console.WriteLine(DLCList[dlcLevel] + " [" + dlcLevel + "] => " + pathPart);
                                    dlcpaths[pathPart] = dlcLevel;
                                    dlcdirs[dir] = dlcLevel;
                                    isOverride = true;
                                }

                            }

                            Directory.CreateDirectory(outPath);

                            // If dlc level of this directory is superior to current matching one
                            if(isOverride)
                            {
                                // Write higher level ymt
                                if (ymtMatch.Success)
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        file.Export(ms);

                                        var rMeta = new ResourceFile_GTA5_pc<MetaFile>();

                                        rMeta.Load(ms);

                                        string xml = MetaXml.GetXml(rMeta.ResourceData);

                                        File.WriteAllText(outPath + "\\" + fileName + ".xml", xml);
                                    }
                                }
                                else if(cYddMatch.Success)
                                {
                                    foreach (var entry in ComponentFilePrefix)
                                    {
                                        Directory.CreateDirectory(outPath + "\\components\\" + entry.Value);
                                    }
                                }
                                else if(pYddMatch.Success)
                                {
                                    foreach (var entry in AnchorFilePrefix)
                                    {
                                        Directory.CreateDirectory(outPath + "\\props\\" + entry.Value);
                                    }
                                }

                                if(cYddMatch.Success || pYddMatch.Success)
                                {
                                    dynamic directoryInfos = new JObject();

                                    string dlc = DLCList[dlcLevel];

                                    directoryInfos["dlc"] = dlc;
                                    directoryInfos["path"] = Directory.GetParent(fullFileName);

                                    var jsonString = JsonConvert.SerializeObject(directoryInfos, new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.Indented });

                                    File.WriteAllText(outPath + "\\directory.json", jsonString);
                                }

                            }


                        }

                    });
                }
                else
                {
                    if (opts.InputDirectory == null)
                    {
                        Console.WriteLine("Please provide input directory with --input");
                        return;
                    }

                    if (opts.OutputDirectory == null)
                    {
                        Console.WriteLine("Please provide input directory with --output");
                        return;
                    }

                    Init(args);

                    int maxDLCLevel = (Array.IndexOf(DLCList, opts.DLCLevel) == -1) ? DLCList.Length - 1 : Array.IndexOf(DLCList, opts.DLCLevel);

                    string[] files = Directory.GetFiles(opts.InputDirectory).Where(e => e.EndsWith("ymt.xml")).ToArray();
                    string[] dirs = Directory.GetDirectories(opts.InputDirectory);
                    var addonDirs = new List<string>();

                    var ymts = new Dictionary<string, YmtPedDefinitionFile>();

                    var processedYmts = new Dictionary<string, Tuple<
                        Dictionary<string, Tuple<string, int, int, int, string, string>>,
                        Dictionary<string, int>,
                        Dictionary<string, Tuple<string, int, int, int, string, string>>,
                        Dictionary<string, int>
                    >>();

                    for(int j=0; j<files.Length; j++)
                    {
                        string targetMetaXml = files[j];
                        string targetName = targetMetaXml.Split('\\').Last().Replace(".ymt.xml", "");
                        string parentDirectoryPath = Directory.GetParent(targetMetaXml).FullName;
                        string parentDirectoryName = parentDirectoryPath.Split('\\').Last();

                        // Parse .ymt.xml
                        string xml = File.ReadAllText(targetMetaXml);
                        var doc = new XmlDocument();
                        doc.LoadXml(xml);
                        var meta = XmlMeta.GetMeta(doc);
                        var ymt = new YmtPedDefinitionFile();
                        ymt.ResourceFile.ResourceData = meta;
                        ymt.Parse();

                        ymts[targetName] = ymt;

                    }

                    if(opts.FiveMFormat)
                    {
                        Directory.CreateDirectory(opts.OutputDirectory + "\\stream");
                        File.Create(opts.OutputDirectory + "\\__resource.lua");
                    }
                    else
                    {
                        Directory.CreateDirectory(opts.OutputDirectory + "\\x64\\models\\cdimages\\streamedpeds_mp.rpf");
                        Directory.CreateDirectory(opts.OutputDirectory + "\\x64\\models\\cdimages\\streamedpedprops.rpf");

                        string contentXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<CDataFileMgr__ContentsOfDataFileXml>
    <disabledFiles />
    <includedXmlFiles />
    <includedDataFiles />
    <dataFiles>
        <Item>
            <filename>dlc_gtauclothes:/%PLATFORM%/models/cdimages/streamedpeds_mp.rpf</filename>
            <fileType>RPF_FILE</fileType>
            <overlay value=""true"" />
            <disabled value=""true"" />
            <persistent value=""true"" />
        </Item>
        <Item>
            <filename>dlc_gtauclothes:/%PLATFORM%/models/cdimages/streamedpedprops.rpf</filename>
            <fileType>RPF_FILE</fileType>
            <overlay value=""true"" />
            <disabled value=""true"" />
            <persistent value=""true"" />
        </Item>
    </dataFiles>
    <contentChangeSets>
        <Item>
            <changeSetName>gtauclothes_AUTOGEN</changeSetName>
            <filesToDisable />
            <filesToEnable>
                <Item>dlc_gtauclothes:/%PLATFORM%/models/cdimages/streamedpeds_mp.rpf</Item>
                <Item>dlc_gtauclothes:/%PLATFORM%/models/cdimages/streamedpedprops.rpf</Item>
            </filesToEnable>
            <txdToLoad />
            <txdToUnload />
            <residentResources />
            <unregisterResources />
        </Item>
    </contentChangeSets>
    <patchFiles />
</CDataFileMgr__ContentsOfDataFileXml>";

                        string setup2Xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<SSetupData>
    <deviceName>dlc_gtauclothes</deviceName>
    <datFile>content.xml</datFile>
    <timeStamp>03/30/2018 17:26:39</timeStamp>
    <nameHash>gtauclothes</nameHash>
    <contentChangeSetGroups>
        <Item>
            <NameHash>GROUP_STARTUP</NameHash>
            <ContentChangeSets>
                <Item>gtauclothes_AUTOGEN</Item>
            </ContentChangeSets>
        </Item>
    </contentChangeSetGroups>
</SSetupData>";

                        File.WriteAllText(opts.OutputDirectory + "\\content.xml", contentXml);
                        File.WriteAllText(opts.OutputDirectory + "\\setup2.xml", setup2Xml);
                    }

                    foreach (var ymtEntry in ymts)
                    {

                        var targetName = ymtEntry.Key;
                        var ymt = ymtEntry.Value;

                        // Components
                        var cCount = new Dictionary<Unk_884254308, int>();
                        var cYddMapping = new Dictionary<string, Tuple<string, int, int, int, string, string>>();  // sourceYddPath => prefix, origPos, pos, count, folder, yddFileName
                        var cTextureCount = new Dictionary<string, int>();

                        // Props
                        var pCount = new Dictionary<Unk_2834549053, int>();
                        var pYddMapping = new Dictionary<string, Tuple<string, int, int, int, string, string>>();   // sourceYddPath => prefix, origPos, pos, count, folder, yddFileName
                        var pTextureCount = new Dictionary<string, int>();

                        // Set component base count
                        Unk_884254308[] cValues = (Unk_884254308[])Enum.GetValues(typeof(Unk_884254308));

                        foreach (Unk_884254308 component in cValues)
                        {
                            if (component == Unk_884254308.PV_COMP_INVALID || component == Unk_884254308.PV_COMP_MAX)
                                continue;

                            cCount[component] = (ymt.Unk_376833625.Components[component] ?? new MUnk_3538495220()).Unk_1756136273.Count;
                        }

                        // Set prop base count
                        Unk_2834549053[] pValues = (Unk_2834549053[])Enum.GetValues(typeof(Unk_2834549053));

                        foreach (Unk_2834549053 anchor in pValues)
                        {
                            if (anchor == Unk_2834549053.NUM_ANCHORS)
                                continue;

                            int max = (opts.ReservePropEntries > ymt.Unk_376833625.PropInfo.Props[anchor].Count) ? opts.ReservePropEntries : ymt.Unk_376833625.PropInfo.Props[anchor].Count;

                            pCount[anchor] = (ymt.Unk_376833625.PropInfo.Props[anchor] ?? new List<MUnk_94549140>()).Count;
                        }

                        foreach (var entry in ComponentFilePrefix)
                        {
                            Unk_884254308 component = entry.Key;
                            string prefix = entry.Value;
                            string targetDirectory = opts.InputDirectory + "\\" + targetName + "\\components\\" + prefix;

                            if (Directory.Exists(targetDirectory))
                            {
                                IEnumerable<string> addonFilesUnordered = Directory.GetFiles(targetDirectory).Where(e => e.EndsWith(".ydd"));

                                int padLen = 0;

                                if(addonFilesUnordered.Count() > 0)
                                    padLen = addonFilesUnordered.Max(e => e.Length);

                                string[] addonFiles = addonFilesUnordered.OrderBy(e => e.PadLeft(padLen, '0')).ToArray();

                                var addons = new List<int>();

                                for (int k = 0; k < addonFiles.Length; k++)
                                {
                                    addons.Add(k);
                                }

                                if (addons.Count > 0)
                                {
                                    // Create addon component entries
                                    var def = ymt.Unk_376833625.Components[component] ?? new MUnk_3538495220();

                                    for (int k = 0; k < addons.Count; k++)
                                    {
                                        int addonPos = def.Unk_1756136273.Count();
                                        string textureDirectory = targetDirectory + "\\" + addons[k];
                                        var addonTextures = new List<int>();
                                        var item = new MUnk_1535046754();
                                        IEnumerable<string> texturesUnordered = Directory.GetFiles(textureDirectory).Where(e => e.EndsWith(".ytd"));
                                        int padLen1 = 0;

                                        if (texturesUnordered.Count() > 0)
                                            padLen1 = texturesUnordered.Max(e => e.Length);

                                        string[] textures = texturesUnordered.OrderBy(e => e.PadLeft(padLen1, '0')).ToArray();
                                        string yddFileName = prefix + "_" + addonPos.ToString().PadLeft(3, '0') + "_u.ydd";

                                        cYddMapping[addonFiles[k]] = new Tuple<string, int, int, int, string, string>(prefix, addons[k], addonPos, addons.Count, targetDirectory, yddFileName);

                                        // Create addon texture entries
                                        for (int l = 0; l < textures.Length; l++)
                                        {
                                            addonTextures.Add(l);
                                        }

                                        cTextureCount[addonFiles[k]] = addonTextures.Count;

                                        for (int l = 0; l < addonTextures.Count; l++)
                                        {
                                            var texture = new MUnk_1036962405();
                                            item.ATexData.Add(texture);

                                            // Create componentinfo
                                            var cInfo = new MCComponentInfo();

                                            cInfo.Unk_2114993291 = 0;
                                            cInfo.Unk_3509540765 = (byte)component;
                                            cInfo.Unk_4196345791 = (byte)l;

                                            ymt.Unk_376833625.CompInfos.Add(cInfo);
                                        }

                                        if(File.Exists(addonFiles[k].Replace(".ydd", ".yld")))
                                        {
                                            item.ClothData.Unk_2828247905 = 1;
                                        }

                                        def.Unk_1756136273.Add(item);

                                        cCount[component]++;
                                    }

                                    ymt.Unk_376833625.Components[component] = def;
                                }

                            }

                        }

                        foreach (var entry in AnchorFilePrefix)
                        {
                            Unk_2834549053 anchor = entry.Key;
                            string prefix = entry.Value;
                            string targetDirectory = opts.InputDirectory + "\\" + targetName + "_p" + "\\props\\" + prefix;

                            if (Directory.Exists(targetDirectory))
                            {
                                IEnumerable<string> addonFilesUnordered = Directory.GetFiles(targetDirectory).Where(e => e.EndsWith(".ydd"));

                                int padLen = 0;

                                if (addonFilesUnordered.Count() > 0)
                                    padLen = addonFilesUnordered.Max(e => e.Length);

                                string[] addonFiles = addonFilesUnordered.OrderBy(e => e.PadLeft(padLen, '0')).ToArray();

                                var addons = new List<int>();

                                for (int k = 0; k < addonFiles.Length; k++)
                                {
                                    addons.Add(k);
                                }

                                if (addons.Count > 0)
                                {
                                    // Create addon prop entries
                                    var defs = ymt.Unk_376833625.PropInfo.Props[anchor] ?? new List<MUnk_94549140>();

                                    for (int k = 0; k < addons.Count; k++)
                                    {
                                        int addonPos = defs.Count();
                                        string textureDirectory = targetDirectory + "\\" + addons[k];
                                        var addonTextures = new List<int>();
                                        var item = new MUnk_94549140(ymt.Unk_376833625.PropInfo);
                                        IEnumerable<string> texturesUnordered = Directory.GetFiles(textureDirectory).Where(e => e.EndsWith(".ytd"));
                                        int padLen2 = 0;

                                        if (texturesUnordered.Count() > 0)
                                            padLen2 = texturesUnordered.Max(e => e.Length);

                                        string[] textures = texturesUnordered.OrderBy(e => e.PadLeft(padLen2, '0')).ToArray();
                                        string yddFileName = "p_" + prefix + "_" + addonPos.ToString().PadLeft(3, '0') + ".ydd";

                                        item.AnchorId = (byte)anchor;

                                        pYddMapping[addonFiles[k]] = new Tuple<string, int, int, int, string, string>(prefix, addons[k], addonPos, addons.Count, targetDirectory, yddFileName);

                                        // Create addon texture entries
                                        for (int l = 0; l < textures.Length; l++)
                                        {
                                            addonTextures.Add(l);
                                        }

                                        pTextureCount[addonFiles[k]] = addonTextures.Count;

                                        for (int l = 0; l < addonTextures.Count; l++)
                                        {
                                            var texture = new MUnk_254518642();
                                            item.TexData.Add(texture);
                                        }

                                        // Get or create linked anchor
                                        var aanchor = ymt.Unk_376833625.PropInfo.AAnchors.Find(e => e.Anchor == anchor);

                                        if (aanchor == null)
                                        {
                                            aanchor = new MCAnchorProps(ymt.Unk_376833625.PropInfo);
                                            aanchor.PropsMap[item] = (byte) item.TexData.Count;
                                            ymt.Unk_376833625.PropInfo.AAnchors.Add(aanchor);
                                        }
                                        else
                                        {
                                            aanchor.PropsMap[item] = (byte) item.TexData.Count;
                                        }

                                        defs.Add(item);
                                        pCount[anchor]++;
                                    }

                                    ymt.Unk_376833625.PropInfo.Props[anchor] = defs;
                                }
                            }

                        }

                        // Create reserved component entries
                        foreach (Unk_884254308 component in cValues)
                        {
                            if (component == Unk_884254308.PV_COMP_INVALID || component == Unk_884254308.PV_COMP_MAX)
                                continue;

                            int count = cCount[component];
                            int max = (opts.ReserveEntries > count) ? opts.ReserveEntries : count;
                            var def = ymt.Unk_376833625.Components[component] ?? new MUnk_3538495220();

                            for (int i = count; i < max; i++)
                            {
                                var item = new MUnk_1535046754();
                                var texture = new MUnk_1036962405();
                                item.ATexData.Add(texture);

                                // Create componentinfo
                                var cInfo = new MCComponentInfo();

                                cInfo.Unk_2114993291 = 0;
                                cInfo.Unk_3509540765 = (byte)component;
                                cInfo.Unk_4196345791 = (byte)i;

                                ymt.Unk_376833625.CompInfos.Add(cInfo);

                                def.Unk_1756136273.Add(item);
                            }

                            if (def.Unk_1756136273.Count > 0)
                                ymt.Unk_376833625.Components[component] = def;
                        }

                        // Create reserved prop entries
                        foreach (Unk_2834549053 anchor in pValues)
                        {
                            if (anchor == Unk_2834549053.NUM_ANCHORS)
                                continue;

                            int count = pCount[anchor];
                            int max = (opts.ReservePropEntries > count) ? opts.ReservePropEntries : count;
                            var defs = ymt.Unk_376833625.PropInfo.Props[anchor] ?? new List<MUnk_94549140>();

                            for (int i = count; i < max; i++)
                            {
                                var item = new MUnk_94549140(ymt.Unk_376833625.PropInfo);
                                item.AnchorId = (byte)anchor;
                                var texture = new MUnk_254518642();
                                item.TexData.Add(texture);

                                var aanchor = ymt.Unk_376833625.PropInfo.AAnchors.Find(e => e.Anchor == anchor);

                                if (aanchor == null)
                                {
                                    aanchor = new MCAnchorProps(ymt.Unk_376833625.PropInfo);
                                    aanchor.Anchor = anchor;
                                    aanchor.PropsMap[item] = 1;
                                    ymt.Unk_376833625.PropInfo.AAnchors.Add(aanchor);
                                }
                                else
                                {
                                    aanchor.PropsMap[item] = 1;
                                }


                                defs.Add(item);
                            }

                            if (defs.Count > 0)
                                ymt.Unk_376833625.PropInfo.Props[anchor] = defs;
                        }

                        processedYmts[targetName] = new Tuple<
                            Dictionary<string, Tuple<string, int, int, int, string, string>>,
                            Dictionary<string, int>,
                            Dictionary<string, Tuple<string, int, int, int, string, string>>,
                            Dictionary<string, int>
                        >(cYddMapping, cTextureCount, pYddMapping, pTextureCount);

                        if (opts.FiveMFormat)
                        {
                            ymt.Save(opts.OutputDirectory + "\\stream\\" + targetName + ".ymt");

                            // var xml2 = MetaXml.GetXml(ymt.ResourceFile.ResourceData);
                            // File.WriteAllText(opts.OutputDirectory + "\\stream\\" + targetMetaYmtFileName + ".xml", xml2);
                        }
                        else
                        {
                            ymt.Save(opts.OutputDirectory + "\\x64\\models\\cdimages\\streamedpeds_mp.rpf\\" + targetName + ".ymt");
                        }

                        dynamic overrideInfos = new JObject();

                        overrideInfos["components"] = new JObject();
                        overrideInfos["props"] = new JObject();

                        foreach (Unk_884254308 component in cValues)
                        {
                            if (component == Unk_884254308.PV_COMP_INVALID || component == Unk_884254308.PV_COMP_MAX)
                                continue;

                            int count = ymt.Unk_376833625.Components[component]?.Unk_1756136273.Count ?? 0;
                            int max = (opts.ReserveEntries > count) ? opts.ReserveEntries : count;

                            overrideInfos["components"][ComponentFilePrefix[component]] = new JObject() { ["start"] = cCount[component], ["end"] = max };
                        }

                        foreach (Unk_2834549053 anchor in pValues)
                        {
                            if (anchor == Unk_2834549053.NUM_ANCHORS)
                                continue;

                            int count = ymt.Unk_376833625.PropInfo.Props[anchor]?.Count ?? 0;
                            int max = (opts.ReservePropEntries > count) ? opts.ReservePropEntries : count;

                            overrideInfos["props"][AnchorFilePrefix[anchor]] = new JObject() { ["start"] = pCount[anchor], ["end"] = max };
                        }

                        var jsonString = JsonConvert.SerializeObject(overrideInfos, new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.Indented });

                        File.WriteAllText(opts.OutputDirectory + "\\" + targetName + ".override.json", jsonString);
                    }

                    // Check which directories contains addon component / props
                    for (int i=0; i<dirs.Length; i++)
                    {
                        bool found = false;

                        foreach(var entry in ComponentFilePrefix)
                        {
                            string prefix = entry.Value;


                            if (Directory.Exists(dirs[i] + "\\components\\" + prefix) && Directory.GetFiles(dirs[i] + "\\components\\" + prefix).Where(e => e.EndsWith(".ydd")).Count() > 0)
                            {
                                found = true;
                                break;
                            }

                            if (Directory.Exists(dirs[i] + "\\props\\" + prefix) && Directory.GetFiles(dirs[i] + "\\props\\" + prefix).Where(e => e.EndsWith(".ydd")).Count() > 0)
                            {
                                found = true;
                                break;
                            }

                            if (found)
                                break;

                        }

                        if (found)
                            addonDirs.Add(dirs[i]);
                    }

                    for (int i = 0; i < addonDirs.Count; i++)
                    {
                        Console.WriteLine(addonDirs[i]);

                        string directory = addonDirs[i];
                        string[] path = directory.Split('\\');
                        string name = path[path.Length - 1];
                        string ymtDirName = name;

                        if (ymtDirName.EndsWith("_p"))
                            ymtDirName = ymtDirName.Substring(0, ymtDirName.Length - 2);

                        Tuple<
                            Dictionary<string, Tuple<string, int, int, int, string, string>>,
                            Dictionary<string, int>,
                            Dictionary<string, Tuple<string, int, int, int, string, string>>,
                            Dictionary<string, int>
                        > processedYmtData = null;

                        // Copy models / textures with resolved names to build directory
                        if(processedYmts.TryGetValue(ymtDirName, out processedYmtData))
                        {
                            foreach (var entry in processedYmtData.Item1)
                            {
                                if (opts.FiveMFormat)
                                    GenPedDefs_CreateComponentFiles_FiveM(opts, ymtDirName, entry, processedYmtData.Item2[entry.Key]);
                                else
                                    GenPedDefs_CreateComponentFiles(opts, ymtDirName, entry, processedYmtData.Item2[entry.Key]);
                            }

                            foreach (var entry in processedYmtData.Item3)
                            {
                                if (opts.FiveMFormat)
                                    GenPedDefs_CreatePropFiles_FiveM(opts, ymtDirName, entry, processedYmtData.Item4[entry.Key]);
                                else
                                    GenPedDefs_CreatePropFiles(opts, ymtDirName, entry, processedYmtData.Item4[entry.Key]);
                            }
                        }

                    }

                }

            });
        }

        public static void GenPedDefs_CreateComponentFiles(GenPedDefsOptions opts, string targetDirName, KeyValuePair<string, Tuple<string, int, int, int, string, string>> entry, int textureCount)
        {
            string sourceYddFile = entry.Key;
            string sourceYldFile = sourceYddFile.Replace(".ydd", ".yld");
            string prefix = entry.Value.Item1;
            int origPos = entry.Value.Item2;
            int pos = entry.Value.Item3;
            int count = entry.Value.Item4;
            string folder = entry.Value.Item5;
            string yddFileName = entry.Value.Item6;
            string directory = opts.OutputDirectory + "\\x64\\models\\cdimages\\streamedpeds_mp.rpf\\" + targetDirName;

            Directory.CreateDirectory(directory);

            string targetYddFile = directory + "\\" + yddFileName;

            File.Copy(sourceYddFile, targetYddFile, true);

            if(File.Exists(sourceYldFile))
            {
                string targetYldFile = targetYddFile.Replace(".ydd", ".yld");
                File.Copy(sourceYldFile, targetYldFile, true);
            }

            int texCount = 0;

            for (int k = pos; k < textureCount + pos; k++)
            {
                char c = 'a';

                for (int l = 0; l < texCount; l++)
                    c++;

                string sourceYtdPath = folder + "\\" + origPos + "\\" + texCount + ".ytd";
                string targetYtdFile = prefix + "_diff_" + pos.ToString().PadLeft(3, '0') + "_" + c + "_uni.ytd";
                string targetYtdPath = directory + "\\" + targetYtdFile;

                File.Copy(sourceYtdPath, targetYtdPath, true);

                texCount++;

            }
        }

        public static void GenPedDefs_CreatePropFiles(GenPedDefsOptions opts, string targetDirName, KeyValuePair<string, Tuple<string, int, int, int, string, string>> entry, int textureCount)
        {
            string sourceYddFile = entry.Key;
            string prefix = entry.Value.Item1;
            int origPos = entry.Value.Item2;
            int pos = entry.Value.Item3;
            int count = entry.Value.Item4;
            string folder = entry.Value.Item5;
            string yddFileName = entry.Value.Item6;
            string directory = opts.OutputDirectory + "\\x64\\models\\cdimages\\streamedpedprops.rpf\\" + targetDirName + "_p";

            Directory.CreateDirectory(directory);

            string targetYddFile = directory + "\\" + yddFileName;

            File.Copy(sourceYddFile, targetYddFile, true);

            int texCount = 0;

            for (int k = pos; k < textureCount + pos; k++)
            {
                char c = 'a';

                for (int l = 0; l < texCount; l++)
                    c++;

                string sourceYtdPath = folder + "\\" + origPos + "\\" + texCount + ".ytd";
                string targetYtdFile = "p_" + prefix + "_diff_" + pos.ToString().PadLeft(3, '0') + "_" + c + ".ytd";
                string targetYtdPath = directory + "\\" + targetYtdFile;

                File.Copy(sourceYtdPath, targetYtdPath, true);

                texCount++;

            }
        }


        public static void GenPedDefs_CreateComponentFiles_FiveM(GenPedDefsOptions opts, string targetFileName, KeyValuePair<string, Tuple<string, int, int, int, string, string>> entry, int textureCount)
        {
            string sourceYddFile = entry.Key;
            string sourceYldFile = sourceYddFile.Replace(".ydd", ".yld");
            string prefix = entry.Value.Item1;
            int origPos = entry.Value.Item2;
            int pos = entry.Value.Item3;
            int count = entry.Value.Item4;
            string folder = entry.Value.Item5;
            string yddFileName = entry.Value.Item6;

            string targetYddFile = opts.OutputDirectory + "\\stream\\" + targetFileName + "^" + yddFileName;

            File.Copy(sourceYddFile, targetYddFile, true);

            if (File.Exists(sourceYldFile))
            {
                string targetYldFile = targetYddFile.Replace(".ydd", ".yld");
                File.Copy(sourceYldFile, targetYldFile, true);
            }
            int texCount = 0;

            for (int k = pos; k < textureCount + pos; k++)
            {
                char c = 'a';

                for (int l = 0; l < texCount; l++)
                    c++;

                string sourceYtdPath = folder + "\\" + origPos + "\\" + texCount + ".ytd";
                string targetYtdFile = prefix + "_diff_" + pos.ToString().PadLeft(3, '0') + "_" + c + "_uni.ytd";
                string targetYtdPath = opts.OutputDirectory + "\\stream\\" + targetFileName + "^" + targetYtdFile;

                File.Copy(sourceYtdPath, targetYtdPath, true);

                texCount++;

            }
        }

        public static void GenPedDefs_CreatePropFiles_FiveM(GenPedDefsOptions opts, string targetFileName, KeyValuePair<string, Tuple<string, int, int, int, string, string>> entry, int textureCount)
        {
            string sourceYddFile = entry.Key;
            string prefix = entry.Value.Item1;
            int origPos = entry.Value.Item2;
            int pos = entry.Value.Item3;
            int count = entry.Value.Item4;
            string folder = entry.Value.Item5;
            string yddFileName = entry.Value.Item6;

            string targetYddFile = opts.OutputDirectory + "\\stream\\" + targetFileName + "_p^" + yddFileName;

            File.Copy(sourceYddFile, targetYddFile, true);

            int texCount = 0;

            for (int k = pos; k < textureCount + pos; k++)
            {
                char c = 'a';

                for (int l = 0; l < texCount; l++)
                    c++;

                string sourceYtdPath = folder + "\\" + origPos + "\\" + texCount + ".ytd";
                string targetYtdFile = "p_" + prefix + "_diff_" + pos.ToString().PadLeft(3, '0') + "_" + c + ".ytd";
                string targetYtdPath = opts.OutputDirectory + "\\stream\\" + targetFileName + "_p" + "^" + targetYtdFile;

                File.Copy(sourceYtdPath, targetYtdPath, true);

                texCount++;

            }
        }

        public static Dictionary<Unk_884254308, string> ComponentFilePrefix = new Dictionary<Unk_884254308, string>()
        {
            { Unk_884254308.PV_COMP_HEAD, "head" },
            { Unk_884254308.PV_COMP_BERD, "berd" },
            { Unk_884254308.PV_COMP_HAIR, "hair" },
            { Unk_884254308.PV_COMP_UPPR, "uppr" },
            { Unk_884254308.PV_COMP_LOWR, "lowr" },
            { Unk_884254308.PV_COMP_HAND, "hand" },
            { Unk_884254308.PV_COMP_FEET, "feet" },
            { Unk_884254308.PV_COMP_TEEF, "teef" },
            { Unk_884254308.PV_COMP_ACCS, "accs" },
            { Unk_884254308.PV_COMP_TASK, "task" },
            { Unk_884254308.PV_COMP_DECL, "decl" },
            { Unk_884254308.PV_COMP_JBIB, "jbib" },
        };


        public static Dictionary<Unk_2834549053, string> AnchorFilePrefix = new Dictionary<Unk_2834549053, string>()
        {
            { Unk_2834549053.ANCHOR_HEAD,        "head" },
            { Unk_2834549053.ANCHOR_EYES,        "eyes" },
            { Unk_2834549053.ANCHOR_EARS,        "ears" },
            { Unk_2834549053.ANCHOR_MOUTH,       "mouth" },
            { Unk_2834549053.ANCHOR_LEFT_HAND,   "lhand" },
            { Unk_2834549053.ANCHOR_RIGHT_HAND,  "rhand" },
            { Unk_2834549053.ANCHOR_LEFT_WRIST,  "lwrist" },
            { Unk_2834549053.ANCHOR_RIGHT_WRIST, "rwrist" },
            { Unk_2834549053.ANCHOR_HIP,         "hip" },
            { Unk_2834549053.ANCHOR_LEFT_FOOT,   "lfoot" },
            { Unk_2834549053.ANCHOR_RIGHT_FOOT,  "rfoot" },
            { Unk_2834549053.Unk_604819740,      "unk604819740" },
            { Unk_2834549053.Unk_2358626934,     "unk2358626934" },
        };

    }
}
