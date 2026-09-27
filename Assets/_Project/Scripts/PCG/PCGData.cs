using UnityEngine;
using System.Collections.Generic;

public class Edge 
{
    public Vector3 U;
    public Vector3 V;

    public Edge(Vector3 u, Vector3 v) { U = u; V = v; }

    public static bool AreEqual(Edge a, Edge b)
    {
        return (a.U == b.U && a.V == b.V) || (a.U == b.V && a.V == b.U);
    }
}

public class Triangle
{
    public Vector3[] Vertices = new Vector3[3];
    public List<Edge> Edges = new List<Edge>();

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vertices[0] = a; Vertices[1] = b; Vertices[2] = c;
        Edges.Add(new Edge(a, b));
        Edges.Add(new Edge(b, c));
        Edges.Add(new Edge(c, a));
    }
}