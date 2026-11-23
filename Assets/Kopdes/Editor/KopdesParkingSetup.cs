using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KopdesParkingSetup
{
    const string World = "Assets/Kopdes/World";
    const string MeshFolder = World + "/Vehicles";

    [MenuItem("Kopdes/Install Parking")]
    public static void Install()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != KopdesShopSetup.ScenePath)
            throw new InvalidOperationException("Open the Kopdes scene before installing its parking.");
        Apply();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the Kopdes parking.");
        AssetDatabase.SaveAssets();
    }

    public static void Apply()
    {
        var shop = UnityEngine.Object.FindAnyObjectByType<KopdesShop>();
        var player = UnityEngine.Object.FindAnyObjectByType<KopdesPlayer>();
        if (!shop || !player) throw new InvalidOperationException("The Kopdes shop and player must be present.");
        var existingParking = GameObject.Find("Parking • Kopdes");
        if (existingParking)
        {
            shop.vehicles = existingParking.GetComponentsInChildren<ShopVehicle>(true);
            MarkBays(existingParking.transform, shop.vehicles);
            EditorUtility.SetDirty(shop);
            return;
        }

        var checkout = GameObject.Find("Checkout").transform;
        var counterCenter = new Vector3(5, 0, -1.9f);
        checkout.RotateAround(counterCenter, Vector3.up, 180f);
        shop.cashier.transform.SetPositionAndRotation(new Vector3(4.88f, .31f, -3.05f), Quaternion.identity);
        shop.checkoutPackage.transform.position = new Vector3(4.88f, 1.57f, -1.94f);

        player.walkSpeed = 2.45f;
        player.runSpeed = 4.7f;
        var controller = shop.cashier.runtimeAnimatorController as AnimatorController;
        if (!controller) throw new InvalidOperationException("The character controller needs a Walk state.");
        var walk = controller.layers[0].stateMachine.states.Single(item => item.state.name == "Walk").state;
        walk.speed = .76f;
        EditorUtility.SetDirty(walk);

        var lot = new GameObject("Parking • Kopdes").transform;
        var gravel = Material("Gravel");
        Box(lot, "Compacted gravel • cars", new Vector3(-7.2f, .021f, -11.14f), new Vector3(9.8f, .062f, 8.5f), gravel, true);
        Box(lot, "Compacted gravel • motorcycle", new Vector3(6.7f, .022f, -10.0f), new Vector3(4.4f, .06f, 5.6f), gravel, true);

        float[] positions = { -5.3f, -8.55f, 6.7f };
        shop.vehicles = new ShopVehicle[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            bool motorcycle = i == 2;
            float x = positions[i];
            Vector3 spot = new(x, .06f, motorcycle ? -9.55f : -9.58f);
            var bay = Child(lot, motorcycle ? "Motorcycle bay" : "Car bay " + (i + 1), spot);
            var actor = Child(lot, motorcycle ? "Motorcycle • resident 3" : "Car • resident " + (i + 1), spot);
            var vehicle = actor.gameObject.AddComponent<ShopVehicle>();
            shop.vehicles[i] = vehicle;
            vehicle.parkingBay = bay;
            if (motorcycle) Scooter(vehicle);
            else Car(vehicle, i == 0 ? Material("Blue") : Material("WarehouseTrim"));
        }
        MarkBays(lot, shop.vehicles);
        EditorUtility.SetDirty(shop);
        EditorUtility.SetDirty(player);
    }

    static void MarkBays(Transform lot, ShopVehicle[] vehicles)
    {
        if (lot.Find("Bay markings")) return;
        foreach (var old in lot.Cast<Transform>().Where(item =>
            item.name == "Bay line" || item.name == "Wheel stop" ||
            item.name == "Parking signpost" || item.name == "Parking signboard" || item.name == "Parking directions").ToArray())
            Undo.DestroyObjectImmediate(old.gameObject);

        var marks = Child(lot, "Bay markings", lot.position);
        var paint = Material("RoadWhite");
        var concrete = Material("Tile");
        float[] cars = vehicles.Where(vehicle => !vehicle.rider).Select(vehicle => vehicle.parkingBay.position.x).OrderBy(x => x).ToArray();
        float motor = vehicles.Single(vehicle => vehicle.rider).parkingBay.position.x;
        float halfWidth = (cars[1] - cars[0]) / 2f;

        foreach (float x in new[] { cars[0] - halfWidth, cars[0] + halfWidth, cars[1] + halfWidth })
            Box(marks, "Car bay line", new Vector3(x, .058f, -9.64f), new Vector3(.07f, .012f, 4.7f), paint);
        foreach (float x in new[] { motor - .82f, motor + .82f })
            Box(marks, "Motorcycle bay line", new Vector3(x, .058f, -9.62f), new Vector3(.07f, .012f, 2.65f), paint);

        foreach (float x in cars)
            Box(marks, "Car wheel stop", new Vector3(x, .10f, -7.28f), new Vector3(1.58f, .10f, .17f), concrete, true);
        Box(marks, "Motorcycle wheel stop", new Vector3(motor, .10f, -8.35f), new Vector3(.63f, .10f, .17f), concrete, true);
    }

    static void Car(ShopVehicle vehicle, Material paint)
    {
        Transform car = vehicle.transform;
        var rubber = Material("Hair");
        var metal = Material("Steel");
        var glass = Material("VentDark");
        var white = Material("ACWhite");
        var red = Material("Red");
        MeshPart(car, "Sculpted body", Shell("CarBody", new[]
        {
            new Vector4(.80f, 1.94f, .32f, 0),
            new Vector4(.91f, 2.04f, .45f, 0),
            new Vector4(.91f, 2.01f, .72f, 0),
            new Vector4(.78f, 1.94f, .93f, -.03f),
            new Vector4(.70f, 1.75f, 1.01f, -.12f)
        }), paint);
        MeshPart(car, "Dark glass cabin", Shell("CarCabin", new[]
        {
            new Vector4(.77f, 1.40f, .94f, -.21f),
            new Vector4(.70f, 1.11f, 1.27f, -.25f),
            new Vector4(.65f, 1.02f, 1.39f, -.28f)
        }), glass);
        MeshPart(car, "Painted roof", Shell("CarRoof", new[]
        {
            new Vector4(.66f, 1.04f, 1.36f, -.28f),
            new Vector4(.64f, 1.00f, 1.43f, -.29f)
        }), paint);
        var wheels = new List<Transform>();
        foreach (float side in new[] { -1f, 1f })
        {
            foreach (float z in new[] { -1.26f, 1.25f })
                wheels.Add(Wheel(car, new Vector3(side * .83f, .31f, z), .31f, .23f, rubber, white));
            Piece(car, "Mirror", PrimitiveType.Sphere, new Vector3(side * .86f, 1.08f, .70f), new Vector3(.26f, .135f, .24f), paint);
            Box(car, "Door handle", new Vector3(side * .878f, .86f, .14f), new Vector3(.027f, .034f, .16f), metal);
            Box(car, "Headlight", new Vector3(side * .55f, .75f, 1.958f), new Vector3(.43f, .12f, .09f), white);
            Box(car, "Tail light", new Vector3(side * .61f, .75f, -1.954f), new Vector3(.34f, .13f, .10f), red);
        }
        Box(car, "Radiator grille", new Vector3(0, .61f, 2.018f), new Vector3(.72f, .19f, .08f), glass);
        Box(car, "Front license plate", new Vector3(0, .47f, 2.047f), new Vector3(.33f, .09f, .025f), white);
        Box(car, "Rear license plate", new Vector3(0, .62f, -2.026f), new Vector3(.33f, .09f, .025f), white);
        var collision = car.gameObject.AddComponent<BoxCollider>();
        collision.center = new Vector3(0, .64f, 0);
        collision.size = new Vector3(1.84f, 1.24f, 4.13f);
        vehicle.wheels = wheels.ToArray();
        vehicle.wheelRadius = .31f;
    }

    static void Scooter(ShopVehicle vehicle)
    {
        Transform bike = vehicle.transform;
        var rubber = Material("Hair");
        var steel = Material("Steel");
        var white = Material("ACWhite");
        var red = Material("Red");
        vehicle.wheels = new[]
        {
            Wheel(bike, new Vector3(0, .24f, -.64f), .245f, .135f, rubber, white),
            Wheel(bike, new Vector3(0, .24f, .69f), .245f, .12f, rubber, white)
        };
        vehicle.wheelRadius = .245f;
        Piece(bike, "Tail fairing", PrimitiveType.Sphere, new Vector3(0, .58f, -.43f), new Vector3(.50f, .43f, .98f), red);
        Piece(bike, "Front fairing", PrimitiveType.Sphere, new Vector3(0, .68f, .52f), new Vector3(.40f, .76f, .47f), red);
        Piece(bike, "Seat", PrimitiveType.Sphere, new Vector3(0, .80f, -.35f), new Vector3(.41f, .17f, .82f), rubber);
        Box(bike, "Footboard", new Vector3(0, .29f, .03f), new Vector3(.41f, .10f, .75f), steel);
        Box(bike, "Handlebar", new Vector3(0, 1.02f, .52f), new Vector3(.65f, .055f, .075f), rubber);
        Piece(bike, "Headlight", PrimitiveType.Sphere, new Vector3(0, .85f, .738f), new Vector3(.27f, .17f, .095f), white);
        Box(bike, "Taillight", new Vector3(0, .61f, -.91f), new Vector3(.24f, .10f, .035f), Material("Yellow"));

        Transform rider = Child(bike, "Helmeted rider", bike.position);
        vehicle.rider = rider.gameObject;
        var top = Material("Yellow");
        Piece(rider, "Jacket", PrimitiveType.Sphere, new Vector3(0, 1.09f, -.21f), new Vector3(.43f, .54f, .37f), top);
        Piece(rider, "Helmet", PrimitiveType.Sphere, new Vector3(0, 1.47f, -.12f), new Vector3(.285f, .32f, .32f), white);
        Piece(rider, "Helmet visor", PrimitiveType.Sphere, new Vector3(0, 1.48f, -.002f), new Vector3(.25f, .15f, .115f), steel);
        foreach (float side in new[] { -1f, 1f })
        {
            Limb(rider, new Vector3(side * .16f, 1.23f, -.13f), new Vector3(side * .27f, 1.02f, .51f), .12f, top);
            Limb(rider, new Vector3(side * .14f, .84f, -.35f), new Vector3(side * .21f, .57f, .16f), .16f, steel);
            Limb(rider, new Vector3(side * .21f, .58f, .16f), new Vector3(side * .20f, .34f, .08f), .13f, steel);
        }
        var collision = bike.gameObject.AddComponent<BoxCollider>();
        collision.center = new Vector3(0, .51f, 0);
        collision.size = new Vector3(.59f, 1.04f, 1.83f);
        rider.gameObject.SetActive(false);
    }

    static void Limb(Transform rider, Vector3 from, Vector3 to, float width, Material material)
    {
        Transform limb = Piece(rider, "Clothing", PrimitiveType.Sphere, (from + to) / 2,
            new Vector3(width, Vector3.Distance(from, to) + width * .7f, width), material);
        limb.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
    }

    static Transform Wheel(Transform actor, Vector3 center, float radius, float width, Material rubber, Material metal)
    {
        Transform pivot = Child(actor, "Wheel", actor.TransformPoint(center));
        var tyre = Piece(pivot, "Tyre", PrimitiveType.Cylinder, Vector3.zero, new Vector3(radius * 2, width / 2, radius * 2), rubber);
        var hub = Piece(pivot, "Hub", PrimitiveType.Cylinder, Vector3.zero, new Vector3(radius * 1.22f, width * .51f, radius * 1.22f), metal);
        tyre.localRotation = hub.localRotation = Quaternion.Euler(0, 0, 90f);
        Box(pivot, "Inset spoke", Vector3.zero, new Vector3(width * 1.035f, radius * .95f, .027f), rubber);
        Box(pivot, "Inset spoke", Vector3.zero, new Vector3(width * 1.035f, .027f, radius * .95f), rubber);
        return pivot;
    }

    // Rings are half-width, half-length, height and longitudinal offset.
    static UnityEngine.Mesh Shell(string name, Vector4[] rings)
    {
        string path = MeshFolder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
        if (existing) return existing;
        if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder(World, "Vehicles");

        const int verticesPerRing = 20;
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        for (int level = 0; level < rings.Length; level++)
        {
            Vector4 ring = rings[level];
            const float radius = .23f;
            for (int corner = 0; corner < 4; corner++)
            for (int point = 0; point < 5; point++)
            {
                float angle = (corner * 90f + point * 22.5f) * Mathf.Deg2Rad;
                float cx = (corner == 0 || corner == 3 ? 1 : -1) * (ring.x - radius);
                float cz = (corner <= 1 ? 1 : -1) * (ring.y - radius);
                vertices.Add(new Vector3(cx + Mathf.Cos(angle) * radius, ring.z, ring.w + cz + Mathf.Sin(angle) * radius));
            }
            if (level == 0) continue;
            int lower = (level - 1) * verticesPerRing;
            for (int i = 0; i < verticesPerRing; i++)
            {
                int a = lower + i;
                int b = lower + (i + 1) % verticesPerRing;
                indices.AddRange(new[] { a, a + verticesPerRing, b, b, a + verticesPerRing, b + verticesPerRing });
            }
        }

        int top = vertices.Count - verticesPerRing;
        for (int i = 1; i < verticesPerRing - 1; i++)
            indices.AddRange(new[] { 0, i, i + 1, top, top + i + 1, top + i });
        var mesh = new UnityEngine.Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static void MeshPart(Transform car, string name, UnityEngine.Mesh mesh, Material material)
    {
        var part = Child(car, name, car.position);
        part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    static Material Material(string name) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{World}/Materials/KD_{name}.mat") ??
        throw new InvalidOperationException("Missing parking material " + name);

    static Transform Child(Transform parent, string name, Vector3 worldPosition)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.position = worldPosition;
        return child;
    }

    static Transform Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, bool solid = false) =>
        Piece(parent, name, PrimitiveType.Cube, localPosition, size, material, solid);

    static Transform Piece(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 size,
        Material material, bool solid = false)
    {
        var part = GameObject.CreatePrimitive(type);
        if (!solid) UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part.transform;
    }
}
