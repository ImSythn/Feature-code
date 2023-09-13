using System.Collections.Generic;

using LibGame;

public class PotentialStaticCollider : IPotentialCollider
{
	private StaticCollisionResolver staticCollisionResolver;
	private Polygon polygon;
	private StaticCollider staticCollider;

	public PotentialStaticCollider(StaticCollisionResolver staticCollisionResolver, Polygon polygon, StaticCollider staticCollider)
	{
		this.staticCollisionResolver = staticCollisionResolver;
		this.polygon = polygon;
		this.staticCollider = staticCollider;
	}

	public void DetectCollision(TickContext context, float deltaTime, EntityHandle entityHandle, EntityColliderSet colliderSet, List<CollisionData> collisionBuffer)
	{
		if (!Rect.TestIntersection(colliderSet.velocityExpandedBounds, polygon.Bounds))
			return;

		CollisionType type = staticCollisionResolver.Invoke(context, entityHandle, staticCollider);

		if (type == CollisionType.NONE)
			return;

		for (int entityColliderIndex = 0; entityColliderIndex < colliderSet.colliders.Count; ++entityColliderIndex)
		{
			var entityColliderPair = colliderSet.colliders[entityColliderIndex];

			if (!entityColliderPair.second.Invoke(entityHandle))
				continue;

			EntityCollider entityCollider = entityColliderPair.first;

			IntersectionResult result = IntersectionUtil.Intersect (entityCollider.WorldSpace, polygon, entityHandle.Transform.velocity);

			if ((!result.currentlyIntersecting && !result.willIntersect) || (result.willIntersect && result.intersectionTime > deltaTime))
				continue;

			CollisionData data = new CollisionData()
			{
				a = new CollisionSubjectData()
				{
					entity = entityHandle,
					normal = -result.normal,
				},

				b = new CollisionSubjectData()
				{
					collider = staticCollider,
					normal = result.normal,
				},

				type = type,
				intersectionResult = result,
				potentialCollider = this
			};

			collisionBuffer.Add(data);
		}
	}
}

public class PotentialDynamicCollider : IPotentialCollider
{
	private EntityCollisionResolver entityCollisionResolver;
	private EntityColliderSet colliderSet;
	private ushort entityID;
	private Transform transform;

	public PotentialDynamicCollider(EntityCollisionResolver entityCollisionResolver, EntityColliderSet entityCollider, ushort entityID, ref Transform transform)
	{
		this.entityCollisionResolver = entityCollisionResolver;
		colliderSet = entityCollider;
		this.entityID = entityID;
		this.transform = transform;
	}

	public void DetectCollision(TickContext context, float deltaTime, EntityHandle testHandle, EntityColliderSet testColliderSet, List<CollisionData> collisionBuffer)
	{
		if (colliderSet == testColliderSet)
			return;

		if (!Rect.TestIntersection(testColliderSet.velocityExpandedBounds, colliderSet.velocityExpandedBounds))
			return;

		EntityHandle handle = context.state.CreateHandle(entityID);

		if (handle.index < testHandle.index)
			return;

		CollisionType type = entityCollisionResolver.Invoke(context, testHandle, handle);

		if (type == CollisionType.NONE)
			return;

		ref Entity e2 = ref context.state.entities.FindByKey(entityID);

		for (int ci1 = 0; ci1 < testColliderSet.colliders.Count; ++ci1)
		{
			var c1 = testColliderSet.colliders[ci1];

			if (!c1.second(testHandle))
				continue;

			for (int ci2 = 0; ci2 < colliderSet.colliders.Count; ++ci2)
			{
				var c2 = colliderSet.colliders[ci2];

				if (!c2.second(handle))
					continue;

				IntersectionResult result = IntersectionUtil.Intersect(c1.first.WorldSpace, c2.first.WorldSpace, testHandle.Transform.velocity, transform.velocity);

				if ((!result.currentlyIntersecting && !result.willIntersect) || (result.willIntersect && result.intersectionTime > deltaTime))
					continue;

				CollisionData data = new CollisionData()
				{
					a = new CollisionSubjectData()
					{
						entity = testHandle,
						normal = -result.normal,
					},

					b = new CollisionSubjectData()
					{
						entity = handle,
						normal = result.normal,
					},

					type = type,
					intersectionResult = result,
					potentialCollider = this
				};

				collisionBuffer.Add(data);
			}
		}
	}

	public override bool Equals(object obj)
	{
		var item = obj as PotentialDynamicCollider;

		if (item == null)
		{
			return false;
		}

		return this.entityID.Equals(item.entityID);
	}

	public override int GetHashCode()
	{
		return this.entityID.GetHashCode();
	}
}

public class Cell
{
	public Cell next = null;
	public IPotentialCollider potentialCollider;
}

class ColliderGrid
{
	private const int GRID_DIVISSIONS_WIDTH = 32;
	private const int GRID_DIVISSIONS_HEIGHT = 8;

	private Rect boundary;

	private MemoryPool<Cell> cellPool;

	private Cell[,] cells;

	private float gridCellWidth;
	private float gridCellHeight;

	public ColliderGrid(Rect boundary)
	{
		this.boundary = boundary;

		cellPool = new MemoryPool<Cell>(new ConstructorFactory<Cell>());
		cells = new Cell[GRID_DIVISSIONS_WIDTH, GRID_DIVISSIONS_HEIGHT];

		gridCellWidth = this.boundary.width / GRID_DIVISSIONS_WIDTH;
		gridCellHeight = this.boundary.height / GRID_DIVISSIONS_HEIGHT;
	}

	private void FindCellBoundsContainingCollider(Rect bounds, out int minX, out int minY, out int maxX, out int maxY)
	{
		minX = (int)((bounds.x - boundary.x) / gridCellWidth);
		minY = (int)((bounds.y - boundary.y) / gridCellHeight);
		maxX = (int)((bounds.x + bounds.width - boundary.x) / gridCellWidth);
		maxY = (int)((bounds.y + bounds.height - boundary.y) / gridCellHeight);

		minX = MathUtil.Clamp(minX, 0, GRID_DIVISSIONS_WIDTH - 1);
		minY = MathUtil.Clamp(minY, 0, GRID_DIVISSIONS_HEIGHT - 1);
		maxX = MathUtil.Clamp(maxX, 0, GRID_DIVISSIONS_WIDTH - 1);
		maxY = MathUtil.Clamp(maxY, 0, GRID_DIVISSIONS_HEIGHT - 1);
	}

	public void Insert(StaticCollider collider, StaticCollisionResolver staticCollisionResolver)
	{
		foreach (Polygon polygon in collider.polygons)
		{
			PotentialStaticCollider potentialStaticCollider = new PotentialStaticCollider(staticCollisionResolver, polygon, collider);

			FindCellBoundsContainingCollider(collider.bounds, out int minX, out int minY, out int maxX, out int maxY);

			for (int x = minX; x <= maxX; x++)
			{
				for (int y = minY; y <= maxY; y++)
				{
					Polygon polygonCell = new Polygon(Polygon.Rectangle(new Rect(boundary.x + (gridCellWidth * x), boundary.y + (gridCellHeight * y), gridCellWidth, gridCellHeight)));

					if (SAT.Intersect(polygon, polygonCell).currentlyIntersecting)
					{
						Cell cell = cellPool.Get();
						cell.next = cells[x, y];
						cell.potentialCollider = potentialStaticCollider;
						cells[x, y] = cell;
					}
				}
			}
		}
	}

	public void Insert(EntityColliderSet collider, ushort entityID, ref Transform transform, EntityCollisionResolver entityCollisionResolver)
	{
		FindCellBoundsContainingCollider(collider.velocityExpandedBounds, out int minX, out int minY, out int maxX, out int maxY);

		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
			{
				PotentialDynamicCollider potentialDynamicCollider = new PotentialDynamicCollider(entityCollisionResolver, collider, entityID, ref transform);

				Cell cell = cellPool.Get();
				cell.next = cells[x, y];
				cell.potentialCollider = potentialDynamicCollider;
				cells[x, y] = cell;
			}
		}
	}

	public void GetCollidersToTest(EntityColliderSet collider, List<IPotentialCollider> potentialCollisions)
	{
		FindCellBoundsContainingCollider(collider.velocityExpandedBounds, out int minX, out int minY, out int maxX, out int maxY);

		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
			{
				Cell colliderCell = cells[x, y];

				while (colliderCell != null)
				{
					if(!potentialCollisions.Contains(colliderCell.potentialCollider))
						potentialCollisions.Add(colliderCell.potentialCollider);

					colliderCell = colliderCell.next;
				}
			}
		}
	}

	public void ClearDynamics()
	{
		for (int x = 0; x < GRID_DIVISSIONS_WIDTH; x++)
		{
			for (int y = 0; y < GRID_DIVISSIONS_HEIGHT; y++)
			{
				Cell cell = cells[x, y];

				while (cell != null)
				{
					if (cell.potentialCollider is PotentialDynamicCollider)
					{
						cells[x, y] = cell.next;
						cellPool.Free(cell);
					}
					cell = cell.next;
				}
			}
		}
	}
}
