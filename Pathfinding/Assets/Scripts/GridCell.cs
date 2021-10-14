using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameAI.PathFinding;

// We have implemented our generic PathFinder
// and a comcrete AStarPathFinder that is derived
// from our generic path finder.
// Now we will integrate our pathfinder with our
// grid-based map.
// Since GridCell_Viz is a C# class that is derived
// from Monohevarior, we will need a different class
// that derives from GameAI.PathFinding.Node<T>
//
// So we have created a new class called GrideCell.
// We wil derive GridCell from GameAI.PathFinding.Node<Vector2Int>

public class GridCell : Node<Vector2Int>
{
  public bool isWalkable = true;

  // We will need access to the grid to 
  // know grid related parameters such as
  // numX, numY and the 2d array of cells.
  private Grid_Viz mGrid;

  public GridCell(Vector2Int index, Grid_Viz grid)
    : base(index)
  {
    mGrid = grid;
  }

  public override List<Node<Vector2Int>> GetNeighbours()
  {
    // for the we will just revert to the Grid_Viz.
    // Actually there are many ways of implementing it.
    return mGrid.GetNeighbours(Value.x, Value.y);
  }
}
