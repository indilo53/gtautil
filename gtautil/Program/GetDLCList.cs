
using System;
using System.Text.RegularExpressions;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.Utilities;
using RageLib.Resources.GTA5.PC.GameFiles;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleGetDLCListOptions(string[] args)
        {
            CommandLine.Parse<GetDLCListOptions>(args, (opts, gOpts) =>
            {
                Init(args);

                for (int i=0; i<DLCList.Length; i++)
                {
                    Console.WriteLine(DLCList[i]);
                }
            });
        }
    }
}
