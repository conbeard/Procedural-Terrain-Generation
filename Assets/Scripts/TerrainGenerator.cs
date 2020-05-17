using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() {
        Mesh mesh = new Mesh();
        Vector3[] verticies = new Vector3[0];

        verticies.Append(new Vector3(0.0f, 0.0f, 0.0f));
        verticies.Append(new Vector3(0.0f, 1.0f, 0.0f));
        verticies.Append(new Vector3(1.0f, 1.0f, 0.0f));

        mesh.vertices = verticies;
        MeshFilter mfComponent = gameObject.AddComponent<MeshFilter>();
        mfComponent.mesh = mesh;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
