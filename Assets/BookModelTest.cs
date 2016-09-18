using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BookModelTest : MonoBehaviour
{
    public Material page1Texture;
    public Material page1BackTexture;
    public Material page2Texture;
    public Material edgeTexture;
    Material coverTexture;
    GameObject page1;
    GameObject page1Back;
    GameObject page2;
    PageSimNode[] pageSim;

    class PageSimNode
    {
        public Vector3 modelPos;
        public Vector3 pos;
        public Vector3 vel;
        public Vector3 force;
        public bool immovable;
        float minY;
        struct PageSimConnection
        {
            public float length;
            public PageSimNode target;
            public Vector3 GetForce(Vector3 pos)
            {
                Vector3 offset = (target.pos - pos);
                float actualRange = offset.magnitude;
                float tension = actualRange - length;
                //if(tension > 0)
                {
                    return offset.normalized * tension;
                }
                return Vector3.zero;
            }
            public PageSimConnection(PageSimNode target, float length)
            {
                this.target = target;
                this.length = length;
            }
        }
        List<PageSimConnection> connections = new List<PageSimConnection>();

        public PageSimNode(Vector3 pos, float minY, bool immovable)
        {
            this.pos = pos;
            this.modelPos = pos;
            this.immovable = immovable;
            this.minY = minY;
        }
        public void AddConnection(PageSimNode other)
        {
            float length = (other.pos - this.pos).magnitude;
            //Debug.Log("New connection from"+other.pos+" to "+this.pos+", length = "+length);
            connections.Add(new PageSimConnection(other, length));
            other.connections.Add(new PageSimConnection(this, length));
        }

        public void UpdateForces()
        {
            if (connections.Count == 2)
            {
                PageSimNode neighbor0 = connections[0].target;
                PageSimNode neighbor1 = connections[0].target;
                PageSimNode neighbor2 = (neighbor0.connections.Count > 1) ? neighbor0.connections[0].target: null;
                PageSimNode neighbor3 = (neighbor1.connections.Count > 1) ? neighbor1.connections[1].target : null;

                Vector3 sumOfNeighbors = neighbor0.pos + neighbor1.pos;
                int numNeighbors = 2;
                if (neighbor2 != null)
                {
                    sumOfNeighbors += neighbor2.pos;
                    numNeighbors++;
                }
                if (neighbor3 != null)
                {
                    sumOfNeighbors += neighbor3.pos;
                    numNeighbors++;
                }
                Vector3 connectionMidpoint = sumOfNeighbors / numNeighbors;
                Vector3 bentOffset = (pos - connectionMidpoint);
                float bentAmount = bentOffset.magnitude;

                if (bentAmount > 0) // if this is zero the lines are perfectly straight
                {
                    //                    Vector3 bentDirection = bentOffset / bentAmount;

                    float straighteningStrength = 0;// -0.25f;
                    Vector3 straighteningForce = bentOffset * straighteningStrength;// straighteningStrength * (bentDirection * bentAmount);

                    force += straighteningForce;

                    //                connectionMidpoint + bentDirection * connections[0].length;

                    neighbor0.force -= straighteningForce;
                    neighbor1.force -= straighteningForce;
                    if(neighbor2 != null)
                        neighbor2.force -= straighteningForce;

                    if(neighbor3 != null)
                        neighbor3.force -= straighteningForce;

                    Vector3 conn1Offset = (connections[1].target.pos - connections[0].target.pos);
                    float desiredSeparation = connections[1].length + connections[0].length;
                    float conn1OffsetDist = conn1Offset.magnitude;
                    float separatingStrength = -0.25f;
                    if (conn1OffsetDist > 0 && conn1OffsetDist < desiredSeparation)
                    {
                        Vector3 conn1Direction = conn1Offset/conn1OffsetDist;
                        connections[0].target.force -= separatingStrength * conn1Direction * (conn1OffsetDist - desiredSeparation);
                        connections[1].target.force += separatingStrength * conn1Direction * (conn1OffsetDist - desiredSeparation);
                    }
                }
            }

/*                float separationForce = 0.5f;
                connections[0].target.force += bentAmount * separationForce * (connections[0].target.pos - connectionMidpoint);
                connections[1].target.force += bentAmount * separationForce * (connections[1].target.pos - connectionMidpoint);*/
//            }
            
            foreach (PageSimConnection connection in connections)
            {
                force += connection.GetForce(pos);
            }
        }

        public void UpdatePosition()
        {
            if (!immovable)
            {
                vel += force;
                vel *= 0.5f;
                pos += vel;
                float actualMinY = (pos.z < 0)? minY: 0;
                if (pos.y < actualMinY)
                {
                    pos.y = actualMinY;
                }

                if (connections.Count > 0)
                {
                    Vector3 rootPos = connections[0].target.modelPos;
                    modelPos = rootPos + (pos - rootPos).normalized * connections[0].length;
                }
                else
                {
                    modelPos = pos;
                }
            }

            float gravity = -0.002f;
            force = new Vector3(0, gravity, 0);
        }
    }

    // Use this for initialization
    void Start()
    {
        Vector3 padSize = new Vector3(1, 0.1f, -1.25f);
        float epsilon = 0.001f;
        int numPageNodes = 32;
        float pageNodeSpacing = padSize.z / (numPageNodes - 1);

        pageSim = new PageSimNode[numPageNodes];
        pageSim[0] = new PageSimNode(new Vector3(0, padSize.y + epsilon, 0), padSize.y + epsilon, true);
        pageSim[numPageNodes-1] = new PageSimNode(new Vector3(0, padSize.y + epsilon, 1), padSize.y + epsilon, true);

        {
            int Idx = 1;
            int midpoint = pageSim.Length / 2;
            for (; Idx < pageSim.Length/2; ++Idx)
            {
                pageSim[Idx] = new PageSimNode(new Vector3(0, padSize.y + epsilon, Idx * pageNodeSpacing), padSize.y + epsilon, false);
            };
            for (; Idx < pageSim.Length; ++Idx)
            {
                pageSim[Idx] = new PageSimNode(new Vector3(0, padSize.y + epsilon + (midpoint-Idx)*pageNodeSpacing, midpoint * pageNodeSpacing), padSize.y + epsilon, false);
            };
        }
        for(int Idx = 1; Idx < pageSim.Length; ++Idx)
        {
            pageSim[Idx].AddConnection(pageSim[Idx-1]);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[8]
        {
            new Vector3(padSize.x,0,padSize.z), new Vector3(padSize.x,padSize.y,padSize.z),
            new Vector3(0,0,padSize.z), new Vector3(0,padSize.y,padSize.z),
            new Vector3(0,0,0), new Vector3(0,padSize.y,0),
            new Vector3(padSize.x,0,0), new Vector3(padSize.x,padSize.y,0),
        };
        mesh.uv = new Vector2[8]
        {
            new Vector3(0,0), new Vector3(0,1),
            new Vector3(0.3f,0), new Vector3(0.3f,1),
            new Vector3(0.7f,0), new Vector3(0.7f,1),
            new Vector3(1,0), new Vector3(1,1),
        };
        mesh.triangles = new int[]
        {
            0,3,1, 0,2,3,
            2,5,3, 2,4,5,
            4,7,5, 4,6,7,
            6,1,7, 6,0,1,
        };
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().materials = new Material[] { edgeTexture };

        Vector2 pageTip = new Vector2(1, padSize.z);

        Vector3[] page1Vertices = new Vector3[pageSim.Length * 2];
        Vector2[] page1UVs = new Vector2[pageSim.Length * 2];
        Vector3[] page1BackVertices = new Vector3[pageSim.Length * 2];
        Vector2[] page1BackUVs = new Vector2[pageSim.Length * 2];

        DisplayPageSim(page1Vertices);
        DisplayPageSim(page1BackVertices);
        float uvScale = 1.0f / (pageSim.Length-1);
        for (int Idx = 0; Idx < pageSim.Length; ++Idx)
        {
            page1UVs[Idx * 2 + 0] = new Vector2(0, 1-Idx*uvScale);
            page1UVs[Idx * 2 + 1] = new Vector2(1, 1-Idx*uvScale);
            page1BackUVs[Idx * 2 + 0] = new Vector2(0, 1-Idx * uvScale);
            page1BackUVs[Idx * 2 + 1] = new Vector2(1, 1-Idx * uvScale);
        };

        int[] page1Triangles = new int[6 * (pageSim.Length - 1) ];
        int[] page1BackTriangles = new int[6 * (pageSim.Length - 1)];
        int vertexIdx = 0;
        int triangleIdx = 0;
        while (triangleIdx < page1Triangles.Length)
        {
            page1Triangles[triangleIdx+0] = vertexIdx + 0;
            page1Triangles[triangleIdx+1] = vertexIdx + 1;
            page1Triangles[triangleIdx+2] = vertexIdx + 3;

            page1Triangles[triangleIdx+3] = vertexIdx + 0;
            page1Triangles[triangleIdx+4] = vertexIdx + 3;
            page1Triangles[triangleIdx+5] = vertexIdx + 2;

            page1BackTriangles[triangleIdx + 0] = vertexIdx + 0;
            page1BackTriangles[triangleIdx + 1] = vertexIdx + 3;
            page1BackTriangles[triangleIdx + 2] = vertexIdx + 1;

            page1BackTriangles[triangleIdx + 3] = vertexIdx + 0;
            page1BackTriangles[triangleIdx + 4] = vertexIdx + 2;
            page1BackTriangles[triangleIdx + 5] = vertexIdx + 3;

            vertexIdx += 2;
            triangleIdx += 6;
        }

        page1 = new GameObject();
        page1.transform.parent = transform;
        page1.transform.localPosition = new Vector3(0, 0, 0);
        Mesh page1Mesh = new Mesh();
        page1Mesh.vertices = page1Vertices;
        page1Mesh.uv = page1UVs;
        page1Mesh.triangles = page1Triangles;
        page1.AddComponent<MeshFilter>();
        page1.GetComponent<MeshFilter>().mesh = page1Mesh;
        page1.AddComponent<MeshRenderer>();
        page1.GetComponent<MeshRenderer>().material = page1Texture;

        page1Back = new GameObject();
        page1Back.transform.parent = transform;
        page1Back.transform.localPosition = new Vector3(0, 0, 0);
        Mesh page1BackMesh = new Mesh();
        page1BackMesh.vertices = page1BackVertices;
        page1BackMesh.uv = page1BackUVs;
        page1BackMesh.triangles = page1BackTriangles;
        page1Back.AddComponent<MeshFilter>();
        page1Back.GetComponent<MeshFilter>().mesh = page1BackMesh;
        page1Back.AddComponent<MeshRenderer>();
        page1Back.GetComponent<MeshRenderer>().material = page1BackTexture;

        Mesh page2Mesh = new Mesh();
        page2Mesh.vertices = new Vector3[]
        {
            new Vector3(0,padSize.y,0), new Vector3(padSize.x,padSize.y,0),
            new Vector3(0,padSize.y,padSize.z), new Vector3(padSize.x,padSize.y,padSize.z),
        };
        page2Mesh.uv = new Vector2[]
        {
            new Vector3(0,0), new Vector3(0,1),
            new Vector3(1,0), new Vector3(1,1),
        };
        page2Mesh.triangles = new int[]
        {
            0,1,3, 0,3,2,
        };

        page2 = new GameObject();
        page2.transform.parent = transform;
        page2.transform.localPosition = new Vector3(0, 0, 0);
        page2.AddComponent<MeshFilter>();
        page2.GetComponent<MeshFilter>().mesh = page2Mesh;
        page2.AddComponent<MeshRenderer>();
        page2.GetComponent<MeshRenderer>().material = page2Texture;
    }

    // Update is called once per frame
    void Update ()
    {
        Vector3 pageTarget = new Vector3(0, Input.mousePosition.y*0.004f, (Input.mousePosition.x-400)*-0.004f);
        Vector3 pageTargetOffset = pageTarget - pageSim[pageSim.Length - 1].pos;
        float forceMultiplierStep = 1.0f / ((pageSim.Length - 1) * (pageSim.Length-1));
        for(int Idx = 0; Idx < pageSim.Length; ++Idx)
        {
            pageSim[Idx].force += pageTargetOffset * forceMultiplierStep * Idx * Idx;
        }
        RunPageSim();
        RunPageSim();
        RunPageSim();

        Mesh mesh1 = page1.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices1 = mesh1.vertices;
        DisplayPageSim(vertices1);
        mesh1.vertices = vertices1;

        Mesh mesh2 = page1Back.GetComponent<MeshFilter>().mesh;
        mesh2.vertices = vertices1;
    }

    void RunPageSim()
    {
        foreach(PageSimNode node in pageSim)
        {
            node.UpdateForces();
        }
        foreach (PageSimNode node in pageSim)
        {
            node.UpdatePosition();
        }
    }

    void DisplayPageSim(Vector3[] vertices)
    {
        for (int Idx = 0; Idx < pageSim.Length; ++Idx)
        {
            vertices[Idx * 2 + 0] = pageSim[Idx].modelPos;
            vertices[Idx * 2 + 1] = pageSim[Idx].modelPos + new Vector3(1,0,0);
        }
    }
}
