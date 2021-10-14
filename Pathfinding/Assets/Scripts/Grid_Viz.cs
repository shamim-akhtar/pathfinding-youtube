using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameAI.PathFinding;

public class Grid_Viz : MonoBehaviour
{
  public int numX; // columns
  public int numY; // rows

  // the prefab to the grid cell
  [SerializeField]
  GameObject gridCellPrefab;

  // Allow some color selections
  [SerializeField]
  Color WALKABLE_COLOR = new Color(120.0f/255.0f, 156.0f/255.0f, 238.0f/255.0f, 1.0f);
  [SerializeField]
  Color NON_WALKABLE_COLOR = new Color(0.1f, 0.1f, 0.1f, 1.0f);

  [SerializeField]
  Color ADD_TO_OPEN_LIST = new Color(0.1f, 0.7f, 0.2f, 1.0f);
  [SerializeField]
  Color ADD_TO_CLOSED_LIST = new Color(0.3f, 0.4f, 0.8f, 1.0f);
  [SerializeField]
  Color CURRENT_NODE = new Color(0.6f, 0.5f, 0.1f, 1.0f);

  [SerializeField]
  NPC npc;

  private GridCell_Viz[,] mGridCells;

  GridCell mStartLocation;

  void Start()
  {
    CreateGrid();
  }

  void CreateGrid()
  {
    // initialize the 2d array.
    mGridCells = new GridCell_Viz[numX, numY];

    // iterate through the number of colums
    for (int i = 0; i < numX; ++i)
    {
      // iterate through the number of rows.
      for(int j = 0; j < numY; ++j)
      {
        // create a grid cell.
        // we are creating a gridcell just for
        // visualizing our grid for pathfinding.
        // in actual implementation we only need to
        // create a 2d array without any game object
        // associated with it.

        GameObject obj = Instantiate(gridCellPrefab, new Vector3(i, j, 0.0f), Quaternion.identity);
        obj.name = "cell_" + i.ToString() + "_" + j.ToString();
        obj.transform.SetParent(transform);
        mGridCells[i,j] = obj.GetComponent<GridCell_Viz>();
        //mGridCells[i, j].index = new Vector2Int(i, j);
        mGridCells[i, j].Cell = new GridCell(new Vector2Int(i, j), this);
      }
    }

    mStartLocation = mGridCells[0, 0].Cell;
    npc.transform.position = new Vector3(
      mGridCells[0, 0].transform.position.x,
      mGridCells[0, 0].transform.position.y,
      npc.transform.position.z); // we use the orizinal z for the npc.
  }

  // Update is called once per frame
  void Update()
  {
    // lets add a functionilty to change the 
    // color of the grid cell to black.
    // Later on we will use this to toggle
    // a grid cell walkable and non-walkable.
    if (Input.GetMouseButtonDown(0))
    {
      HandleToggleWalkable();
    }

    // use right mouse button click to 
    // handle npc moveto.
    if (Input.GetMouseButtonDown(1))
    {
      HandleNPCMoveTo();
    }
  }

  void HandleNPCMoveTo()
  {
    Vector2 rayPos = new Vector2(
      Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
      Camera.main.ScreenToWorldPoint(Input.mousePosition).y);

    RaycastHit2D hit = Physics2D.Raycast(
      rayPos, 
      Vector2.zero, 0.0f);

    if (hit)
    {
      GameObject hitObj = 
        hit.transform.gameObject;

      GridCell_Viz cellViz = 
        hitObj.GetComponent<GridCell_Viz>();
      if (cellViz)
      {
        //npc.AddWayPoint(
        //  cellViz.transform.position.x, 
        //  cellViz.transform.position.y);
        StartCoroutine(
          Coroutine_FindPathAndMove(
            mStartLocation, 
            cellViz.Cell, 
            npc));
      }
    }
  }

  // we have completed pathfinding.
  // Now we will do some visualisation
  // based on our pathfinding progress.
  public void OnAddToOpenList(PathFinder<Vector2Int>.PathFinderNode node)
  {
    int x = node.Location.Value.x;
    int y = node.Location.Value.y;
    mGridCells[x, y].SetInnerColor(ADD_TO_OPEN_LIST);
  }
  public void OnAddToClosedList(PathFinder<Vector2Int>.PathFinderNode node)
  {
    int x = node.Location.Value.x;
    int y = node.Location.Value.y;
    mGridCells[x, y].SetInnerColor(ADD_TO_CLOSED_LIST);
  }
  public void OnSetCurrentNode(PathFinder<Vector2Int>.PathFinderNode node)
  {
    int x = node.Location.Value.x;
    int y = node.Location.Value.y;
    mGridCells[x, y].SetInnerColor(CURRENT_NODE);
  }

  public void ResetColors()
  {
    for(int i = 0; i < numX; ++i)
    {
      for(int j = 0; j < numY; ++j)
      {
        if(mGridCells[i,j].Cell.isWalkable)
        {
          mGridCells[i, j].SetInnerColor(WALKABLE_COLOR);
        }
        else
        {
          mGridCells[i, j].SetInnerColor(NON_WALKABLE_COLOR);
        }
      }
    }
  }

  IEnumerator Coroutine_FindPathAndMove(
    GridCell start,
    GridCell goal,
    NPC obj)
  {
    // Before every pathfinding we reset the color
    // for all the cells.
    ResetColors();

    // This is where we will use our pathfinder.
    // hope that it works.
    // it has been a long coding without testing.
    AStarPathFinder<Vector2Int> pathFinder = new AStarPathFinder<Vector2Int>();
    pathFinder.HeuristicCost = ManhattanCostFunc;
    pathFinder.NodeTraversalCost = EuclideanCostFunc;

    // set the delegates to change colors of the cells
    // as pathfinding progresses.
    pathFinder.onAddToClosedList = OnAddToClosedList;
    pathFinder.onAddToOpenList = OnAddToOpenList;
    pathFinder.onChangeCurrentNode = OnSetCurrentNode;

    // Initialize the pathfinder.
    pathFinder.Initialize(start, goal);

    PathFinderStatus status = pathFinder.Status;
    while(status == PathFinderStatus.RUNNING)
    {
      // thie where we will keep calling
      // the step function until the path 
      // finding is either success or failure.
      status = pathFinder.Step();
      yield return new WaitForSeconds(0.1f);
    }

    if(status == PathFinderStatus.SUCCESS)
    {
      // found path.
      // Accumulate the nodes. 
      // remember the path is in reverse direction
      // that means from the goal to the start.
      List<Vector2Int> reverse_path = new List<Vector2Int>();
      PathFinder<Vector2Int>.PathFinderNode node = pathFinder.CurrentNode;

      // we traverse up the node till we reach null
      // if you remember null is the root node's parent.
      while(node != null)
      {
        reverse_path.Add(node.Location.Value);
        node = node.Parent;
      }

      // we now have the reverse path.
      // traverse this reverse array and add way
      // points to the npc.
      for(int i = reverse_path.Count - 1; i >= 0; i--)
      {
        Vector2Int index = reverse_path[i];
        Vector3 pos = mGridCells[index.x, index.y].transform.position;
        obj.AddWayPoint(pos.x, pos.y);
      }
      mStartLocation = goal;
    }
    if(status == PathFinderStatus.FAILURE)
    {
      Debug.Log("cannot find path");
    }
  }

  // Implement the cost functions.
  public static float ManhattanCostFunc(
    Vector2Int a, 
    Vector2Int b)
  {
    return 
      Mathf.Abs(a.x - b.x) +
      Mathf.Abs(a.y - b.y);
  }

  public static float EuclideanCostFunc(
    Vector2Int a, 
    Vector2Int b)
  {
    return (a - b).magnitude;
  }

  void HandleToggleWalkable()
  {
    Vector2 rayPos = new Vector2(
      Camera.main.ScreenToWorldPoint(Input.mousePosition).x,
      Camera.main.ScreenToWorldPoint(Input.mousePosition).y);

    RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero, 0.0f);
    if (hit)
    {
      GameObject hitObj = hit.transform.gameObject;
      GridCell_Viz cellViz = hitObj.GetComponent<GridCell_Viz>();
      if (cellViz)
      {
        ToggleWalkable(cellViz);
      }
    }
  }

  void ToggleWalkable(GridCell_Viz cv)
  {
    cv.Cell.isWalkable = !cv.Cell.isWalkable;
    if (cv.Cell.isWalkable)
    {
      cv.SetInnerColor(WALKABLE_COLOR);
    }
    else
    {
      cv.SetInnerColor(NON_WALKABLE_COLOR);
    }
  }

  // GetNeighbours 
  public List<Node<Vector2Int>> GetNeighbours(int xx, int yy)
  {
    //
    List<Node<Vector2Int>> neighbours = new List<Node<Vector2Int>>();
  
    //Top.
    if(yy < numY - 1)
    {
      int y = yy + 1;
      int x = xx;
      if(mGridCells[x,y].Cell.isWalkable)
      {
        neighbours.Add(mGridCells[x, y].Cell);
      }
    }
    //Top-right.
    if (yy < numY - 1 && xx < numX - 1)
    {
      int y = yy + 1;
      int x = xx + 1;
      if (mGridCells[x, y].Cell.isWalkable)
      {
        neighbours.Add(mGridCells[x, y].Cell);
      }
    }
    //Right.
    if (xx < numX - 1)
    {
      int y = yy;
      int x = xx + 1;
      if (mGridCells[x, y].Cell.isWalkable)
      {
        neighbours.Add(mGridCells[x, y].Cell);
      }
    }
    //Right-down.
    if (yy > 0 && xx < numX - 1)
    {
      int y = yy - 1;
      int x = xx + 1;
      if (mGridCells[x, y].Cell.isWalkable)
      {
        neighbours.Add(mGridCells[x, y].Cell);
      }
    }
    //Down
    if (yy > 0)
    {
      int y = yy - 1;
      int x = xx;
      if (mGridCells[x, y].Cell.isWalkable)
      {
        neighbours.Add(mGridCells[x, y].Cell);
      }
    }
    //Down-left
    if (yy > 0 && xx > 0)
    {
      int y = yy - 1;
      int x = xx - 1;
      if (mGridCells[x, y].Cell.isWalkable)
      {
        neighbours.Add(mGridCells[x, y].Cell);
      }
    }
    //Left
    if (xx > 0)
    {
      int y = yy;
      int x = xx - 1;
      if (mGridCells[x, y].Cell.isWalkable)
      {
        neighbours.Add(mGridCells[x, y].Cell);
      }
    }
    //Top-left
    if (yy < numY - 1 && xx > 0)
    {
      int y = yy + 1;
      int x = xx - 1;
      if (mGridCells[x, y].Cell.isWalkable)
      {
        neighbours.Add(mGridCells[x, y].Cell);
      }
    }

    // check again. This function is very prone
    // to error.
    return neighbours;
  }
}
