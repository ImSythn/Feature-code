using Pathing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum Biome
{
	Water = 0,
	Grass = 1,
	Forest = 3,
	Desert = 5,
	Mountain = 10,
}

public class Grid : MonoBehaviour
{
	private Vector2Int[,] oddRDirections = {
		{new Vector2Int(+1, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1),
		new Vector2Int(-1, 0), new Vector2Int(-1, +1), new Vector2Int(0, +1)},
		{new Vector2Int(+1, 0), new Vector2Int(+1, -1), new Vector2Int(0, -1),
		new Vector2Int(-1, 0), new Vector2Int(0, +1), new Vector2Int(+1, +1)}
	};

	private static Grid _instance;

	public static Grid Instance { get { return _instance; } }

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(this.gameObject);
		}
		else
		{
			_instance = this;
		}
	}

	public Transform hexPrefab;

	public int gridWidth = 8;
	public int gridHeight = 8;

	private Tile[,] hexGrid;

	private float hexWidth = 1.0f;
	private float hexHeight = 0.991327f;

	private Tile startTile;
	private Tile goalTile;

	private List<Tile> path;

	private System.Random random;

	Vector3 startPos;

	void Start()
	{
		random = new System.Random();
		hexGrid = new Tile[gridWidth, gridHeight];
		CalcStartPos();
		CreateGrid();
	}

	private void CalcStartPos()
	{
		float offset = 0;
		if (gridHeight / 2 % 2 != 0)
			offset = hexWidth / 2;

		float x = -hexWidth * (gridWidth / 2) - offset;
		float z = hexHeight * 0.75f * (gridHeight / 2);

		startPos = new Vector3(x, 0, z);
	}

	private Vector3 CalcWorldPos(Vector2 gridPos)
	{
		float offset = 0;
		if (gridPos.y % 2 != 0)
			offset = hexWidth / 2;

		float x = startPos.x + gridPos.x * hexWidth + offset;
		float z = startPos.z - gridPos.y * hexHeight * 0.75f;

		return new Vector3(x, 0, z);
	}

	private void CreateGrid()
	{
		for (int y = 0; y < gridHeight; y++)
		{
			for (int x = 0; x < gridWidth; x++)
			{
				Transform hex = Instantiate(hexPrefab);
				Vector2 gridPos = new Vector2(x, y);
				hex.position = CalcWorldPos(gridPos);
				hex.parent = this.transform;
				hex.name = "Hexagon" + x + "|" + y;

				Tile tile = hex.GetComponent<Tile>();
				hexGrid[x, y] = tile;
				tile.SetCordinates(x, y);

				Array values = Enum.GetValues(typeof(Biome));

				Biome randomBiome = (Biome)values.GetValue(random.Next(values.Length));
				tile.SetBiome(randomBiome);
			}
		}
	}


	public List<Tile> GetTileNeighbours(Tile tile)
	{
		List<Tile> neighbours = new List<Tile>(); ;

		for (int direction = 0; direction < 6; direction++)
		{
			Vector2Int neighbourCordinates = OddROffsetNeighbour(tile, direction);
			if (validTile(neighbourCordinates.x, neighbourCordinates.y))
				neighbours.Add(hexGrid[neighbourCordinates.x, neighbourCordinates.y]);
		}

		if(neighbours.Count == 0)
			Debug.Log("No path possible.");


		return neighbours;
	}

	private Vector2Int OddROffsetNeighbour(Tile tile, int direction)
	{
		int parity = tile.cordinates.y & 1;
		Vector2Int dir = oddRDirections[parity, direction];
		return new Vector2Int(tile.cordinates.x + dir.x, tile.cordinates.y + dir.y);

	}


	private bool validTile(int x, int y)
	{
		if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight)
			return false;
		if (hexGrid[x, y].TravelCost == 0) // water
			return false;

		return true;
	}

	public void TileClicked(Tile tile)
	{
		if (tile.TravelCost == 0) // water
			return;

		ResetPath();

		if (startTile == null)
			SetStartTile(tile);
		else
			SetGoalTile(tile);

		if (goalTile != null && startTile != null)
		{
			HandleAStar();
		}
	}

	private void HandleAStar()
	{
		path = AStar.GetPath(startTile, goalTile).Cast<Tile>().ToList();
			
		foreach (Tile tile in path)
		{
			if (tile != startTile && tile != goalTile)
			{
				tile.HandleVisualization(Color.red);
			}
		}
	}

	private void ResetPath()
	{
		if (path != null)
		{
			foreach (Tile tile in path)
			{
				if (tile != startTile && tile != goalTile)
				{
					tile.HandleVisualization(Color.white);
				}
			}
		}
		path = null;
	}

	private void SetStartTile(Tile tile)
	{
		startTile = tile;

		tile.HandleVisualization(Color.green);
	}

	private void SetGoalTile(Tile tile)
	{
		if (startTile == tile && goalTile == null)
		{
			startTile.HandleVisualization(Color.white);
			startTile = null;
		}
		else if (goalTile == tile)
		{
			goalTile.HandleVisualization(Color.white);
			goalTile = null;
		}
		else if (startTile != tile)
		{
			if (goalTile != null)
				goalTile.HandleVisualization(Color.white);

			goalTile = tile;
			goalTile.HandleVisualization(Color.blue);
		}

	}
}
