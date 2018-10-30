using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpDX;
using RageLib.Hash;
using RageLib.Resources.GTA5.PC.Meta;
using RageLib.GTA5.ResourceWrappers.PC.Meta.Structures;
using RageLib.Archives;
using RageLib.GTA5.Archives;
using RageLib.Resources.GTA5;
using RageLib.Resources;
using RageLib.GTA5.Cryptography;
using System.IO.Compression;
using RageLib.GTA5.Utilities;
using RageLib.GTA5.ArchiveWrappers;
using RageLib.Resources.GTA5.PC.GameFiles;

namespace GTAUtil
{
    public static class Utils
    {
        /* Generic Utils */
        public static FileSystemInfo[] Expand(string pattern)
        {
            return Glob.Glob.Expand(pattern.Replace('/', '\\')).ToArray();
        }

        public static FileSystemInfo[] Expand(string[] patterns)
        {
            var files = new List<FileSystemInfo>();

            for (int i = 0; i < patterns.Length; i++)
            {
                files.AddRange(Expand(patterns[i]));
            }

            return files.ToArray();
        }

        public static FileSystemInfo[] Expand(IEnumerable<string> patterns)
        {
            return Expand(patterns.ToArray());
        }

        // Get full extension like .ytyp.xml
        public static string GetFullExtension(string fileName)
        {
            string[] parts = fileName.Split('\\').Last().Split('.');
            var sb = new StringBuilder();

            for (int j = parts.Length - 1; j >= 1; j--)
            {
                if (j < parts.Length - 1)
                {
                    sb.Insert(0, '.');
                }

                sb.Insert(0, parts[j]);
            }

            return sb.ToString();
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            if (filespec == folder)
            {
                return ".";
            }

            Uri pathUri = new Uri(filespec);

            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }

            Uri folderUri = new Uri(folder);

            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        // Helper method to not overwrite file if already exists
        public static string GetUniqueFileName(string fileName)
        {
            var info = new FileInfo(fileName);
            string directory = Path.GetDirectoryName(fileName);
            string name = Path.GetFileNameWithoutExtension(info.Name).Split('.').First();
            string fullExt = Utils.GetFullExtension(info.Name);

            if (File.Exists(fileName))
            {
                int j = 1;

                while (File.Exists(directory + "\\" + name + " (" + j + ")." + fullExt))
                {
                    j++;
                }

                fileName = directory + "\\" + name + " (" + j + ")." + fullExt;
            }

            return fileName;
        }

        public static byte[] GetFileData(FileSystemInfo fsInfo)
        {
            //if(fsInfo is RpfFileInfo)
            //{

            //}
            //else if(fsInfo is FileInfo)
            //{
                return File.ReadAllBytes(fsInfo.FullName);
            //}

            //return null;
        }

        public static byte[] GetBinaryFileData(IArchiveBinaryFile file, RageArchiveEncryption7 Encryption)
        {
            using (var ms = new MemoryStream())
            {
                file.Export(ms);

                byte[] data = ms.ToArray();

                if (Encryption == RageArchiveEncryption7.NG)
                {
                    data = GTA5Crypto.DecryptNG(data, file.Name, (uint)file.UncompressedSize);
                }
                else if (Encryption == RageArchiveEncryption7.AES)
                {
                    data = GTA5Crypto.DecryptAES(data);
                }

                if (file.IsCompressed)
                {
                    using (var dfls = new DeflateStream(new MemoryStream(data), CompressionMode.Decompress))
                    {
                        using (var outstr = new MemoryStream())
                        {
                            dfls.CopyTo(outstr);
                            data = outstr.ToArray();
                        }
                    }
                }

                return data;
            }
        }

        public static T GetResourceData<T>(IArchiveResourceFile file) where T : FileBase64_GTA5_pc, new()
        {
            var resource = new ResourceFile_GTA5_pc<T>();

            using (var ms = new MemoryStream())
            {
                file.Export(ms);
                resource.Load(ms);
            }

            return resource.ResourceData;
        }

        public static void ForFile(string fullFileName, Action<IArchiveFile, RageArchiveEncryption7> cb)
        {
            string[] split = fullFileName.Replace('/', '\\').Split(new string[] { ".rpf" }, StringSplitOptions.None);

            for (int i = 0; i < split.Length - 1; i++)
                split[i] = split[i] + ".rpf";

            var baseRpf = Settings.Default.GTAFolder + "\\" + split[0];

            try
            {
                var fileInfo = new FileInfo(baseRpf);
                var fileStream = new FileStream(baseRpf, FileMode.Open);

                var inputArchive = RageArchiveWrapper7.Open(fileStream, fileInfo.Name);

                ArchiveUtilities.ForEachFile(split[0], inputArchive.Root, inputArchive.archive_.Encryption, (string currFullFileName, IArchiveFile file, RageArchiveEncryption7 encryption) =>
                {
                    if (currFullFileName == fullFileName)
                    {
                        cb(file, encryption);
                    }
                });

                inputArchive.Dispose();
            }
            catch (Exception e)
            {

            }
        }

        public static string HashString(MetaName h)
        {
            if (Enum.IsDefined(typeof(MetaName), h))
            {
                return h.ToString();
            }

            uint uh = (uint)h;
            if (uh == 0) return "";

            var str = Jenkins.TryGetString(uh);
            if (!string.IsNullOrEmpty(str)) return str;

            return "hash_" + uh.ToString("X").PadLeft(8, '0');

        }

        public static Vector3 Multiply(Quaternion rotation, Vector3 point)
        {
            float q0 = rotation.W;
            float q0Square = rotation.W * rotation.W;
            Vector3 q = new Vector3(rotation.X, rotation.Y, rotation.Z);
            return ((q0Square - q.LengthSquared()) * point) + (2 * Vector3.Dot(q, point) * q) + (2 * q0 * Vector3.Cross(q, point));
        }

        public static Vector3 RotateTransform(Quaternion rotation, Vector3 point, Vector3 center)
        {
            Vector3 PointNewCenter = Vector3.Subtract(point, center);
            Vector3 TransformedPoint = Multiply(rotation, PointNewCenter);
            return Vector3.Add(TransformedPoint, center);
        }

        public static Vector3 GetCentroid(Vector3[] points)
        {
            var sum = new Vector3(0f, 0f, 0f);

            for(int i=0; i<points.Length; i++)
            {
                sum.X += points[i].X;
                sum.Y += points[i].Y;
                sum.Z += points[i].Z;
            }

            return new Vector3(sum.X / points.Length, sum.Y / points.Length, sum.Z / points.Length);
        }

        /* Specific Utils */
        public static void World2Mlo(MCEntityDef entity, MCMloArchetypeDef mlo, Vector3 mloWorldPosition, Quaternion mloWorldRotation)
        {
            var objRot = new Quaternion(entity.Rotation.X, entity.Rotation.Y, entity.Rotation.Z, entity.Rotation.W);
            var rotationDiff = objRot * mloWorldRotation;    // Multiply initial entity rotation by mlo rotation 

            entity.Position -= mloWorldPosition;    // Substract mlo world coords from entity world coords
            entity.Position = Utils.RotateTransform(Quaternion.Invert(mloWorldRotation), entity.Position, Vector3.Zero);   // Rotate entity around center of mlo instance (mlo entities rotations in space are inverted)

            entity.Rotation = new Vector4(rotationDiff.X, rotationDiff.Y, rotationDiff.Z, rotationDiff.W);
        }

        public static Vector3 World2Mlo(Vector3 position, MCMloArchetypeDef mlo, Vector3 mloWorldPosition, Quaternion mloWorldRotation)
        {
            var newPosition = new Vector3(position.X, position.Y, position.Z);

            newPosition -= mloWorldPosition;
            newPosition = Utils.RotateTransform(Quaternion.Invert(mloWorldRotation), newPosition, Vector3.Zero);

            return newPosition;
        }

        public static void Mlo2World(MCEntityDef entity, MCMloArchetypeDef mlo, Vector3 mloWorldPosition, Quaternion mloWorldRotation)
        {
            var objRot = new Quaternion(entity.Rotation.X, entity.Rotation.Y, entity.Rotation.Z, entity.Rotation.W);

            entity.Position = Utils.RotateTransform(mloWorldRotation, entity.Position, Vector3.Zero);
            entity.Position += mloWorldPosition;

            var rotationDiff = objRot * Quaternion.Invert(mloWorldRotation);
            entity.Rotation = new Vector4(rotationDiff.X, rotationDiff.Y, rotationDiff.Z, rotationDiff.W);
        }

        public static Vector3 Mlo2World(Vector3 position, MCMloArchetypeDef mlo, Vector3 mloWorldPosition, Quaternion mloWorldRotation)
        {
            var newPosition = new Vector3(position.X, position.Y, position.Z);

            newPosition -= Utils.RotateTransform(mloWorldRotation, position, Vector3.Zero);
            newPosition += mloWorldPosition;

            return newPosition;
        }

        public static Vector3[][] CalcExtents(List<MCEntityDef> entities)
        {
            Vector3 emin = new Vector3(float.MaxValue);
            Vector3 emax = new Vector3(float.MinValue);
            Vector3 smin = new Vector3(float.MaxValue);
            Vector3 smax = new Vector3(float.MinValue);

            Vector3[] c = new Vector3[8];
            Vector3[] s = new Vector3[8];

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                var drawable = Program.GetDrawable(entity.ArchetypeName);

                // Console.WriteLine(Jenkins.GetString(entity.ArchetypeName));

                if(drawable != null)
                {
                    Quaternion orientation = new Quaternion(entity.Rotation);

                    Vector3 dcenter = ((Vector3)drawable.BoundingCenter) * entity.ScaleXY;
                    Vector3 dbbmin  = ((Vector3)(Vector4)drawable.BoundingBoxMin) * entity.ScaleXY - dcenter;
                    Vector3 dbbmax  = ((Vector3)(Vector4)drawable.BoundingBoxMax) * entity.ScaleXY - dcenter;

                    Vector3 c1 = Utils.RotateTransform(orientation, dbbmin, Vector3.Zero);
                    Vector3 c2 = Utils.RotateTransform(orientation, dbbmax, Vector3.Zero);

                    Vector3 bbmin = Vector3.Min(c1, c2);
                    Vector3 bbmax = Vector3.Max(c1, c2);

                    bbmin += entity.Position;
                    bbmax += entity.Position;

                    var sbmin = bbmin - entity.LodDist;
                    var sbmax = bbmax + entity.LodDist;

                    emin = Vector3.Min(emin, bbmin);
                    emax = Vector3.Max(emax, bbmax);
                    smin = Vector3.Min(smin, sbmin);
                    smax = Vector3.Max(smax, sbmax);
                }

            }
        
            return new Vector3[2][]
            {
                new Vector3[2] { emin, emax },
                new Vector3[2] { smin, smax },
            };
        }

    }

    public static class QuaternionExtension
    {
        public static Vector3 Multiply(this Quaternion a, Vector3 b)
        {
            float axx = a.X * 2.0f;
            float ayy = a.Y * 2.0f;
            float azz = a.Z * 2.0f;
            float awxx = a.W * axx;
            float awyy = a.W * ayy;
            float awzz = a.W * azz;
            float axxx = a.X * axx;
            float axyy = a.X * ayy;
            float axzz = a.X * azz;
            float ayyy = a.Y * ayy;
            float ayzz = a.Y * azz;
            float azzz = a.Z * azz;
            return new Vector3(((b.X * ((1.0f - ayyy) - azzz)) + (b.Y * (axyy - awzz))) + (b.Z * (axzz + awyy)),
                        ((b.X * (axyy + awzz)) + (b.Y * ((1.0f - axxx) - azzz))) + (b.Z * (ayzz - awxx)),
                        ((b.X * (axzz - awyy)) + (b.Y * (ayzz + awxx))) + (b.Z * ((1.0f - axxx) - ayyy)));
        }

        public static Matrix ToMatrix(this Quaternion q)
        {
            float xx = q.X * q.X;
            float yy = q.Y * q.Y;
            float zz = q.Z * q.Z;
            float xy = q.X * q.Y;
            float zw = q.Z * q.W;
            float zx = q.Z * q.X;
            float yw = q.Y * q.W;
            float yz = q.Y * q.Z;
            float xw = q.X * q.W;
            Matrix result = new Matrix();
            result.M11 = 1.0f - (2.0f * (yy + zz));
            result.M12 = 2.0f * (xy + zw);
            result.M13 = 2.0f * (zx - yw);
            result.M14 = 0.0f;
            result.M21 = 2.0f * (xy - zw);
            result.M22 = 1.0f - (2.0f * (zz + xx));
            result.M23 = 2.0f * (yz + xw);
            result.M24 = 0.0f;
            result.M31 = 2.0f * (zx + yw);
            result.M32 = 2.0f * (yz - xw);
            result.M33 = 1.0f - (2.0f * (yy + xx));
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1.0f;
            return result;
        }

        public static Vector4 ToVector4(this Quaternion q)
        {
            return new Vector4(q.X, q.Y, q.Z, q.W);
        }

    }

}
