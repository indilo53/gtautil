
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RageLib.GTA5.Utilities;
using RageLib.Hash;
using RageLib.Helpers;
using RageLib.Resources.Common;
using RageLib.Resources.GTA5.PC.Drawables;
using RageLib.Resources.GTA5.PC.GameFiles;
using RageLib.Resources.GTA5.PC.Meta;
using RageLib.Resources.GTA5.PC.Textures;
using RageLib.ResourceWrappers.GTA5.PC.Textures;
using SharpDX;

namespace GTAUtil
{
    partial class Program
    {
        static void HandleDeamonOptions(string[] args)
        {
            CommandLine.Parse<DaemonOptions>(args, (opts, gOpts) =>
            {
                Init(args);

                Console.WriteLine("Listening on port " + opts.Port);

                Daemon_StartWebServer(new[] { "http://127.0.0.1:" + opts.Port + "/" });
            });
        }

        public static void Daemon_StartWebServer(string[] prefixes)
        {
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("Prefixes needed");

            HttpListener listener = new HttpListener();

            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            listener.Start();

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                JToken responseJSON = null;

                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string json = reader.ReadToEnd();

                    try
                    {

                        var data   = JObject.Parse(json);
                        var action = (string)data["action"];
                        
                        switch(action)
                        {
                            case "drawable.parse":
                            {
                                    var path = (string)data["path"];
                                    var ydr = new YdrFile();

                                    ydr.Load(path);

                                    responseJSON = Deamon_GetDrawableJSON(ydr.Drawable, path);

                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                    }

                }

                string responseText = JsonConvert.SerializeObject(responseJSON, new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.None });

                byte[] buffer = Encoding.UTF8.GetBytes(responseText);

                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);

                output.Close();
            }
        }

        public static TxdFile[] Deamon_LoadExternalTextures(string drawablePath)
        {
            Console.WriteLine("Loading external textures");

            var txds = new List<TxdFile>();

            var files = Directory.GetFiles(Path.GetDirectoryName(drawablePath), "*.ytd");

            for(int i=0; i<files.Length; i++)
            {
                Console.WriteLine("  " + files[i]);

                var txd = new TxdFile();

                txd.Load(files[i]);

                txds.Add(txd);
            }

            return txds.ToArray();
        }

        public static JObject Deamon_GetDrawableJSON(GtaDrawable drawable, string drawablePath)
        {
            Console.WriteLine("Requested drawable : " + drawable.Name.Value);

            var txds = Deamon_LoadExternalTextures(drawablePath);

            var data = new JObject()
            {
                ["shaderGroup"] = new JObject()
                {
                    ["shaders"] = new JArray(),
                    ["textures"] = new JArray()
                },
                ["models"] = new JObject()
                {
                    ["high"]    = new JArray(),
                    ["medium"]  = new JArray(),
                    ["low"]     = new JArray(),
                    ["verylow"] = new JArray(),
                    ["x"]       = new JArray(),
                }
            };

            var jShaders  = (JArray)data["shaderGroup"]["shaders"];
            var jTextures = (JArray)data["shaderGroup"]["textures"];

            for (int i = 0; i < drawable.ShaderGroup.Shaders.Count; i++)
            {
                var shader = drawable.ShaderGroup.Shaders[i];

                var jShader = new JObject()
                {
                    ["textures"] = new JObject()
                };

                for (int j=0; j<shader.ParametersList.Parameters.Count; j++)
                {
                    var param = shader.ParametersList.Parameters[j];

                    if (param.Data is TextureDX11)
                    {
                        var tx = param.Data as TextureDX11;
                        var txName = tx.Name.Value.ToLowerInvariant();

                        switch (param.Unknown_1h)
                        {
                            case 0: jShader["textures"]["diffuse"] = txName; break;
                            case 2: jShader["textures"]["specular"] = txName; break;
                            case 3: jShader["textures"]["bump"] = txName; break;
                        }
                    }
                    else if (param.Data is Texture)
                    {
                        var txName = (param.Data as Texture).Name.Value.ToLowerInvariant();

                        for(int k=0; k< txds.Length; k++)
                        {
                            var txd = txds[k];

                            for (int l = 0; l < txd.TextureDictionary.Textures.Entries.Count; l++)
                            {
                                var tx      = txd.TextureDictionary.Textures.Entries[l];
                                var txName2 = tx.Name.Value.ToLowerInvariant();

                                if (txName == txName2)
                                {
                                    switch (param.Unknown_1h)
                                    {
                                        case 0: jShader["textures"]["diffuse"]  = txName; break;
                                        case 2: jShader["textures"]["specular"] = txName; break;
                                        case 3: jShader["textures"]["bump"]     = txName; break;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                jShaders.Add(jShader);
            }

            for (int i = 0; i < txds.Length; i++)
            {
                var txd = txds[i];

                for (int j = 0; j < txd.TextureDictionary.Textures.Entries.Count; j++)
                {
                    var tx        = txd.TextureDictionary.Textures.Entries[j];
                    var txWrapper = new TextureWrapper_GTA5_pc(tx);
                    var txName    = tx.Name.Value.ToLowerInvariant();
                    var path      = Path.GetTempPath() + "\\gtautil_texture_" + txName + ".dds";

                    for(int m=0; m< jShaders.Count; m++)
                    {
                        var jShader = (JObject) jShaders[m];

                        if ((string)jShader["textures"]["diffuse"] == txName || (string)jShader["textures"]["specular"] == txName || (string)jShader["textures"]["bump"] == txName)
                        {
                            Console.WriteLine("Reusing external texture " + txName);

                            DDSIO.SaveTextureData(txWrapper, path);

                            Utils.Hash(txName);

                            var jTx = new JObject();
                            var txNameHash = Jenkins.Hash(txName);

                            jTx["hash"] = txNameHash;
                            jTx["name"] = txName;
                            jTx["path"] = path;
                            jTx["width"] = tx.Width;
                            jTx["height"] = tx.Height;

                            jTextures.Add(jTx);
                        }
                    }
                }
            }

            for (int i = 0; i < drawable.ShaderGroup.TextureDictionary.Textures.Entries.Count; i++)
            {
                var tx        = drawable.ShaderGroup.TextureDictionary.Textures.Entries[i];
                var txWrapper = new TextureWrapper_GTA5_pc(tx);
                var txName    = tx.Name.Value.ToLowerInvariant();
                var path      = Path.GetTempPath() + "\\gtautil_texture_" + txName + ".dds";

                DDSIO.SaveTextureData(txWrapper, path);

                Utils.Hash(txName);

                var jTx = new JObject();
                var txNameHash = drawable.ShaderGroup.TextureDictionary.TextureNameHashes.Entries[i].Value;

                jTx["hash"]   = txNameHash;
                jTx["name"]   = txName;
                jTx["path"]   = path;
                jTx["width"]  = tx.Width;
                jTx["height"] = tx.Height;

                jTextures.Add(jTx);
            }

            for (int i = 0; i < drawable.DrawableModelsHigh.Entries.Count; i++)
            {
                var jModel = Deamon_GetModelData(drawable.DrawableModelsHigh?.Entries[i]);
                ((JArray)data["models"]["high"]).Add(jModel);
            }

            for (int i = 0; i < drawable.DrawableModelsMedium?.Entries.Count; i++)
            {
                var jModel = Deamon_GetModelData(drawable.DrawableModelsMedium.Entries[i]);
                ((JArray)data["models"]["medium"]).Add(jModel);
            }

            for (int i = 0; i < drawable.DrawableModelsLow?.Entries.Count; i++)
            {
                var jModel = Deamon_GetModelData(drawable.DrawableModelsLow.Entries[i]);
                ((JArray)data["models"]["low"]).Add(jModel);
            }

            for (int i = 0; i < drawable.DrawableModelsVeryLow?.Entries.Count; i++)
            {
                var jModel = Deamon_GetModelData(drawable.DrawableModelsVeryLow.Entries[i]);
                ((JArray)data["models"]["verylow"]).Add(jModel);
            }

            for (int i = 0; i < drawable.DrawableModelsX?.Entries.Count; i++)
            {
                var jModel = Deamon_GetModelData(drawable.DrawableModelsX.Entries[i]);
                ((JArray)data["models"]["x"]).Add(jModel);
            }

            return data;
        }

        public static JObject Deamon_GetModelData(DrawableModel model)
        {
            var data = new JObject()
            {
                ["shaderMapping"] = new JArray(),
                ["geometries"]    = new JArray()
            };

            var jShaderMapping = (JArray)data["shaderMapping"];
            var jGeometries    = (JArray)data["geometries"];

            for (int i = 0; i < model.ShaderMapping.Count; i++)
            {
                jShaderMapping.Add(model.ShaderMapping[i].Value);
            }

            for (int i = 0; i < model.Geometries.Count; i++)
            {
                var geometry = model.Geometries[i];

                var jGeometry = new JObject()
                {
                    ["vertices"]  = new JArray(),
                    ["triangles"] = new JArray()
                };

                var jTriangles = (JArray)jGeometry["triangles"];
                var jVertices  = (JArray)jGeometry["vertices"];

                var vertices  = new List<Vector3>();

                var vb = geometry.VertexData.VertexBytes;
                var vs = geometry.VertexData.VertexStride;
                var vc = geometry.VertexData.VertexCount;

                for (int j=0; j<vc; j++)
                {
                    var jVertex = new JObject()
                    {
                        ["type"] = geometry.VertexData.VertexType.ToString()
                    };

                    switch (geometry.VertexData.VertexType)
                    {
                        case VertexType.Default:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypeDefault>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["colour"] = vt.Colour;
                                jVertex["texCoord"] = new JObject() { ["x"] = vt.Texcoord.X, ["y"] = vt.Texcoord.Y };

                                break;
                            }

                        case VertexType.DefaultEx:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypeDefaultEx>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["colour"] = vt.Colour;
                                jVertex["texCoord"] = new JObject() { ["x"] = vt.Texcoord.X, ["y"] = vt.Texcoord.Y };

                                break;
                            }

                        case VertexType.PNCCT:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCCT>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["texCoord"] = new JObject() { ["x"] = vt.Texcoord.X, ["y"] = vt.Texcoord.Y };

                                break;
                            }

                        case VertexType.PNCCTTTT:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCCTTTT>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };

                                break;
                            }

                        case VertexType.PNCTTTX:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCTTTX>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["colour"] = vt.Colour;

                                break;
                            }

                        case VertexType.PNCTTTX_2:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCTTTX_2>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["colour"] = vt.Colour;

                                break;
                            }

                        case VertexType.PNCTTTX_3:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCTTTX_3>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["colour"] = vt.Colour;

                                break;
                            }

                        case VertexType.PNCTTX:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCTTX>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["colour"] = vt.Colour;

                                break;
                            }

                        case VertexType.PNCCTTX:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCCTTX>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };

                                break;
                            }

                        case VertexType.PNCCTTX_2:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCCTTX_2>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };

                                break;
                            }

                        case VertexType.PNCCTTTX:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCCTTTX>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };

                                break;
                            }

                        case VertexType.PNCCTT:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCCTT>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };

                                break;
                            }

                        case VertexType.PNCCTX:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNCCTX>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["texCoord"] = new JObject() { ["x"] = vt.Texcoord.X, ["y"] = vt.Texcoord.Y };

                                break;
                            }

                        case VertexType.PTT:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePTT>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };;

                                break;
                            }

                        case VertexType.PNC:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePNC>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["normal"] = new JObject() { ["x"] = vt.Normal.X, ["y"] = vt.Normal.Y, ["z"] = vt.Normal.Z };
                                jVertex["colour"] = vt.Colour;

                                break;
                            }

                        case VertexType.PCT:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePCT>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["colour"] = vt.Colour;
                                jVertex["texCoord"] = new JObject() { ["x"] = vt.Texcoord.X, ["y"] = vt.Texcoord.Y };

                                break;
                            }

                        case VertexType.PT:
                            {
                                var vt = MetaUtils.ConvertData<VertexTypePT>(vb, j * vs);

                                vertices.Add(vt.Position);

                                jVertex["position"] = new JObject() { ["x"] = vt.Position.X, ["y"] = vt.Position.Y, ["z"] = vt.Position.Z };
                                jVertex["texCoord"] = new JObject() { ["x"] = vt.Texcoord.X, ["y"] = vt.Texcoord.Y };

                                break;
                            }

                        default:break;
                    }

                    jVertices.Add(jVertex);
                }

                for (int j = 0; j < geometry.IndexBuffer.Indices.Count - 2; j += 3)
                {
                    var triangle = new JArray() {
                        geometry.IndexBuffer.Indices[j + 0].Value,
                        geometry.IndexBuffer.Indices[j + 1].Value,
                        geometry.IndexBuffer.Indices[j + 2].Value
                    };

                    jTriangles.Add(triangle);
                }

                jGeometries.Add(jGeometry);

            }

            return data;
        }

        public static Texture Deamon_GetTextureBaseParam(List<string> texNames, int index)
        {
            var name = "unknown";

            if (texNames.Count > index)
            {
                var nameval = texNames[index];
                if (nameval != null)
                {
                    name = texNames[index];
                }
            }

            var texParam = new Texture();

            texParam.Unknown_4h = 1;
            texParam.Unknown_30h = 131073;
            texParam.Name = new string_r() { Value = name };

            return texParam;
        }
    }
}
