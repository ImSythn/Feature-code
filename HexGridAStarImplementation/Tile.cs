using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathing;

public class Tile : MonoBehaviour, IAStarNode
{
	public Vector2Int cordinates;
    public float TravelCost;

	public IEnumerable<IAStarNode> Neighbours => Grid.Instance.GetTileNeighbours(this);

	public float CostTo(IAStarNode neighbour)
	{
		Tile neighbourTile = (Tile)neighbour;
		return neighbourTile.TravelCost;
	}

	public float EstimatedCostTo(IAStarNode goal)
	{
		Tile goalTile = (Tile)goal;

		// Manhattan distances is optimal hexagon grid.
		return Mathf.Abs(cordinates.x - goalTile.cordinates.x) + Mathf.Abs(cordinates.y - goalTile.cordinates.y) ;
	}

	public void HandleVisualization(Color color) 
	{
		GetComponent<MeshRenderer>().material.color = color;
	}

	public void SetCordinates(int x, int y)
	{
		cordinates.x = x;
		cordinates.y = y;
	}

	public void SetBiome(Biome biome)
	{
		GetComponent<MeshRenderer>().material = Resources.Load<Material>($"Materials/{System.Enum.GetName(typeof(Biome), biome)}");
		TravelCost = (float)biome;
	}

	void OnMouseOver()
	{
		if (Input.GetMouseButtonUp(0))
		{
			Grid.Instance.TileClicked(this);
		}
	}
}
