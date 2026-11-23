using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KopdesShopSetup
{
    public const string ScenePath = "Assets/Kopdes/Scenes/Kopdes.unity";
    const string WorldAssets = "Assets/Kopdes/World";

    [MenuItem("Kopdes/Install Shop Gameplay")]
    public static void Install()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var player = UnityEngine.Object.FindAnyObjectByType<KopdesPlayer>();
        var character = AssetDatabase.LoadAssetAtPath<GameObject>(WorldAssets + "/Prefabs/KopdesPlayer.prefab");
        AddGameplay(player, character);
        KopdesParkingSetup.Apply();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Kopdes.");
    }

    public static void AddGameplay(KopdesPlayer player, GameObject character)
    {
        if (UnityEngine.Object.FindAnyObjectByType<KopdesShop>()) return;
        if (!player || !player.view || !character) throw new InvalidOperationException("Shop requires the player and character prefab.");

        Transform world = GameObject.Find("Kopdes • Blender World").transform;
        var rackGroups = new[] { world.Find("Shelf_00"), world.Find("Shelf_01"), world.Find("Shelf_02") };
        Transform staffRack = world.Find("Warehouse");
        var oldCashier = world.Find("NPC_Kasir");
        var oldResident = world.Find("NPC_Warga");
        if (rackGroups.Any(rack => !rack) || !staffRack || !oldCashier || !oldResident)
            throw new InvalidOperationException("Missing shop display, warehouse, or NPC source.");

        Material ivory = Material("Ivory");
        Material red = Material("Red");
        Material[] productColors = { red, Material("Yellow"), Material("Blue"), Material("Green") };
        var root = new GameObject("Shop Gameplay");
        var shop = root.AddComponent<KopdesShop>();
        shop.playerView = player.view;
        shop.shelves = new ShopShelf[6];

        string[] categories = { "Sembako", "Makanan", "Rumah Tangga" };
        for (int column = 0; column < rackGroups.Length; column++)
        {
            Transform rack = rackGroups[column];
            foreach (var renderer in rack.GetComponentsInChildren<MeshRenderer>())
                if (renderer.name.EndsWith("_Detail", StringComparison.Ordinal)) renderer.enabled = false;

            for (int row = 0; row < 2; row++)
            {
                float x = -4.7f + 3.2f * column;
                float z = 3.5f * row;
                var display = Child(rack, categories[column] + (row == 0 ? " • Depan" : " • Belakang"), new Vector3(x, 0f, z));
                Trigger(display, new Vector3(0, 1.43f, -.49f), new Vector3(2.03f, 1.92f, .15f));
                var shelf = display.gameObject.AddComponent<ShopShelf>();
                shelf.category = categories[column];
                shelf.startingStock = column == 0 && row == 0 ? 1 : 7;
                shelf.packages = Child(display, "Products", display.position);
                shelf.browsePoint = Child(display, "Browse point", new Vector3(x, .31f, z - 1.1f));
                shop.shelves[column * 2 + row] = shelf;

                for (int level = 0; level < 4; level++)
                {
                    float height = .45f + level * .5f;
                    Box(display, "Ticket rail", new Vector3(x, height + .03f, z - .48f), new Vector3(2.05f, .09f, .04f), red);
                    for (int slot = 0; slot < 3; slot++)
                    {
                        float productX = x + (slot - 1) * .57f;
                        var product = Box(shelf.packages, "Package", new Vector3(productX, height + .19f, z - .25f),
                            new Vector3(.23f, .31f, .26f), productColors[(column + level + slot) % productColors.Length]);
                        Box(product, "Label", new Vector3(productX, height + .20f, z - .383f), new Vector3(.16f, .1f, .007f), ivory);
                        product.gameObject.SetActive(shelf.packages.childCount <= shelf.startingStock);
                    }
                }
            }
        }

        var pickup = Child(staffRack, "Pickup • Warehouse stock", new Vector3(-4.7f, 1.42f, 14.3f));
        shop.stockPickup = Trigger(pickup, Vector3.zero, new Vector3(1.53f, 2.12f, 1.13f));
        var label = Child(pickup, "Stock sign", new Vector3(-3.9f, 2.3f, 14.3f));
        label.rotation = Quaternion.Euler(0, -90f, 0);
        var text = label.gameObject.AddComponent<TextMesh>();
        text.text = "STOK TOKO";
        text.anchor = TextAnchor.MiddleCenter;
        text.fontSize = 48;
        text.characterSize = .095f;
        text.color = new Color(.97f, .9f, .75f);

        oldCashier.gameObject.SetActive(false);
        oldResident.gameObject.SetActive(false);
        oldCashier.name = "Cashier • source model";
        oldResident.name = "Resident • source model";
        var actors = Child(root.transform, "NPCs", Vector3.zero);
        shop.cashier = Actor(character, actors, "Kasir", new Vector3(5f, .31f, -.7f), 180f);
        shop.residents = new[]
        {
            Actor(character, actors, "Warga 1", new Vector3(-5f, .2f, -7.9f), 90f, productColors[3]),
            Actor(character, actors, "Warga 2", new Vector3(6f, .2f, -8.7f), -90f, productColors[2]),
            Actor(character, actors, "Warga 3", new Vector3(-7.5f, .2f, -9.5f), 90f, productColors[1])
        };

        shop.checkoutPackage = Box(root.transform, "Package at checkout", new Vector3(4.75f, 1.57f, -1.95f),
            new Vector3(.24f, .22f, .28f), productColors[1]);
        shop.checkoutPackage.gameObject.SetActive(false);
        KopdesParkingSetup.Apply();
    }

    static Material Material(string name)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>($"{WorldAssets}/Materials/KD_{name}.mat");
        return material ? material : throw new InvalidOperationException("Missing shop material " + name);
    }

    static Transform Child(Transform parent, string name, Vector3 position)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.position = position;
        return child;
    }

    static BoxCollider Trigger(Transform owner, Vector3 center, Vector3 size)
    {
        var collider = owner.gameObject.AddComponent<BoxCollider>();
        collider.center = center;
        collider.size = size;
        collider.isTrigger = true;
        return collider;
    }

    static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        // Labels also use world dimensions because their package parent has a non-uniform scale.
        Vector3 scale = parent.lossyScale;
        box.transform.localScale = new Vector3(size.x / scale.x, size.y / scale.y, size.z / scale.z);
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        return box.transform;
    }

    static Animator Actor(GameObject prefab, Transform parent, string name, Vector3 position, float yaw, Material shirt = null)
    {
        var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        actor.name = name;
        actor.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (shirt)
        {
            foreach (var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] && materials[i].name.Contains("Uniform_Red")) materials[i] = shirt;
                renderer.sharedMaterials = materials;
            }
        }

        Animator animator = actor.GetComponent<Animator>();
        if (!animator || !animator.runtimeAnimatorController)
            throw new InvalidOperationException("NPC requires the character's Idle and Walk controller.");
        return animator;
    }
}
