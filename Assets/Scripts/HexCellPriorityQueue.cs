using System.Collections.Generic;

public class HexCellPriorityQueue {
    public List<HexCell> list = new List<HexCell>();

    private int count = 0;

    private int minimum = int.MaxValue;

    public int Count {
        get { return count; }
    }

    public void Enqueue(HexCell cell) {
        count += 1;
        int priority = cell.SearchPriority;
        if (priority < minimum) {
            minimum = priority;
        }

        while (priority >= list.Count) {
            list.Add(null);
        }

        cell.NexWithSamePriority = list[priority];
        list[priority] = cell;
    }

    public HexCell Dequeue() {
        count -= 1;
        for (; minimum < list.Count; minimum++) {
            HexCell cell = list[minimum];
            if (cell != null) {
                list[minimum] = cell.NexWithSamePriority;
                return cell;
            }
        }

        return null;
    }

    public void Change(HexCell cell, int oldPriority) {
        HexCell current = list[oldPriority];
        HexCell next = current.NexWithSamePriority;
        if (current == cell) {
            list[oldPriority] = next;
        } else {
            // dangerous for null element
            while (next != cell) {
                current = next;
                next = current.NexWithSamePriority;
            }

            current.NexWithSamePriority = cell.NexWithSamePriority;
        }

        Enqueue(cell);
        count -= 1;
    }

    public void Clear() {
        list.Clear();
        count = 0;
        minimum = int.MaxValue;
    }
}