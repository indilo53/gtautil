using CodeWalker.GameFiles;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GTAUtil
{
    public static class Utils
    {
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

    }

}
