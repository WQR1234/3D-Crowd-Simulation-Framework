using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using OsmSharp.API;


public partial class OsmRender : Node
{
	// private double centerX = 0;
	// private double centerY = 0;

	private Dictionary<long, Vector3> _nodesIdDict;
	public int ScaleSize { get; set; } = 111000 / 5;

	public override void _Ready()
	{
		// 逆时针 闭合曲线 （顺逆时针不要弄反，弄反就只能从下（里）面看到）
		// Vector3[] vertices = new[]
		// {
		// 	new Vector3(0, 0, 0), 
		// 	new Vector3(1, 0, 0),
		// 	new Vector3(2, 0, 1),
		// 	new Vector3(0, 0, 1),
		// 	// new Vector3(1, 0, 2),
		// };
		//
		// var meshInstance = CreateBuildingFromVerts(vertices);
		
		// AddChild(meshInstance);

		// string filePath = @"C:\Users\吴启睿\Downloads\map-1.osm";
		// _nodesIdDict = new();
		//
		// ParseOsmFile(filePath);
		
	}

	private MeshInstance3D CreateBuildingFromVerts(IList<Vector3> vertices, Vector3 position)
	{
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		// C# arrays cannot be resized or expanded, so use Lists to create geometry.
		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var normals = new List<Vector3>();
		var indices = new List<int>();

		/***********************************
		* Insert code here to generate mesh.
		* *********************************/
		int vNum = vertices.Count;
		verts.AddRange(vertices);
		verts.AddRange(vertices.Select(v => new Vector3(v.X, 2, v.Z)));

		// 添加底面与顶面索引
		for (int i = 0; i < vNum-2; i++)
		{
			// 底面 （反正也看不到）
			// indices.Add(i+1);
			// indices.Add(0);
			// indices.Add(i+2);
			
			// 顶面
			indices.Add(vNum);
			indices.Add(i+1+vNum);
			indices.Add(i+2+vNum);
		}
		
		
		
		
		// 添加侧面索引
		for (int i = 0; i < vNum; i++)
		{
			indices.Add(i);
			indices.Add((i + 1)%vNum);
			indices.Add(i+vNum);
		    
			indices.Add(i+vNum);
			indices.Add((i + 1)%vNum);
			indices.Add((i + vNum + 1) == 2*vNum ? vNum : (i + vNum + 1));
		    
		}
		

		// Convert Lists to arrays and assign to surface array
		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		// surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		// surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		var arrMesh = new ArrayMesh();
		
		// Create mesh surface from mesh array
		// No blendshapes, lods, or compression used.
		arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

		var meshInstance = new MeshInstance3D();
		// meshInstance.Name = "obstacle01";
		meshInstance.Mesh = arrMesh;
		meshInstance.CreateConvexCollision(false, true);

		meshInstance.Position = position;

		return meshInstance;
	}

	public void ParseOsmFile(string filePath)
	{
		var serializer = new XmlSerializer(typeof(Osm));
		
		string content = File.ReadAllText(filePath);
		var osm = serializer.Deserialize(new StringReader(content)) as Osm;
		if (osm==null)
		{
			GD.Print("parse fail");
			return;
		}

		NormalizeLonLat(osm.Nodes);

		foreach (var osmWay in osm.Ways)
		{
			if (osmWay.Tags.ContainsKey("building"))
			{
				var osmNodeVerts = osmWay.Nodes.Select(id => _nodesIdDict[id]).ToArray();
				if (IsClockwise(osmNodeVerts))
				{
					osmNodeVerts = osmNodeVerts[1..];
					Array.Reverse(osmNodeVerts);
				}
				else
				{
					osmNodeVerts = osmNodeVerts[..^1];
				}
				
				var meshInstance = CreateBuildingFromVerts(osmNodeVerts, Vector3.Zero);
				
				AddChild(meshInstance);
			}
		}
	}

	private void NormalizeLonLat(OsmSharp.Node[] osmNodes)
	{
		double centerX = osmNodes.Sum(x => x.Longitude.GetValueOrDefault());
		double centerY = osmNodes.Sum(x => x.Latitude.GetValueOrDefault());

		centerX /= osmNodes.Length;
		centerY /= osmNodes.Length;

		foreach (var osmNode in osmNodes)
		{
			// GD.Print($"node {osmNode.Id.Value}: {osmNode.Longitude.Value}, {osmNode.Latitude.Value}");
			float dx = (float)((osmNode.Longitude.Value - centerX) * ScaleSize);
			float dy = (float)((osmNode.Latitude.Value - centerY) * ScaleSize);
			_nodesIdDict[osmNode.Id.Value] = new Vector3(-dx, 0, dy);
		}
	}

	private bool IsClockwise(IList<Vector3> points)
	{
		int n = points.Count;

		float area = 0;
		for (int i = 0; i < n - 1; i++)
		{
			int j = (i + 1) % n;
			area += points[i].X * points[j].Z - points[i].Z * points[j].X;
		}

		return area < 0;
	}
}
