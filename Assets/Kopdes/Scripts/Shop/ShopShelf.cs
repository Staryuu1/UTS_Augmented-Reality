using UnityEngine;

public sealed class ShopShelf : MonoBehaviour
{
    public string category;
    public int startingStock = 6;
    public Transform packages;
    public Transform browsePoint;

    public int Stock { get; private set; }
    public int Capacity => packages.childCount;

    void Awake() => SetStock(Mathf.Clamp(startingStock, 0, Capacity));

    public bool TakeOne()
    {
        if (Stock == 0) return false;
        SetStock(Stock - 1);
        return true;
    }

    public int Restock(int available)
    {
        int added = Mathf.Clamp(available, 0, Capacity - Stock);
        SetStock(Stock + added);
        return added;
    }

    void SetStock(int quantity)
    {
        Stock = quantity;
        for (int i = 0; i < Capacity; i++)
            packages.GetChild(i).gameObject.SetActive(i < Stock);
    }
}
