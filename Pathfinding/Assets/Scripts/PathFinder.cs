using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Now we will implement the actual pathfinder.
// Our objective is to make a generic pathfinder
// and then bind our grid-based problem to the 
// pathfinder.
//
// For ease of this tutorial I will implement
// all necessary classes into this file.
// Later we can refactor.
// we will also put pathfinding related classes into GameAI and 
// PathFinding namespaces.

namespace GameAI
{
  namespace PathFinding
  {
    // First of all we wil implement the enumeration
    // types that represent the status of pathfinding
    // search.
    public enum PathFinderStatus
    {
      NOT_INIIALIZED,
      SUCCESS,
      FAILURE,
      RUNNING,
    }

    // Now we will implement a generic Node type
    // This Node type will represent a cell in the map.
    // For our grid-based map, the GridCell or any 
    // other data type that represents a cell in the 
    // grid can be a node.
    abstract public class Node<T>
    {
      // Store the value of T.
      public T Value { get; private set; }

      // constructor.
      public Node(T value)
      {
        Value = value;
      }

      // now we define an abstract function
      // that returns the neighbours of this 
      // node. All derived nodes must implement
      // this function.
      abstract public List<Node<T>> GetNeighbours();
    }

    abstract public class PathFinder<T>
    {
      #region class PathFinderNode
      // We implement a node for the path
      // finding search tree.
      // class PathFinderNode.
      // NOTE: Do not confuse this class with the above
      // Node class.
      // This class encapsulates a pathfinding search tree
      // node and holds other properties required for 
      // pathfinding.
      public class PathFinderNode
      {
        // we need to have a reference to the parent node.
        public PathFinderNode Parent { get; set; }

        // we need to have a reference to the map
        // node that it encapsulates.
        public Node<T> Location
        {
          get;
          set;
        }

        // we also keep all different costs 
        // associated with pathinding
        // H = heuristic cost to the destination/goal.
        // G = the cost till this node from the start.
        // F = G + H
        public float Fcost { get; private set; }
        public float Hcost { get; private set; }
        public float Gcost { get; private set; }

        // constructor.
        public PathFinderNode(Node<T> node, PathFinderNode parent, float G, float H)
        {
          Location = node;
          Parent = parent;
          Hcost = H;

          // We need to create a separate function
          // to set G cost as this cost can be 
          // updated at runtime.
          // Set Astar implementation for details.
          SetGCost(G);
        }

        public void SetGCost(float g)
        {
          Gcost = g;
          Fcost = Hcost + Gcost;
        }
      }
      #endregion

      #region Cost functions related
      // Use function callbacks/delegates to calculate cost.
      // Why? Because we do not want to hardcode our cost 
      // calculation to this pathfinder. 
      // We want the application to be responsible for 
      // cost calculation.
      // The application can decide whether to use 
      // Manhattan cost, Eucledean cost or any other
      // cost function. The cost function can vary based
      // on application domain.
      // Since we are making a generic pathfinder
      // we should allow the flexibility to let the caller
      // decide on the cost function.

      // The delegate for heuristic cost calculation.
      public delegate float CostFunction(T a, T b);

      //// The delegate for node travversal cost. This means
      //// the cost of traversing from one node to another.
      //public delegate float NodeTraversalCostFunction(T a, T b);

      // create two variables to associate these two delegates.
      public CostFunction HeuristicCost;
      public CostFunction NodeTraversalCost;
      #endregion

      #region Open and closed list related functionality
      // Now we implement the open and the closed lists.
      // The open list is used to store the nodes that
      // are yet to be explored. The closed list stores to 
      // nodes that are already searched.
      // For simplicity we wil use List to implement
      // these two lists.
      protected List<PathFinderNode> mOpenList = new List<PathFinderNode>();
      protected List<PathFinderNode> mClosedList = new List<PathFinderNode>();

      // we now implement a couple of helper methods.
      // The first method will return the least cost
      // item from the open list. Remember, we will need
      // to traverse the lest cost item from the openlist.
      // We can use a priority queue. However, for our
      // case we will simply search the list and return the 
      // lest cost item.
      protected PathFinderNode GetLeastCostNode(List<PathFinderNode> list)
      {
        int best_index = 0;
        float best_cost = list[0].Fcost;

        for(int i = 1; i < list.Count; ++i)
        {
          if(best_cost > list[i].Fcost)
          {
            best_cost = list[i].Fcost;
            best_index = i;
          }
        }
        return list[best_index];
      }

      // Next we implement a function that checks
      // and return the index of an item that is equal
      // to another item.
      // We return the index if an item is found
      // else we return -1;
      protected int IsInList(List<PathFinderNode> list, T item)
      {
        for (int i = 0; i < list.Count; ++i)
        {
          if (EqualityComparer<T>.Default.Equals(list[i].Location.Value, item))
          {
            // item found.
            return i;
          }
        }

        // item not found.
        return -1;
      }
      #endregion

      #region Delegates to handle state changes to PathFinderNode
      // We will now create a few function callbacks
      // or delegates to allow a flexibility for the 
      // caller (or the application domain) to handle
      // changes duing the path finding. For example,
      // the caller may decide to show a different colour
      // for a cell when it is added to the open list or 
      // a different colour when added to the closed list.

      // I cant think of a better name.
      public delegate void DelegatePathFinderNode(PathFinderNode node);
      public DelegatePathFinderNode onChangeCurrentNode;
      public DelegatePathFinderNode onAddToOpenList;
      public DelegatePathFinderNode onAddToClosedList;
      public DelegatePathFinderNode onDestinationFound;
      #endregion

      // We have implement most of the scaffolds 
      // required for our pathfinding.

      // The start and the destination/goal nodes.
      public Node<T> Start { get; private set; }
      public Node<T> Goal { get; private set; }

      protected PathFinderNode mCurrentNode = null;

      // Add the followimg property so that we
      // can have a readonly access to the currentNode 
      // during path finding.
      public PathFinderNode CurrentNode
      {
        get
        {
          return mCurrentNode;
        }
      }

      // A reset method to reset existing pathfinding
      // results.
      public void Reset()
      {
        // we should not reset when a pathfinding
        // is already in progress.
        if(Status == PathFinderStatus.RUNNING)
        {
          // do not reset.
          return;
        }
        mOpenList.Clear();
        mClosedList.Clear();
        Status = PathFinderStatus.NOT_INIIALIZED;
      }

      // Okay we are almost done.
      // We shall now proceed to implement the actual
      // pathfinding based on Astar, Djikstra and Greedy-best 
      // first pathfnding algrithm.

      // We will achieve our pathfinding in two stages.
      // The first stage we wil initilize
      // The second stage will be the Step method
      // that will be called continuously until the
      // pathfinder returns either the SUCCESS or FAILURE
      // status.

      // The initialize method.
      public bool Initialize(Node<T> start, Node<T> goal)
      {
        if(Status == PathFinderStatus.RUNNING)
        {
          // we cannot initialize if an exiting
          // pathfinding search is in progress.
          return false;
        }

        // We reset first.
        Reset();

        Start = start;
        Goal = goal;

        // calculate the heuristic cost from start to goal.
        // the heuristic cost function cannot be null.
        float H = HeuristicCost(Start.Value, Goal.Value);

        // We now create a root node for our search tree.
        // Parent = null since we do not have any parent for the root node.
        // G cost = 0.0f since we have not started our traversal yet.
        PathFinderNode root = new PathFinderNode(Start, null, 0.0f, H);

        // Now we add this node to our open list because we 
        // want to start exploring.
        mOpenList.Add(root);

        // We set the current node to be the root node.
        mCurrentNode = root;

        // We call the delegate onAddToOpenList and
        // onChangeCurrentNode.
        onAddToOpenList?.Invoke(mCurrentNode);
        onChangeCurrentNode?.Invoke(mCurrentNode);

        // set the status to RUNNING.
        Status = PathFinderStatus.RUNNING;
        return true;
      }

      // Now we will implement the Step function.
      // Why Step function and not the while loop?
      // Well, as mentioned earlier, we want to 
      // provide the flexibility to the caller.
      // The caller can then choose to call this
      // Step method within a while loop in a coroutine
      // or create threads etc.
      public PathFinderStatus Step()
      {
        // we are going to explore the current node.
        // we can add the current node to our closed list now.
        mClosedList.Add(mCurrentNode);

        // we call the delegate to inform the change.
        onAddToClosedList?.Invoke(mCurrentNode);

        // we now check of the open list is empty.
        if(mOpenList.Count == 0)
        {
          // we have failed in our search
          // since we exhaused all our search space.
          Status = PathFinderStatus.FAILURE;

          return Status;
        }

        // Now we get the least cost item from our open list.
        // and make that as the current node.
        mCurrentNode = GetLeastCostNode(mOpenList);
        // inform the change
        onChangeCurrentNode?.Invoke(mCurrentNode);

        // remove this node from the open list.
        mOpenList.Remove(mCurrentNode);

        // Check if this current node is the goal.
        if(EqualityComparer<T>.Default.Equals(mCurrentNode.Location.Value, Goal.Value))
        {
          // we have found our goal.
          Status = PathFinderStatus.SUCCESS;
          onDestinationFound?.Invoke(mCurrentNode);
          return Status;
        }

        // We have not found our solution yet. Let's eplore further.
        // Get the neighbours.
        List<Node<T>> neighbours = mCurrentNode.Location.GetNeighbours();

        // we traverse each of these neighbours.
        // I am back.
        // 
        foreach(Node<T> cell in neighbours)
        {
          // we want to separate our actual implementation 
          // in different classes based on a specific algorithm.
          AlgorithmSpecificImplementation(cell);
        }

        Status = PathFinderStatus.RUNNING;
        return Status;
      }

      abstract protected void AlgorithmSpecificImplementation(Node<T> cell);

      // We create a variable to keep the
      // pathfinding status. We decided not to use
      // a variable at all.
      public PathFinderStatus Status
      {
        get;
        private set;
      }
    }

    // Now we create concrete implementation of
    // specific algorithm types.
    public class AStarPathFinder<T> : PathFinder<T>
    {
      // In this class we only need to implement
      // the AlgorithmSpecificImplementation function.
      protected override void AlgorithmSpecificImplementation(Node<T> cell)
      {
        // first of all check if the node is
        // already in the closed list.
        // if so then we do not need to continue search
        // for this node.
        if(IsInList(mClosedList, cell.Value) == -1)
        {
          // the cel does not exist in the closed list.
          // We will now calculate the G cost and the H cost.
          // Remember G cost is the cost from the start to 
          // the current node.
          // To get the G cost we will need to get the G cost of the
          // current node and add the travesal cost from current
          // node to it's neighbour which is represented by cell.
          float G = mCurrentNode.Gcost + 
            NodeTraversalCost(mCurrentNode.Location.Value, cell.Value);

          // Calculate the heuristic cost from the cell to the goal.
          float H = HeuristicCost(cell.Value, Goal.Value);

          // Next we check if the cell is already in the open list.
          int idOlist = IsInList(mOpenList, cell.Value);
          if(idOlist == -1)
          {
            // the cell does not exist in the open list.
            // We will add the cell to the open list.
            PathFinderNode n = new PathFinderNode(cell, mCurrentNode, G, H);
            mOpenList.Add(n);

            // call the delegate
            onAddToOpenList?.Invoke(n);
          }
          else
          {
            // the cell already exists in the open list.
            // we need to check the G cost of the existing
            // node. If the new G cost is less than the one
            // already in the openlist then we will need to
            // update the G cost.
            float oldG = mOpenList[idOlist].Gcost;
            if(G < oldG)
            {
              // update the G cost.
              mOpenList[idOlist].SetGCost(G);
              // update the new parent to current node.
              mOpenList[idOlist].Parent = mCurrentNode;

              // call the delegate.
              onAddToOpenList?.Invoke(mOpenList[idOlist]);
            }
          }
        }
      }
    }
  }
}
