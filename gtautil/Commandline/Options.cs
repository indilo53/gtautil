using System.Collections.Generic;

namespace GTAUtil
{
    [Verb("genpropdefs")]
    public class GenPropDefsOptions
    {
        [Option('i', "input")]
        public List<string> InputFiles { get; set; }
        
        [Option('d', "directory")]
        public string Directory { get; set; }
    }

    [Verb("importmeta")]
    public class ImportMetaOptions
    {
        [Option('i', "input")]
        public List<string> InputFiles { get; set; }
        
        [Option('d', "directory")]
        public string Directory { get; set; }
    }

    [Verb("exportmeta")]
    public class ExportMetaOptions
    {
        [Option('i', "input")]
        public List<string> InputFiles { get; set; }

        [Option('d', "directory")]
        public string Directory { get; set; }
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

        [Option('r', "room")]
        public string Room { get; set; }

        [Option('p', "position")]
        public List<float> Position { get; set; }

        [Option('r', "rotation")]
        public List<float> Rotation { get; set; }
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
