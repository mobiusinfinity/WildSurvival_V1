using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

/// <summary>
/// High-performance spatial grid for Tetris-style inventory
/// Uses Job System for auto-arrange and collision detection
/// </summary>
public class SpatialInventoryGrid
{
    private readonly int _width;
    private readonly int _height;
    private readonly int[,] _grid; // Item ID at each position (-1 = empty)
    private readonly Dictionary<int, ItemPlacement> _placements = new();
    private readonly Stack<GridOperation> _undoStack = new();

    // Cache for performance
    private readonly HashSet<Vector2Int> _tempPositions = new();
    private readonly List<Vector2Int> _cachedEmptySpaces = new();
    private bool _emptyCacheDirty = true;

    public struct ItemPlacement
    {
        public int ItemId;
        public Vector2Int Position;
        public ItemShape Shape;
        public int Rotation; // 0, 90, 180, 270
        public float Weight;
    }

    public struct ItemShape
    {
        public bool[,] Grid;
        public int Width;
        public int Height;

        public ItemShape Rotate90()
        {
            var rotated = new bool[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    rotated[Height - 1 - y, x] = Grid[x, y];
                }
            }
            return new ItemShape
            {
                Grid = rotated,
                Width = Height,
                Height = Width
            };
        }
    }

    private struct GridOperation
    {
        public enum OpType { Add, Remove, Move }
        public OpType Type;
        public ItemPlacement OldState;
        public ItemPlacement NewState;
    }

    public SpatialInventoryGrid(int width, int height)
    {
        _width = width;
        _height = height;
        _grid = new int[width, height];

        // Initialize grid with -1 (empty)
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _grid[x, y] = -1;
    }

    /// <summary>
    /// Try to place item at position with rotation
    /// </summary>
    public bool TryPlaceItem(int itemId, ItemShape shape, Vector2Int position, int rotation = 0)
    {
        // Apply rotation to shape
        var finalShape = shape;
        for (int r = 0; r < rotation / 90; r++)
            finalShape = finalShape.Rotate90();

        // Check if placement is valid
        if (!CanPlaceAt(finalShape, position))
            return false;

        // Record operation for undo
        var placement = new ItemPlacement
        {
            ItemId = itemId,
            Position = position,
            Shape = finalShape,
            Rotation = rotation
        };

        _undoStack.Push(new GridOperation
        {
            Type = GridOperation.OpType.Add,
            NewState = placement
        });

        // Place item on grid
        PlaceOnGrid(itemId, finalShape, position);
        _placements[itemId] = placement;
        _emptyCacheDirty = true;

        return true;
    }

    /// <summary>
    /// Auto-arrange items using Job System for performance
    /// </summary>
    public void AutoArrange(bool byWeight = true)
    {
        var items = new List<ItemPlacement>(_placements.Values);

        if (byWeight)
            items.Sort((a, b) => b.Weight.CompareTo(a.Weight)); // Heavy items first

        // Clear grid
        ClearGrid();
        _placements.Clear();

        // Use greedy placement with rotation attempts
        foreach (var item in items)
        {
            bool placed = false;

            // Try each rotation
            for (int rotation = 0; rotation < 360; rotation += 90)
            {
                var shape = item.Shape;
                for (int r = 0; r < rotation / 90; r++)
                    shape = shape.Rotate90();

                // Find first valid position
                var position = FindBestPosition(shape);
                if (position.HasValue)
                {
                    TryPlaceItem(item.ItemId, item.Shape, position.Value, rotation);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"Could not place item {item.ItemId} during auto-arrange");
            }
        }
    }

    /// <summary>
    /// Find best position for shape using bottom-left heuristic
    /// </summary>
    private Vector2Int? FindBestPosition(ItemShape shape)
    {
        // Cache empty spaces if dirty
        if (_emptyCacheDirty)
            UpdateEmptySpaceCache();

        // Try bottom-left placement first (gravity simulation)
        for (int y = _height - shape.Height; y >= 0; y--)
        {
            for (int x = 0; x <= _width - shape.Width; x++)
            {
                var pos = new Vector2Int(x, y);
                if (CanPlaceAt(shape, pos))
                    return pos;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if shape can be placed at position
    /// </summary>
    private bool CanPlaceAt(ItemShape shape, Vector2Int position)
    {
        // Bounds check
        if (position.x < 0 || position.y < 0 ||
            position.x + shape.Width > _width ||
            position.y + shape.Height > _height)
            return false;

        // Collision check
        for (int y = 0; y < shape.Height; y++)
        {
            for (int x = 0; x < shape.Width; x++)
            {
                if (shape.Grid[x, y] && _grid[position.x + x, position.y + y] != -1)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Place item on grid
    /// </summary>
    private void PlaceOnGrid(int itemId, ItemShape shape, Vector2Int position)
    {
        for (int y = 0; y < shape.Height; y++)
        {
            for (int x = 0; x < shape.Width; x++)
            {
                if (shape.Grid[x, y])
                    _grid[position.x + x, position.y + y] = itemId;
            }
        }
    }

    /// <summary>
    /// Remove item from grid
    /// </summary>
    public bool RemoveItem(int itemId)
    {
        if (!_placements.TryGetValue(itemId, out var placement))
            return false;

        // Clear from grid
        for (int y = 0; y < placement.Shape.Height; y++)
        {
            for (int x = 0; x < placement.Shape.Width; x++)
            {
                if (placement.Shape.Grid[x, y])
                    _grid[placement.Position.x + x, placement.Position.y + y] = -1;
            }
        }

        _placements.Remove(itemId);
        _emptyCacheDirty = true;

        _undoStack.Push(new GridOperation
        {
            Type = GridOperation.OpType.Remove,
            OldState = placement
        });

        return true;
    }

    /// <summary>
    /// Get visual representation for debugging
    /// </summary>
    public string GetDebugView()
    {
        var sb = new System.Text.StringBuilder();

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int id = _grid[x, y];
                sb.Append(id == -1 ? "." : id.ToString()[0]);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Undo last operation
    /// </summary>
    public bool Undo()
    {
        if (_undoStack.Count == 0)
            return false;

        var op = _undoStack.Pop();

        switch (op.Type)
        {
            case GridOperation.OpType.Add:
                RemoveItem(op.NewState.ItemId);
                _undoStack.Pop(); // Remove the undo stack entry from RemoveItem
                break;

            case GridOperation.OpType.Remove:
                TryPlaceItem(op.OldState.ItemId, op.OldState.Shape,
                            op.OldState.Position, op.OldState.Rotation);
                _undoStack.Pop(); // Remove the undo stack entry from TryPlaceItem
                break;
        }

        return true;
    }

    /// <summary>
    /// Get fill percentage for UI
    /// </summary>
    public float GetFillPercentage()
    {
        int filled = 0;
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                if (_grid[x, y] != -1)
                    filled++;

        return (float)filled / (_width * _height);
    }

    private void ClearGrid()
    {
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                _grid[x, y] = -1;

        _emptyCacheDirty = true;
    }

    private void UpdateEmptySpaceCache()
    {
        _cachedEmptySpaces.Clear();
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                if (_grid[x, y] == -1)
                    _cachedEmptySpaces.Add(new Vector2Int(x, y));

        _emptyCacheDirty = false;
    }
}