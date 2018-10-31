using System;
using System.Globalization;
using System.Linq;
using System.Xml;
using RageLib.GTA5.ResourceWrappers.PC.Meta;
using RageLib.Hash;
using RageLib.Resources.GTA5;
using RageLib.Resources.GTA5.PC.Meta;
using SharpDX;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleFindOptions(string[] args)
        {
            CommandLine.Parse<FindOptions>(args, (opts, gOpts) =>
            {
                if (opts.Position == null || opts.Position.Count != 3)
                {
                    Console.Error.WriteLine("Please specify position with -p --position");
                    return;
                }

                Init(args);

                if (Cache == null)
                {
                    Console.Error.WriteLine("Please build cache first with buildcache");
                    return;
                }

                var c = CultureInfo.InvariantCulture;

                for (int i = 0; i < Cache["ymap"].Count; i++)
                {
                    var cYmap = Cache["ymap"][i];

                    var entitiesExtentsMin = new Vector3((float)cYmap["entitiesExtentsMin"]["x"], (float)cYmap["entitiesExtentsMin"]["y"], (float)cYmap["entitiesExtentsMin"]["z"]);
                    var entitiesExtentsMax = new Vector3((float)cYmap["entitiesExtentsMax"]["x"], (float)cYmap["entitiesExtentsMax"]["y"], (float)cYmap["entitiesExtentsMax"]["z"]);

                    if (
                        opts.Position[0] >= entitiesExtentsMin.X && opts.Position[0] <= entitiesExtentsMax.X &&
                        opts.Position[1] >= entitiesExtentsMin.Y && opts.Position[1] <= entitiesExtentsMax.Y &&
                        opts.Position[2] >= entitiesExtentsMin.Z && opts.Position[2] <= entitiesExtentsMax.Z
                    )
                    {
                        Console.WriteLine("ymap: " + ((string)cYmap["path"]).Split('\\').Last());

                        for (int j = 0; j < cYmap["mloInstances"].Count; j++)
                        {
                            var cMloInstance = cYmap["mloInstances"][j];
                            var cMloInstanceHash = (uint)cMloInstance["name"];

                            var instancePos = new Vector3((float)cMloInstance["position"]["x"], (float)cMloInstance["position"]["y"], (float)cMloInstance["position"]["z"]);
                            var instanceRot = new Quaternion((float)cMloInstance["rotation"]["x"], (float)cMloInstance["rotation"]["y"], (float)cMloInstance["rotation"]["z"], (float)cMloInstance["rotation"]["w"]);

                            for (int k = 0; k < Cache["ytyp"].Count; k++)
                            {
                                var cYtyp = Cache["ytyp"][k];
                                var cYtypHash = (uint)cYtyp["hash"];

                                for (int l = 0; l < cYtyp["mloArchetypes"].Count; l++)
                                {
                                    var cMloArch = cYtyp["mloArchetypes"][l];
                                    var cMloArchHash = (uint)cMloArch["name"];

                                    if (cMloInstanceHash == cMloArchHash)
                                    {
                                        Console.WriteLine("  ytyp => " + ((string)cYtyp["path"]).Split('\\').Last());
                                        Console.WriteLine("    mlo => " + Jenkins.GetString(cMloArchHash));
                                        Console.WriteLine("    position => " + instancePos.X.ToString(c) + "," + instancePos.Y.ToString(c) + "," + instancePos.Z.ToString(c));
                                        Console.WriteLine("    rotation => " + instanceRot.X.ToString(c) + "," + instanceRot.Y.ToString(c) + "," + instanceRot.Z.ToString(c) + "," + instanceRot.W.ToString(c));

                                        for (int m = 0; m < cMloArch["rooms"].Count; m++)
                                        {
                                            var cMloRoom = cMloArch["rooms"][m];

                                            var roomBbMin = new Vector3((float)cMloRoom["bbMin"]["x"], (float)cMloRoom["bbMin"]["y"], (float)cMloRoom["bbMin"]["z"]);
                                            var roomBbMax = new Vector3((float)cMloRoom["bbMax"]["x"], (float)cMloRoom["bbMax"]["y"], (float)cMloRoom["bbMax"]["z"]);

                                            var roomBbMinWorld = instancePos + roomBbMin;
                                            var roomBbMaxWorld = instancePos + roomBbMax;

                                            roomBbMinWorld = Utils.RotateTransform(Quaternion.Conjugate(instanceRot), roomBbMinWorld, Vector3.Zero);
                                            roomBbMaxWorld = Utils.RotateTransform(Quaternion.Conjugate(instanceRot), roomBbMaxWorld, Vector3.Zero);

                                            if (
                                                opts.Position[0] >= roomBbMinWorld.X && opts.Position[0] <= roomBbMaxWorld.X &&
                                                opts.Position[1] >= roomBbMinWorld.Y && opts.Position[1] <= roomBbMaxWorld.Y &&
                                                opts.Position[2] >= roomBbMinWorld.Z && opts.Position[2] <= roomBbMaxWorld.Z
                                            )
                                            {
                                                Console.WriteLine("      room => " + cMloRoom["name"]);
                                            }
                                        }

                                    }

                                }
                            }
                        }

                        Console.WriteLine("");
                    }
                }

            });
        }
    }
}
