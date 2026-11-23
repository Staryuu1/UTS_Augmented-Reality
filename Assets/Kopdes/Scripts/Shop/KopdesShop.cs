using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KopdesShop : MonoBehaviour
{
    public Camera playerView;
    public Collider stockPickup;
    public ShopShelf[] shelves;
    public Animator[] residents;
    public Animator cashier;
    public ShopVehicle[] vehicles;
    public Transform checkoutPackage;

    public const int BoxCapacity = 12;
    public int Carried { get; private set; }
    public int Sales { get; private set; }

    static readonly Vector3 Porch = new(0f, 0f, -6.8f);
    static readonly Vector3 Entrance = new(0f, 0f, -3.5f);
    static readonly Vector3 FrontAisle = new(0f, 0f, -1.1f);
    static readonly Vector3 LeftAisle = new(-3.1f, 0f, -1.1f);
    static readonly Vector3 RearAisle = new(-3.1f, 0f, 2.4f);
    static readonly Vector3 CheckoutTurn = new(2.93f, 0f, -1.1f);
    static readonly Vector3 Register = new(4.9f, 0f, -.68f);

    bool shopping;
    bool trafficMoving;
    int nextShelf;
    Collider target;
    string message;
    float messageUntil;

    void Start()
    {
        if (vehicles.Length != residents.Length || System.Array.Exists(vehicles, vehicle => !vehicle))
            throw new System.InvalidOperationException("Each shop visitor needs an assigned vehicle.");
        for (int i = 0; i < residents.Length; i++)
            StartCoroutine(Visit(residents[i], vehicles[i], i));
    }

    void Update()
    {
        target = null;
        if (!playerView || Cursor.lockState != CursorLockMode.Locked) return;

        Ray ray = new(playerView.transform.position, playerView.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, 3.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return;

        target = hit.collider;
        if (Keyboard.current?.eKey.wasPressedThisFrame != true) return;

        if (target == stockPickup)
        {
            Feedback(CollectStock() ? $"Stok diambil ({Carried}/{BoxCapacity})." : "Tas stok sudah penuh.");
        }
        else if (target.TryGetComponent(out ShopShelf shelf))
        {
            int added = FillShelf(shelf);
            Feedback(added > 0 ? $"{added} barang ditambahkan." :
                shelf.Stock == shelf.Capacity ? "Rak sudah penuh." : "Ambil stok di gudang belakang.");
        }
    }

    public bool CollectStock()
    {
        if (Carried == BoxCapacity) return false;
        Carried = BoxCapacity;
        return true;
    }

    public int FillShelf(ShopShelf shelf)
    {
        if (System.Array.IndexOf(shelves, shelf) < 0) return 0;
        int added = shelf.Restock(Carried);
        Carried -= added;
        return added;
    }

    void Feedback(string text)
    {
        message = text;
        messageUntil = Time.time + 3;
    }

    void OnGUI()
    {
        GUI.Box(new Rect(24, 18, 248, 47), $"KOPDES\nStok dibawa  {Carried}/{BoxCapacity}     Terjual  {Sales}");
        if (Cursor.lockState != CursorLockMode.Locked) return;

        string prompt = null;
        if (target == stockPickup)
            prompt = Carried == BoxCapacity ? "Tas stok sudah penuh" : "E  Ambil stok gudang";
        else if (target && target.TryGetComponent(out ShopShelf shelf))
        {
            string action = shelf.Stock == shelf.Capacity ? "Rak penuh" :
                Carried == 0 ? "Ambil stok di gudang belakang" : "E  Isi rak";
            prompt = $"{shelf.category}  {shelf.Stock}/{shelf.Capacity}   |   {action}";
        }

        if (prompt != null) GUI.Box(new Rect(Screen.width / 2f - 174, Screen.height / 2f + 24, 348, 30), prompt);
        if (Time.time < messageUntil) GUI.Box(new Rect(Screen.width / 2f - 146, Screen.height / 2f + 61, 292, 26), message);
    }

    IEnumerator Visit(Animator resident, ShopVehicle vehicle, int index)
    {
        resident.gameObject.SetActive(false);
        vehicle.gameObject.SetActive(false);
        yield return new WaitForSeconds(index * 7f);
        int trip = index;

        while (true)
        {
            // The road and driveway share one scripted traffic lane.
            while (trafficMoving) yield return null;
            trafficMoving = true;
            vehicle.Spawn(index == 0 ? -43f : -62f);
            vehicle.gameObject.SetActive(true);
            bool stop = trip++ % 3 == 0 && System.Array.Exists(shelves, shelf => shelf.Stock > 0);
            yield return stop ? vehicle.Arrive() : vehicle.PassBy();
            trafficMoving = false;
            if (!stop)
            {
                vehicle.gameObject.SetActive(false);
                yield return new WaitForSeconds(9f + index * 3f);
                continue;
            }

            resident.transform.SetPositionAndRotation(vehicle.Door, Quaternion.LookRotation(Vector3.forward));
            resident.gameObject.SetActive(true);
            Vector3 waitingPoint = new((index - 1) * 1.35f, .04f, -7.1f - .45f * (index % 2));
            yield return Walk(resident, vehicle.ApproachDoor, waitingPoint);
            resident.CrossFadeInFixedTime("Idle", .18f);

            while (shopping) yield return null;
            ShopShelf shelf = ChooseShelf();
            if (shelf)
            {
                shopping = true;
                yield return Shop(resident, shelf);
                shopping = false;
            }

            yield return Walk(resident, vehicle.ApproachDoor, vehicle.Door);
            resident.gameObject.SetActive(false);
            while (trafficMoving) yield return null;
            trafficMoving = true;
            yield return vehicle.Depart();
            vehicle.gameObject.SetActive(false);
            trafficMoving = false;
            yield return new WaitForSeconds(10f + index * 3f);
        }
    }

    ShopShelf ChooseShelf()
    {
        for (int offset = 0; offset < shelves.Length; offset++)
        {
            int index = (nextShelf + offset) % shelves.Length;
            if (shelves[index].Stock == 0) continue;
            nextShelf = (index + 1) % shelves.Length;
            return shelves[index];
        }
        return null;
    }

    IEnumerator Shop(Animator resident, ShopShelf shelf)
    {
        Vector3 point = shelf.browsePoint.position;
        bool rearRow = point.z > 0;
        yield return Walk(resident, Porch, Entrance, FrontAisle);
        if (rearRow) yield return Walk(resident, LeftAisle, RearAisle);
        yield return Walk(resident, point);
        resident.transform.rotation = shelf.browsePoint.rotation;
        resident.CrossFadeInFixedTime("Idle", .12f);
        yield return new WaitForSeconds(.8f);
        if (!shelf.TakeOne())
            throw new System.InvalidOperationException("Selected shelf has no stock.");

        if (rearRow) yield return Walk(resident, RearAisle, LeftAisle);
        yield return Walk(resident, FrontAisle, CheckoutTurn, Register);
        resident.transform.rotation = Quaternion.LookRotation(Vector3.back);
        resident.CrossFadeInFixedTime("Idle", .12f);
        yield return ScanPurchase();
        yield return Walk(resident, CheckoutTurn, FrontAisle, Entrance, Porch);
    }

    IEnumerator ScanPurchase()
    {
        cashier.transform.LookAt(new Vector3(Register.x, cashier.transform.position.y, Register.z));
        checkoutPackage.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.1f);
        checkoutPackage.gameObject.SetActive(false);
        Sales++;
    }

    IEnumerator Walk(Animator actor, params Vector3[] route)
    {
        actor.CrossFadeInFixedTime("Walk", .12f);
        foreach (Vector3 waypoint in route)
        {
            Vector3 position = actor.transform.position;
            Vector3 horizontal = new(waypoint.x, position.y, waypoint.z);
            while ((horizontal - position).sqrMagnitude > .001f)
            {
                Vector3 direction = horizontal - position;
                direction.y = 0;
                actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation, Quaternion.LookRotation(direction), 290f * Time.deltaTime);
                position = Vector3.MoveTowards(position, new Vector3(waypoint.x, position.y, waypoint.z), 1.35f * Time.deltaTime);
                if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit floor, 1.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    position.y = floor.point.y;
                actor.transform.position = position;
                horizontal.y = position.y;
                yield return null;
            }
        }
    }
}
