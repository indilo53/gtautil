using System.Collections.Generic;

namespace GTAUtil
{
    [Verb("generic")]
    public class GenericOptions
    {
        [Option('d', "dlclevel")]
        public string DlcLevel { get; set; }

        [Option('m', "mods")]
        public List<string> Mods { get; set; }
    }

    [Verb("fixarchive")]
    public class FixArchiveOptions
    {
        [Option('i', "input")]
        public List<string> InputFiles { get; set; }
    }

    [Verb("worldtomlo")]
    public class WorldToMLOOptions
    {
        [Option('p', "mloposition")]
        public List<float> MLOPosition { get; set; }

        [Option('r', "mlorotation")]
        public List<float> MLORotation { get; set; }

        [Option('n', "position")]
        public List<float> Position { get; set; }
    }

    [Verb("genpropdefs")]
    public class GenPropDefsOptions
    {
        [Option('i', "input")]
        public List<string> InputFiles { get; set; }
        
        [Option('o', "output")]
        public string OutputDirectory { get; set; }
    }

    [Verb("importmeta")]
    public class ImportMetaOptions
    {
        [Option('i', "input")]
        public List<string> InputFiles { get; set; }
    }

    [Verb("exportmeta")]
    public class ExportMetaOptions
    {
        [Option('i', "input")]
        public List<string> InputFiles { get; set; }
    }

    [Verb("injectentities")]
    public class InjectEntitiesOptions
    {
        [Option('n', "name")]
        public string Name { get; set; }

        [Option('y', "ymap")]
        public string Ymap { get; set; }

        [Option('z', "ytyp")]
        public string Ytyp { get; set; }

        [Option('c', "collision")]
        public string Collision { get; set; }

        [Option('p', "position")]
        public List<float> Position { get; set; }

        [Option('r', "rotation")]
        public List<float> Rotation { get; set; }

        [Option('x', "deletemissing", Default = false)]
        public bool DeleteMissing { get; set; }

        [Option('s', "static", Default = false)]
        public bool Static { get; set; }

        [Option('m', "mloname")]
        public string MloName { get; set; }
    }

    [Verb("extractentities")]
    public class ExtractEntitiesOptions
    {
        [Option('n', "name")]
        public string Name { get; set; }

        [Option('z', "ytyp")]
        public string Ytyp { get; set; }

        [Option('p', "position")]
        public List<float> Position { get; set; }

        [Option('r', "rotation")]
        public List<float> Rotation { get; set; }

        [Option('m', "mloname")]
        public string MloName { get; set; }
    }

    [Verb("genpeddefinitions")]
    public class GenPedDefinitionsOptions
    {
        [Option('i', "input")]
        public string InputDirectory { get; set; }

        [Option('o', "output")]
        public string OutputDirectory { get; set; }

        [Option('f', "fivem", Default = false)]
        public bool FiveMFormat { get; set; }

        [Option('c', "create", Default = false)]
        public bool CreateMode { get; set; }

        [Option('r', "reserve", Default = 0)]
        public int ReserveEntries { get; set; }

        [Option('s', "reserveprops", Default = 0)]
        public int ReservePropEntries { get; set; }

        [Option('d', "dlclevel", Default = "")]
        public string DLCLevel { get; set; }

        [Option('t', "targets")]
        public List<string> Targets { get; set; }
    }

    [Verb("compilegxt2")]
    public class CompileGxt2Optionns
    {
        [Option('o', "output")]
        public string OutputDirectory { get; set; }

        [Option('l', "lang", Default = "american")]
        public string Lang { get; set; }
    }

    [Verb("mergeymap")]
    public class MergeYmapOptions
    {
        [Option('o', "output")]
        public string OutputDirectory { get; set; }

        [Option('n', "name")]
        public string Name { get; set; }

        [Option('y', "ymap")]
        public string Ymap { get; set; }
    }

    [Verb("moveymap")]
    public class MoveYmapOptions
    {
        [Option('o', "output")]
        public string OutputDirectory { get; set; }

        [Option('n', "name")]
        public string Name { get; set; }

        [Option('y', "ymap")]
        public string Ymap { get; set; }

        [Option('p', "position")]
        public List<float> Position { get; set; }

        [Option('r', "rotation")]
        public List<float> Rotation { get; set; }
    }

    [Verb("test")]
    public class TestOptions
    {

    }

    [Verb("getdlclist")]
    public class GetDLCListOptions
    {
    }

    [Verb("find")]
    public class FindOptions
    {
        [Option('p', "position")]
        public List<float> Position { get; set; }
    }

    [Verb("buildcache")]
    public class BuildCacheOptions
    {

    }

}
