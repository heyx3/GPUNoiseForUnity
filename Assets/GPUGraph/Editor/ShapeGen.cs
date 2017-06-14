using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;


public static class ShapeGen
{
    [MenuItem("Gen/Box")]
    public static void GenerateBox()
    {
        string path = EditorUtility.SaveFilePanelInProject("Choose Path", "Skybox", "asset",
                                                           "Choose the path for the box mesh");
        if (path == "")
            return;


        Vector3[] verts = new Vector3[] {
            new Vector3(-1.0f, -1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, 1.0f),
            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(-1.0f, 1.0f, 1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, 1.0f),
            new Vector3(1.0f, 1.0f, -1.0f),
            new Vector3(1.0f, 1.0f, 1.0f)
        };
        Vector3[] normals = new Vector3[verts.Length];
        for (int i = 0; i < normals.Length; ++i)
            normals[i] = verts[i].normalized;
        int[] inds = new int[] {
            //X min:
            0, 2, 3,
            0, 3, 1,
            
            //X max:
            4, 7, 6,
            4, 5, 7,
            
            //Y min:
            0, 1, 5,
            0, 5, 4,
            
            //Y max:
            2, 7, 3,
            2, 6, 7,
            
            //Z min:
            0, 6, 2,
            0, 4, 6,

            //Z max:
            1, 3, 7,
            1, 7, 5,
        };

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.triangles = inds;
        mesh.UploadMeshData(true);
        AssetDatabase.CreateAsset(mesh, StringUtils.GetRelativePath(path, "Assets"));
    }
}