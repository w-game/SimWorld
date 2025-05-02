using System.Collections.Generic;
using UnityEngine;

public class QuadTree<T>
{
    private Rect boundary;
    private int capacity;
    private List<(Vector2 pos, T item)> items;
    private bool divided = false;

    private QuadTree<T> northeast;
    private QuadTree<T> northwest;
    private QuadTree<T> southeast;
    private QuadTree<T> southwest;

    public QuadTree(Rect boundary, int capacity = 4)
    {
        this.boundary = boundary;
        this.capacity = capacity;
        this.items = new List<(Vector2, T)>();
    }

    public bool Insert(Vector2 pos, T item)
    {
        if (!boundary.Contains(pos)) return false;

        if (items.Count < capacity)
        {
            items.Add((pos, item));
            return true;
        }

        if (!divided) Subdivide();

        return northeast.Insert(pos, item) || northwest.Insert(pos, item)
            || southeast.Insert(pos, item) || southwest.Insert(pos, item);
    }

    public List<T> Query(Rect range)
    {
        List<T> found = new List<T>();
        if (!boundary.Overlaps(range)) return found;

        foreach (var (pos, item) in items)
        {
            if (range.Contains(pos)) found.Add(item);
        }

        if (divided)
        {
            found.AddRange(northeast.Query(range));
            found.AddRange(northwest.Query(range));
            found.AddRange(southeast.Query(range));
            found.AddRange(southwest.Query(range));
        }

        return found;
    }

    private void Subdivide()
    {
        float x = boundary.x;
        float y = boundary.y;
        float w = boundary.width / 2f;
        float h = boundary.height / 2f;

        northeast = new QuadTree<T>(new Rect(x + w, y, w, h), capacity);
        northwest = new QuadTree<T>(new Rect(x, y, w, h), capacity);
        southeast = new QuadTree<T>(new Rect(x + w, y + h, w, h), capacity);
        southwest = new QuadTree<T>(new Rect(x, y + h, w, h), capacity);

        divided = true;
    }
}