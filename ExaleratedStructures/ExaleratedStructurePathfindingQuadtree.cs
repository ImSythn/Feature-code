using System.Collections.Generic;

public struct PolygonEdge
{
	public Hole hole;

	public Edge edge;
}

public class QuadTree
{
	private Rect boundary;

	private QuadTree[] children;

	private PolygonEdge[] edges;

	private int edgeCount;

	public QuadTree(Rect boundary)
	{
		this.boundary = boundary;
			
		children = null;

		edges = new PolygonEdge[2];
		edgeCount = 0;
	}

	public void Query(Edge edge, IList<PolygonEdge> result)
	{
		if (!Rect.LineIntersectsRect(edge, boundary))
			return;

		if (children != null)
		{
			for (int i = 0; i < children.Length; i++)
				children[i].Query(edge, result);
		}
		else
		{
			for (int i = 0; i < edgeCount; i++)
				result.Add(edges[i]);
		}
	}

	public void Insert(PolygonEdge polygonEdge)
	{
		if (!Rect.LineIntersectsRect(polygonEdge.edge, boundary))
			return;

		if (children == null && edgeCount < edges.Length)
		{
			edges[edgeCount] = polygonEdge;
			edgeCount++;
		}
		else
		{
			if (children == null)
				Subdivide();

			for (int i = 0; i < children.Length; i++)
				children[i].Insert(polygonEdge);
		}
	}

	private void Subdivide()
	{
		float x = boundary.x;
		float y = boundary.y;
		float w = boundary.width;
		float h = boundary.height;

		Rect ne = new Rect(x + w / 2, y + h / 2, w / 2, h / 2);
		Rect nw = new Rect(x, y + h / 2, w / 2, h / 2);
		Rect se = new Rect(x + w / 2, y, w / 2, h / 2);
		Rect sw = new Rect(x, y, w / 2, h / 2);

		children = new QuadTree[4];
		children[0] = new QuadTree(ne);
		children[1] = new QuadTree(nw);
		children[2] = new QuadTree(se);
		children[3] = new QuadTree(sw);

		for (int i = 0; i < edges.Length; i++)
			Insert(edges[i]);

		edges = null;
	}
}
