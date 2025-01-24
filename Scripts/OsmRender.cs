using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class OsmRender
{
    public float BuildingHeight { get; set; } = 2; 
    
    private MeshInstance3D CreateOneBuildingMesh(IList<Vector3> vertices)
    {
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        // C# arrays cannot be resized or expanded, so use Lists to create geometry.
        var verts = new List<Vector3>();
        // var uvs = new List<Vector2>();
        // var normals = new List<Vector3>();
        var indices = new List<int>();

        /***********************************
        * Insert code here to generate mesh.
        * *********************************/
        
        int vNum = vertices.Count;
        verts.AddRange(vertices);
        verts.AddRange(vertices.Select(v => new Vector3(v.X, BuildingHeight, v.Z)));

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

        return meshInstance;

    }
    
}
