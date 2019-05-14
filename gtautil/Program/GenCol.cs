
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MIConvexHull;
using SharpDX;
using g3;
using RageLib.Resources;
using RageLib.Resources.Common;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.Resources.GTA5.PC.Meta;
using RageLib.Resources.GTA5.PC.Bounds;
using RageLib.Resources.GTA5.PC.Drawables;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleGenColOptions(string[] args)
        {
            CommandLine.Parse<GenColOptions>(args, (opts, gOpts) =>
            {
                if (opts.InputFile == null)
                {
                    Console.WriteLine("Please provide input file with -i --input");
                    return;
                }

                if (opts.OutputFile == null)
                {
                    Console.WriteLine("Please provide output file with -o --output");
                    return;
                }

                var inputFileInfos = new FileInfo(opts.InputFile);
                var outputFileInfos = new FileInfo(opts.OutputFile);

                if (!inputFileInfos.Exists)
                {
                    Console.WriteLine("Input file does not exists");
                    return;
                }

                if (inputFileInfos.Extension == ".ydr")
                {
                    var ydr = new YdrFile();

                    ydr.Load(inputFileInfos.FullName);

                    var modelData = GenCol_GetModelData(ydr.Drawable.DrawableModelsX);
                    var bComposite = GenCol_CreateBoundComposite(modelData);

                    DMesh3 mesh;

                    if (opts.Mode == "copy")
                    {
                        mesh = new DMesh3();

                        var triangles = new List<DefaultConvexFace<VertexVector3>>();

                        for (int g = 0; g < modelData.Geometries.Count; g++)
                        {
                            var mGeometry = modelData.Geometries[g];

                            for (int i = 0; i < mGeometry.Indices.Count - 2; i += 3)
                            {
                                var vert1 = mGeometry.Vertices[mGeometry.Indices[i + 0]];
                                var vert2 = mGeometry.Vertices[mGeometry.Indices[i + 1]];
                                var vert3 = mGeometry.Vertices[mGeometry.Indices[i + 2]];

                                var triangle = new DefaultConvexFace<VertexVector3>()
                                {
                                    Vertices = new VertexVector3[]
                                    {
                                        vert1,
                                        vert2,
                                        vert3,
                                    }
                                };

                                triangles.Add(triangle);
                            }

                        }

                        mesh = GenCol_CreateMesh(triangles);
                    }
                    else
                    {
                        var hull = ConvexHull.Create(modelData.Vertices);
                        var hullTriangles = hull.Result.Faces.ToList();

                        mesh = GenCol_CreateMesh(hullTriangles);
                    }

                    GenCol_Reshape(mesh, opts.Smooth, opts.TriangleCount);

                    mesh = GenCol_CleanVertices(mesh);

                    var quantum = (modelData.BbMax - modelData.BbMin) / (2 ^ opts.Qantum);

                    var bGeometry = new BoundGeometry
                    {
                        Type = 4,
                        Vertices = new ResourceSimpleArray<BoundVertex>(),
                        BoundingBoxCenter = (RAGE_Vector3)modelData.BsCenter,
                        BoundingSphereRadius = modelData.BsRadius,
                        BoundingBoxMin = (RAGE_Vector3)modelData.BbMin,
                        BoundingBoxMax = (RAGE_Vector3)modelData.BbMax,
                        CenterGravity = new RAGE_Vector3(0.0f, 0.0f, 0.0f),
                        CenterGeometry = new RAGE_Vector3(0.0f, 0.0f, 0.0f),
                        Margin = 0.04f,
                        Quantum = new RAGE_Vector3(quantum.X, quantum.Y, quantum.Z),
                        Polygons = new ResourceSimpleArray<BoundPolygon>(),
                        Materials = new ResourceSimpleArray<BoundMaterial>(),
                        MaterialColours = new ResourceSimpleArray<uint_r>(),
                        PolygonMaterialIndices = new ResourceSimpleArray<byte_r>(),
                        Unknown_78h_Data = new ResourceSimpleArray<BoundVertex>(),
                    };

                    var material = new BoundMaterial();

                    bGeometry.Materials.Add(material);

                    var matColour = new uint_r { Value = 0 };

                    bGeometry.MaterialColours.Add(matColour);

                    var meshVertices = mesh.Vertices().ToList();

                    for (int i = 0; i < meshVertices.Count; i++)
                    {
                        var vertex = meshVertices[i];
                        var bVertex = new BoundVertex
                        {
                            X = Convert.ToInt16(vertex.x / quantum.X),
                            Y = Convert.ToInt16(vertex.y / quantum.Y),
                            Z = Convert.ToInt16(vertex.z / quantum.Z),
                        };

                        bGeometry.Vertices.Add(bVertex);
                        bGeometry.Unknown_78h_Data.Add(bVertex);
                    }

                    var meshTriangles = mesh.Triangles().ToList();

                    for (int i = 0; i < meshTriangles.Count; i++)
                    {
                        var polygon = new BoundPolygon();
                        var triangle = new BoundPolygonTriangle();

                        triangle.TriArea = 0.0f;

                        int vidx1 = meshTriangles[i].a;
                        int vidx2 = meshTriangles[i].b;
                        int vidx3 = meshTriangles[i].c;

                        if (vidx1 == -1 || vidx2 == -1 || vidx3 == -1)
                        {
                            continue;
                        }

                        triangle.TriIndex1 = (ushort)((triangle.TriIndex1 & ~0x7FFF) | (vidx1 & 0x7FFF));
                        triangle.TriIndex2 = (ushort)((triangle.TriIndex2 & ~0x7FFF) | (vidx2 & 0x7FFF));
                        triangle.TriIndex3 = (ushort)((triangle.TriIndex3 & ~0x7FFF) | (vidx3 & 0x7FFF));

                        triangle.EdgeIndex1 = 0;
                        triangle.EdgeIndex2 = 1;
                        triangle.EdgeIndex3 = 2;

                        polygon.data = new byte[16];

                        int offset = 0;

                        byte[] bytes = BitConverter.GetBytes(triangle.TriArea);
                        Buffer.BlockCopy(bytes, 0, polygon.data, offset, bytes.Length);
                        offset += bytes.Length;

                        bytes = BitConverter.GetBytes(triangle.TriIndex1);
                        Buffer.BlockCopy(bytes, 0, polygon.data, offset, bytes.Length);
                        offset += bytes.Length;

                        bytes = BitConverter.GetBytes(triangle.TriIndex2);
                        Buffer.BlockCopy(bytes, 0, polygon.data, offset, bytes.Length);
                        offset += bytes.Length;

                        bytes = BitConverter.GetBytes(triangle.TriIndex3);
                        Buffer.BlockCopy(bytes, 0, polygon.data, offset, bytes.Length);
                        offset += bytes.Length;

                        bytes = BitConverter.GetBytes(triangle.EdgeIndex1);
                        Buffer.BlockCopy(bytes, 0, polygon.data, offset, bytes.Length);
                        offset += bytes.Length;

                        bytes = BitConverter.GetBytes(triangle.EdgeIndex2);
                        Buffer.BlockCopy(bytes, 0, polygon.data, offset, bytes.Length);
                        offset += bytes.Length;

                        bytes = BitConverter.GetBytes(triangle.EdgeIndex3);
                        Buffer.BlockCopy(bytes, 0, polygon.data, offset, bytes.Length);
                        offset += bytes.Length;

                        bGeometry.Polygons.Add(polygon);

                        var matIndex = new byte_r { Value = 0 };

                        bGeometry.PolygonMaterialIndices.Add(matIndex);
                    }

                    bComposite.Children.Add(bGeometry);

                    if (outputFileInfos.Extension == ".ybn")
                    {
                        var ybn = new YbnFile
                        {
                            Bound = bComposite
                        };

                        ybn.Save(opts.OutputFile);
                    }
                    else if (outputFileInfos.Extension == ".ydr")
                    {
                        ydr.Drawable.Bound = bComposite;
                        ydr.Save(opts.OutputFile);
                    }
                    else
                    {
                        Console.WriteLine("Output file type not valid");
                    }
                }
                else
                {
                    Console.WriteLine("Input file type not valid");
                }

            });
        }

        static ModelData GenCol_GetModelData(ResourcePointerList64<DrawableModel> models)
        {
            var vertices = new List<VertexVector3>();
            var bbMin = new Vector3(float.MaxValue);
            var bbMax = new Vector3(float.MinValue);
            var bsCenter = Vector3.Zero;
            float bsRadius = 0.0f;
            var geometries = new List<GeometryData>();

            for (int i = 0; i < models.Entries.Count; i++)
            {
                var entry = models.Entries[i];

                for (int j = 0; j < entry.Geometries.Count; j++)
                {
                    var geometry = entry.Geometries[j];

                    var gVertices = new List<VertexVector3>();
                    var gBbMin = new Vector3(float.MaxValue);
                    var gBbMax = new Vector3(float.MinValue);
                    var gBsCenter = Vector3.Zero;
                    float gBsRadius = 0.0f;

                    var vb = geometry.VertexData.VertexBytes;
                    var vs = geometry.VertexData.VertexStride;
                    var vc = geometry.VertexData.VertexCount;

                    for (int k = 0; k < vc; k++)
                    {
                        var position = MetaUtils.ConvertData<Vector3>(vb, k * vs);
                        var vertex = new VertexVector3(k, position);

                        gBbMin = Vector3.Min(gBbMin, vertex.PositionVector);
                        gBbMax = Vector3.Max(gBbMax, vertex.PositionVector);

                        bbMin = Vector3.Min(bbMin, vertex.PositionVector);
                        bbMax = Vector3.Max(bbMax, vertex.PositionVector);

                        gVertices.Add(vertex);
                        vertices.Add(vertex);
                    }

                    if (gVertices.Count > 0)
                    {
                        gBsCenter = (gBbMin + gBbMax) * 0.5f;

                        foreach (var vertex in gVertices)
                        {
                            gBsRadius = Math.Max(gBsRadius, (vertex.PositionVector - gBsCenter).Length());
                        }
                    }

                    var gData = new GeometryData
                    {
                        Ref = geometry,
                        BbMax = gBbMax,
                        BbMin = gBbMin,
                        BsCenter = gBsCenter,
                        BsRadius = gBsRadius,
                        Vertices = gVertices,
                        Indices = geometry.IndexBuffer.Indices.Data.Select(e => e.Value).ToList()
                    };

                    geometries.Add(gData);
                }
            }

            if (vertices.Count > 0)
            {
                bsCenter = (bbMin + bbMax) * 0.5f;

                foreach (var vertex in vertices)
                {
                    bsRadius = Math.Max(bsRadius, (vertex.PositionVector - bsCenter).Length());
                }
            }

            return new ModelData
            {
                BbMin = bbMin,
                BbMax = bbMax,
                BsCenter = bsCenter,
                BsRadius = bsRadius,
                Geometries = geometries,
                Vertices = vertices
            };
        }

        static DMesh3 GenCol_CreateMesh(List<DefaultConvexFace<VertexVector3>> triangles)
        {
            DMesh3 mesh = new DMesh3();

            var g3Vertices  = new List<Vector3d>();
            var g3Triangles = new List<Vector3d[]>();

            for (int i = 0; i < triangles.Count; i++)
            {
                var t1 = new Vector3d(triangles[i].Vertices[0].Position[0], triangles[i].Vertices[0].Position[1], triangles[i].Vertices[0].Position[2]);
                var t2 = new Vector3d(triangles[i].Vertices[1].Position[0], triangles[i].Vertices[1].Position[1], triangles[i].Vertices[1].Position[2]);
                var t3 = new Vector3d(triangles[i].Vertices[2].Position[0], triangles[i].Vertices[2].Position[1], triangles[i].Vertices[2].Position[2]);

                var v1 = g3Vertices.FindIndex(e => e.x == t1.x && e.y == t1.y && e.z == t1.z);
                var v2 = g3Vertices.FindIndex(e => e.x == t2.x && e.y == t2.y && e.z == t2.z);
                var v3 = g3Vertices.FindIndex(e => e.x == t3.x && e.y == t3.y && e.z == t3.z);

                if(v1 == -1)
                {
                    g3Vertices.Add(t1);
                    mesh.AppendVertex(t1);
                }

                if (v2 == -1)
                {
                    g3Vertices.Add(t2);
                    mesh.AppendVertex(t2);
                }

                if (v3 == -1)
                {
                    g3Vertices.Add(t3);
                    mesh.AppendVertex(t3);
                }

                g3Triangles.Add(new Vector3d[] { t1, t2, t3 });
            }

            for (int i = 0; i < g3Triangles.Count; i++)
            {
                var idx = new Index3i(
                    g3Vertices.IndexOf(g3Triangles[i][0]),
                    g3Vertices.IndexOf(g3Triangles[i][1]),
                    g3Vertices.IndexOf(g3Triangles[i][2])
                );

                mesh.AppendTriangle(idx);
            }

            return mesh;
        }

        static void GenCol_Reshape(DMesh3 mesh, int smooth, int triangleCount)
        {
            if (smooth > 0)
            {
                Remesher remesher = new Remesher(mesh)
                {
                    EnableCollapses = false,
                    EnableFlips = false,
                    EnableSplits = false,
                    EnableSmoothing = true,
                    SmoothType = Remesher.SmoothTypes.MeanValue
                };

                for (int i = 0; i < smooth; i++)
                    remesher.BasicRemeshPass();
            }
            if (triangleCount != -1)
            {
                Reducer reducer = new Reducer(mesh);
                reducer.ReduceToTriangleCount(triangleCount);
            }
        }

        static DMesh3 GenCol_CleanVertices(DMesh3 mesh)
        {
            var newMesh      = new DMesh3(mesh, true);
            var tmpVertices  = newMesh.Vertices().ToList();
            var tmpTriangles = newMesh.Triangles().ToList();

            var finalVertices = new List<Vector3d>();
            var usedVertices  = new List<int>();

            for (int i = 0; i < tmpTriangles.Count; i++)
            {
                var triangle = tmpTriangles[i];

                if (usedVertices.IndexOf(triangle.a) == -1)
                    usedVertices.Add(triangle.a);

                if (usedVertices.IndexOf(triangle.b) == -1)
                    usedVertices.Add(triangle.b);

                if (usedVertices.IndexOf(triangle.c) == -1)
                    usedVertices.Add(triangle.c);
            }

            usedVertices.Sort();

            var finalMesh = new DMesh3();

            for (int i = 0; i < usedVertices.Count; i++)
            {
                finalVertices.Add(tmpVertices[usedVertices[i]]);
                finalMesh.AppendVertex(tmpVertices[usedVertices[i]]);
            }

            for (int i = 0; i < tmpTriangles.Count; i++)
            {
                var triangle = tmpTriangles[i];

                triangle.a = finalVertices.IndexOf(tmpVertices[triangle.a]);
                triangle.b = finalVertices.IndexOf(tmpVertices[triangle.b]);
                triangle.c = finalVertices.IndexOf(tmpVertices[triangle.c]);

                if (triangle.a == -1 || triangle.b == -1 || triangle.c == -1)
                    continue;

                finalMesh.AppendTriangle(triangle);
            }

            return finalMesh;

        }

        static BoundComposite GenCol_CreateBoundComposite(ModelData data)
        {
            var bComposite = new BoundComposite()
            {
                BoundingBoxCenter = (RAGE_Vector3)data.BsCenter,
                BoundingSphereRadius = data.BsRadius,
                BoundingBoxMin = (RAGE_Vector3)data.BbMin,
                BoundingBoxMax = (RAGE_Vector3)data.BbMax,
                CenterGravity = new RAGE_Vector3(0.0f, 0.0f, 0.0f),
                Margin = 0.04f,
                Type = 10,
                Children = new ResourcePointerArray64<Bound>(),
                ChildFlags1 = new ResourceSimpleArray<ulong_r>() { new ulong_r { Value = 0 } },
                ChildFlags2 = new ResourceSimpleArray<ulong_r>() { new ulong_r { Value = 0 } }
            };

            var mat = Matrix5x4.Identity;
            var matrix = new RAGE_Matrix4();

            matrix.m11 = mat.M11;
            matrix.m12 = mat.M12;
            matrix.m13 = mat.M13;
            matrix.m14 = mat.M14;
            matrix.m21 = mat.M21;
            matrix.m21 = mat.M21;
            matrix.m22 = mat.M22;
            matrix.m23 = mat.M23;
            matrix.m24 = mat.M24;
            matrix.m31 = mat.M31;
            matrix.m32 = mat.M32;
            matrix.m33 = mat.M33;
            matrix.m34 = mat.M34;
            matrix.m41 = mat.M41;
            matrix.m42 = mat.M42;
            matrix.m43 = mat.M43;
            matrix.m44 = mat.M44;

            bComposite.ChildTransformations1 = new ResourceSimpleArray<RAGE_Matrix4> { matrix };
            bComposite.ChildTransformations2 = new ResourceSimpleArray<RAGE_Matrix4> { matrix };

            return bComposite;
        }
    }

    class GeometryData
    {
        public DrawableGeometry Ref = new DrawableGeometry();
        public Vector3 BbMin = new Vector3(float.MaxValue);
        public Vector3 BbMax = new Vector3(float.MinValue);
        public Vector3 BsCenter = Vector3.Zero;
        public float BsRadius = 0.0f;
        public List<VertexVector3> Vertices = new List<VertexVector3>();
        public List<ushort> Indices = new List<ushort>();
    }

    class ModelData
    {
        public Vector3 BbMin = new Vector3(float.MaxValue);
        public Vector3 BbMax = new Vector3(float.MinValue);
        public Vector3 BsCenter = Vector3.Zero;
        public float BsRadius = 0.0f;
        public List<VertexVector3> Vertices = new List<VertexVector3>();
        public List<GeometryData> Geometries = new List<GeometryData>();
    }

    class VertexVector3 : IVertex
    {
        public Vector3 PositionVector = Vector3.Zero;

        public int Index = -1;

        public double[] Position
        {
            get { return new double[] { PositionVector.X, PositionVector.Y, PositionVector.Z }; }
        }

        public VertexVector3(int index, float x, float y, float z)
        {
            Index = index;
            PositionVector = new Vector3(x, y, z);
        }

        public VertexVector3(int index, Vector3 v)
        {
            Index = index;
            PositionVector = v;
        }
    }
}
