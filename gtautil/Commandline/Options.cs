using System.Collections.Generic;

namespace GTAUtil
{
    [Verb("generic")]
    public class GenericOptions
    {
        [Option('d', "dlclevel", HelpText = "Max DLC level")]
        public string DlcLevel { get; set; }

        [Option('m', "mods", HelpText = "Additionnal files to load")]
        public List<string> Mods { get; set; }
    }

    [Verb("fixarchive")]
    public class FixArchiveOptions
    {
        [Option('i', "input", HelpText = "Input files")]
        public List<string> InputFiles { get; set; }

        [Option('r', "recursive", Default = false, HelpText = "Enable recursive mode")]
        public bool Recursive { get; set; }
    }

    [Verb("createarchive")]
    public class CreateArchiveOptions
    {
        [Option('i', "input", HelpText = "Input folder")]
        public string InputFolder { get; set; }

        [Option('o', "output", HelpText = "Output folder")]
        public string OutputFolder { get; set; }

        [Option('n', "name", HelpText = "RPF name")]
        public string Name { get; set; }
    }

    [Verb("extractarchive")]
    public class ExtractArchiveOptions
    {
        [Option('i', "input", HelpText = "Input archive")]
        public string InputFile { get; set; }

        [Option('o', "output", HelpText = "Output folder")]
        public string OutputFolder { get; set; }
    }

    [Verb("worldtomlo")]
    public class WorldToMLOOptions
    {
        [Option('p', "mloposition", HelpText = "MLO world position")]
        public List<float> MLOPosition { get; set; }

        [Option('r', "mlorotation", HelpText = "MLO world rotation")]
        public List<float> MLORotation { get; set; }

        [Option('n', "position", HelpText = "World position")]
        public List<float> Position { get; set; }
    }

    [Verb("genpropdefs")]
    public class GenPropDefsOptions
    {
        [Option('i', "input", HelpText = "Input files")]
        public List<string> InputFiles { get; set; }
        
        [Option('o', "output", HelpText = "Output directory")]
        public string OutputDirectory { get; set; }

        [Option('y', "ytyp", HelpText = "Ytyps containing entity informations")]
        public List<string> Ytyp { get; set; }
    }

    [Verb("importmeta")]
    public class ImportMetaOptions
    {
        [Option('i', "input", HelpText = "Input files")]
        public List<string> InputFiles { get; set; }

        [Option("metadata", Default = false, HelpText = "Generate metadata")]
        public bool Metadata { get; set; }

        [Option("delete", HelpText = "Entities to delete")]
        public List<string> Delete { get; set; }

        [Option("deletemode", Default = "delete", HelpText = "Delete mode [delete|dummy]")]
        public string DeleteMode { get; set; }

        [Option("deletescope", Default = "full", HelpText = "Delete mode [full|children]")]
        public string DeleteScope { get; set; }
    }

    [Verb("exportmeta")]
    public class ExportMetaOptions
    {
        [Option('i', "input", HelpText = "Input files")]
        public List<string> InputFiles { get; set; }

        [Option("metadata", Default = false, HelpText = "Parse metadata")]
        public bool Metadata { get; set; }
    }

    [Verb("injectentities")]
    public class InjectEntitiesOptions
    {
        [Option('n', "name", HelpText = "Generated ytyp name")]
        public string Name { get; set; }

        [Option('y', "ymap", HelpText = "Source ymap")]
        public string Ymap { get; set; }

        [Option('z', "ytyp", HelpText = "Source ytyp")]
        public string Ytyp { get; set; }

        [Option('p', "position", HelpText = "Ytyp world positon defined in parent ymap")]
        public List<float> Position { get; set; }

        [Option('r', "rotation", HelpText = "Ytyp world rotation defined in parent ymap")]
        public List<float> Rotation { get; set; }

        [Option('x', "deletemissing", Default = false, HelpText = "Delete enities which are not in the source ymap for the generated ytyp")]
        public bool DeleteMissing { get; set; }

        [Option('s', "static", Default = false, HelpText = "Make all entities static")]
        public bool Static { get; set; }

        [Option('m', "mloname", HelpText = "MLO name if there is multiple MLOs in the source ytyp")]
        public string MloName { get; set; }
    }

    [Verb("extractentities")]
    public class ExtractEntitiesOptions
    {
        [Option('n', "name", HelpText = "Output directory name")]
        public string Name { get; set; }

        [Option('z', "ytyp", HelpText = "Source ytyp")]
        public string Ytyp { get; set; }

        [Option('p', "position", HelpText = "Ytyp world positon defined in parent ymap")]
        public List<float> Position { get; set; }

        [Option('r', "rotation", HelpText = "Ytyp world rotation defined in parent ymap")]
        public List<float> Rotation { get; set; }

        [Option('m', "mloname", HelpText = "MLO name if there is multiple MLOs in the source ytyp")]
        public string MloName { get; set; }
    }

    [Verb("genpeddefs")]
    public class GenPedDefsOptions
    {
        [Option('i', "input", HelpText = "Input directory")]
        public string InputDirectory { get; set; }

        [Option('o', "output", HelpText = "Output directory")]
        public string OutputDirectory { get; set; }

        [Option('f', "fivem", Default = false, HelpText = "Export to FiveM format")]
        public bool FiveMFormat { get; set; }

        [Option('c', "create", Default = false, HelpText = "Create project")]
        public bool CreateMode { get; set; }

        [Option('r', "reserve", Default = 0, HelpText = "Number of component slots to reserve per component type")]
        public int ReserveEntries { get; set; }

        [Option('s', "reserveprops", Default = 0, HelpText = "Number of prop slots to reserve per prop type")]
        public int ReservePropEntries { get; set; }

        [Option('d', "dlclevel", HelpText = "Max DLC Level")]
        public string DLCLevel { get; set; }

        [Option('t', "targets", HelpText = "Targets. Example: mp_m_freemode_01,mp_f_freemode_01")]
        public List<string> Targets { get; set; }
    }

    [Verb("compilegxt2")]
    public class CompileGxt2Optionns
    {
        [Option('o', "output", HelpText = "Output directory")]
        public string OutputDirectory { get; set; }

        [Option('l', "lang", Default = "american")]
        public string Lang { get; set; }
    }

    [Verb("mergeymap")]
    public class MergeYmapOptions
    {
        [Option('o', "output", HelpText = "Output directory")]
        public string OutputDirectory { get; set; }

        [Option('n', "name", HelpText = "Generated ymap name")]
        public string Name { get; set; }

        [Option('y', "ymap", HelpText = "Source ymaps")]
        public string Ymap { get; set; }
    }

    [Verb("moveymap")]
    public class MoveYmapOptions
    {
        [Option('o', "output", HelpText = "Output directory")]
        public string OutputDirectory { get; set; }

        [Option('n', "name", HelpText = "Generated ymap name")]
        public string Name { get; set; }

        [Option('y', "ymap", HelpText = "Source ymap")]
        public string Ymap { get; set; }

        [Option('p', "position", HelpText = "Position offset")]
        public List<float> Position { get; set; }

        [Option('r', "rotation", HelpText = "Rotation offset")]
        public List<float> Rotation { get; set; }
    }

    [Verb("gencol")]
    public class GenColOptions
    {
        [Option('i', "input", HelpText = "Input file")]
        public string InputFile { get; set; }

        [Option('o', "output", HelpText = "Output file")]
        public string OutputFile { get; set; }

        [Option('s', "smooth", Default = 0, HelpText = "Number of passes to smooth the mesh")]
        public int Smooth { get; set; }

        [Option('t', "triangles", Default = -1, HelpText = "Max triangle count in generated collision")]
        public int TriangleCount { get; set; }

        [Option('q', "quantum", Default = 512, HelpText = "Quantum multiplier => (bbMax - bbMin) / (2 ^ multiplier)")]
        public int Qantum { get; set; }

        [Option('m', "mode", Default = "copy")]
        public string Mode { get; set; }

    }

    [Verb("ymaptoydr")]
    public class YmapToYdrOptions
    {
        [Option('i', "input", HelpText = "Input file")]
        public string InputFile { get; set; }

        [Option('o', "output", HelpText = "Output file")]
        public string OutputFile { get; set; }
    }

    [Verb("compiledrawableshaders")]
    public class CompileDrawableShadersOptions
    {

    }

    [Verb("daemon")]
    public class DaemonOptions
    {
        [Option('p', "port", Default = 1337, HelpText = "Input file")]
        public int Port { get; set; }
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
        [Option('p', "position", HelpText = "Position")]
        public List<float> Position { get; set; }
    }

    [Verb("buildcache")]
    public class BuildCacheOptions
    {

    }

}
