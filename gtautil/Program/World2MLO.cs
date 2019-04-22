
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.GTA5.Utilities;
using RageLib.Resources.GTA5.PC.GameFiles;
using SharpDX;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleWorldToMLOOptions(string[] args)
        {
            CommandLine.Parse<WorldToMLOOptions>(args, (opts, gOpts) =>
            {
                if (opts.MLOPosition == null)
                {
                    return;
                }

                if (opts.MLORotation == null)
                {
                    return;
                }

                if (opts.Position == null)
                {
                    return;
                }

                var c = CultureInfo.InvariantCulture;

                var mloPos = new Vector3(opts.MLOPosition[0], opts.MLOPosition[1], opts.MLOPosition[2]);
                var mloRot = new Quaternion(opts.MLORotation[0], opts.MLORotation[1], opts.MLORotation[2], opts.MLORotation[3]);
                var pos = new Vector3(opts.Position[0], opts.Position[1], opts.Position[2]);

                var placement = Utils.World2Mlo(pos, Vector4.Zero, mloPos, mloRot);

                Console.WriteLine(placement.Item1.X.ToString(c) + "," + placement.Item1.Y.ToString(c) + "," + placement.Item1.Z.ToString(c));
            });
        }
    }
}
