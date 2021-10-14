using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell_Viz : MonoBehaviour
{
  // the index to the 2d array of grid cells.
  //public Vector2Int index;
  public GridCell Cell { get; set; }

  [SerializeField]
  SpriteRenderer outerBox;
  [SerializeField]
  SpriteRenderer innerBox;

  //public bool isWalkable = true;

  public void SetOuterColor(Color col)
  {
    outerBox.color = col;
  }

  public void SetInnerColor(Color col)
  {
    innerBox.color = col;
  }
}
