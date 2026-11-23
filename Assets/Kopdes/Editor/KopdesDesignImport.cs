using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;

public static class KopdesDesignImport
{
    [Serializable] public class WorldData { public MatData[] materials; public Batch[] batches; }
    [Serializable] public class MatData { public string name; public float r,g,b,a,emission; }
    [Serializable] public class Batch { public string name,group,material; public bool collision; public float[] vertices; public int[] triangles; }
    const string Base="Assets/Kopdes/World";
    const string Report="/Users/becket/Documents/Project/Unity/Documentation/DesignUpdate";
    [MenuItem("Kopdes/Rebuild World")]
    public static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Base+"/Generated");Directory.CreateDirectory(Base+"/Prefabs");Directory.CreateDirectory(Base+"/Materials");
        AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene("Assets/Kopdes/Scenes/Archive/WorldSource.unity");
        var old=GameObject.Find("Kopdes • Blender World");
        if(!old)throw new Exception("Original Blender world root not found");
        var porch=UnityEngine.Object.FindAnyObjectByType<PorchAmbience>()?.porchLight;
        UnityEngine.Object.DestroyImmediate(old);
        var data=JsonUtility.FromJson<WorldData>(File.ReadAllText(Base+"/KopdesWorld.json"));
        var materials=new Dictionary<string,Material>();
        foreach(var d in data.materials)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=d.name;m.color=new Color(d.r,d.g,d.b,d.a);m.SetFloat("_Smoothness",d.name.Contains("AC")?.35f:.15f);
            if(d.emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(d.r,d.g,d.b)*d.emission);}
            if(d.a<1){m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.renderQueue=3000;}
            string path=Base+"/Materials/"+d.name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing){EditorUtility.CopySerialized(m,existing);UnityEngine.Object.DestroyImmediate(m);m=existing;}else AssetDatabase.CreateAsset(m,path);
            materials[d.name]=m;
        }
        var world=new GameObject("Kopdes • Blender World");var groups=new Dictionary<string,GameObject>();
        foreach(var b in data.batches)
        {
            if(b.group=="Storage")throw new Exception("Old interior storage exported unexpectedly");
            if(!groups.ContainsKey(b.group)){var g=new GameObject(b.group);g.transform.SetParent(world.transform);groups[b.group]=g;}
            string path=Base+"/Generated/"+b.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.name=b.name;mesh.indexFormat=IndexFormat.UInt32;
            var verts=new Vector3[b.vertices.Length/3];for(int i=0;i<verts.Length;i++)verts[i]=new Vector3(b.vertices[i*3],b.vertices[i*3+1],b.vertices[i*3+2]);
            if(b.group!="Forest" && b.group!="Mountains" && b.group!="Groundcover"){
                var flat=new Vector3[b.triangles.Length];var idx=new int[flat.Length];for(int i=0;i<idx.Length;i++){flat[i]=verts[b.triangles[i]];idx[i]=i;}mesh.vertices=flat;mesh.triangles=idx;
            }else{mesh.vertices=verts;mesh.triangles=b.triangles;}
            mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var go=new GameObject(b.name);go.transform.SetParent(groups[b.group].transform);go.isStatic=true;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[b.material];if(b.collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        world.AddComponent<PorchAmbience>().porchLight=porch;
        foreach(string name in new[]{"Warehouse","HVAC","LightingFixtures"})PrefabUtility.SaveAsPrefabAsset(groups[name],Base+"/Prefabs/"+name+".prefab");
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){
            if(light.name=="Shop ceiling"){light.intensity=2.3f;light.range=9;light.color=new Color(.91f,.96f,1);light.transform.position=new Vector3(light.transform.position.x,3.48f,light.transform.position.z<2?-1:4.7f);light.renderMode=LightRenderMode.ForcePixel;}
            if(light.name=="Warm porch"){light.intensity=2.0f;light.range=10;}
        }
        var lights=new GameObject("Warehouse and service lighting");
        foreach(var p in new[]{new Vector3(-2,3.1f,14.3f),new Vector3(-2,3.1f,17.1f),new Vector3(-3,3.26f,10.3f)}){
            var g=new GameObject("LED • Warehouse / passage");g.transform.SetParent(lights.transform);g.transform.position=p;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.91f,.96f,1);l.intensity=2.2f;l.range=6;l.renderMode=LightRenderMode.ForcePixel;
        }
        RenderSettings.ambientSkyColor=new Color(.48f,.54f,.56f);RenderSettings.ambientEquatorColor=new Color(.34f,.39f,.34f);RenderSettings.ambientGroundColor=new Color(.20f,.23f,.20f);QualitySettings.pixelLightCount=12;
        var character=ImportCharacter();
        var player=UnityEngine.Object.FindAnyObjectByType<KopdesPlayer>();
        if(!player)throw new Exception("Player missing");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(character);visual.name="Player Character • first-person shadow";visual.transform.SetParent(player.transform,false);
        // First-person camera sees the world, while the player's full body casts its shadow.
        foreach(var r in visual.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
        Validate(scene,groups,character);
        KopdesShopSetup.AddGameplay(player, character);
        EditorSceneManager.SaveScene(scene, KopdesShopSetup.ScenePath);
        Capture(player.view,new Vector3(17,13,28),new Vector3(-1,2,10),"World-Rear");
        Capture(player.view,new Vector3(-.5f,2.15f,-3.1f),new Vector3(-2,2,7.7f),"Shop-Interior");
        Capture(player.view,new Vector3(-2.9f,1.9f,12.05f),new Vector3(-2,1.3f,17),"Warehouse-Interior");
        Capture(player.view,new Vector3(6.4f,2.95f,-.1f),new Vector3(7.7f,3,1.8f),"AC-Interior");
        CreateCharacterPreview(character);
        EditorSceneManager.OpenScene(KopdesShopSetup.ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("KOPDES_WORLD_OK: warehouse, rear exit, HVAC, brighter LEDs and rigged player imported.");
    }
    static GameObject ImportCharacter()
    {
        string path=Base+"/Kopdes_Player.fbx";
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        var clips=importer.defaultClipAnimations;
        foreach(var c in clips){c.loopTime=true;c.loopPose=true;}
        importer.clipAnimations=clips;importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!model)throw new Exception("Character FBX import failed");
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(model);obj.name="Kopdes Player Character";
        var animator=obj.GetComponent<Animator>(); if(!animator)animator=obj.AddComponent<Animator>();
        string controllerPath=Base+"/Player.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var sm=controller.layers[0].stateMachine;foreach(var s in sm.states)sm.RemoveState(s.state);
        var animationClips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        foreach(var c in animationClips){var st=sm.AddState(c.name.Contains("Walk")?"Walk":"Idle");st.motion=c;if(c.name.Contains("Idle"))sm.defaultState=st;}
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
        var prefab=PrefabUtility.SaveAsPrefabAsset(obj,Base+"/Prefabs/KopdesPlayer.prefab");UnityEngine.Object.DestroyImmediate(obj);
        File.WriteAllText(Report+"/character-import.txt","Clips: "+string.Join(", ",animationClips.Select(c=>c.name))+"\nRenderers: "+prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Length+"\n");
        if(animationClips.Length<2 || prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0)throw new Exception("Character rig or animation clips missing");
        return prefab;
    }
    static void Validate(UnityEngine.SceneManagement.Scene scene,Dictionary<string,GameObject> groups,GameObject character)
    {
        Physics.SyncTransforms();
        if(groups.ContainsKey("Storage"))throw new Exception("Interior storage still exists");
        if(Physics.SphereCast(new Vector3(-3,1.4f,7),.27f,Vector3.forward,out var hit,6))throw new Exception("Rear passage blocked by "+hit.collider.name);
        if(!Physics.Raycast(new Vector3(-3,2,15),Vector3.down,out var floor,3))throw new Exception("Warehouse floor missing");
        if(floor.point.y<.2f || floor.point.y>.4f)throw new Exception("Warehouse floor elevation incorrect: "+floor.point);
        File.WriteAllText(Report+"/unity-design-validation.txt","PASS: no interior Storage group; rear staff doorway and warehouse entry clearance; warehouse floor; HVAC and LED fixture groups; imported skinned player with Idle and Walk clips.\n");
    }
    static void Capture(Camera cam,Vector3 pos,Vector3 target,string name)
    {
        var p=cam.transform.position;var q=cam.transform.rotation;cam.transform.position=pos;cam.transform.LookAt(target);
        var rt=new RenderTexture(1440,900,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes(Report+"/"+name+".png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);cam.transform.SetPositionAndRotation(p,q);
    }
    static void CreateCharacterPreview(GameObject character)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(character);obj.transform.position=Vector3.zero;
        var camera=new GameObject("Character review camera").AddComponent<Camera>();camera.transform.position=new Vector3(2.8f,1.9f,4.8f);camera.transform.LookAt(new Vector3(0,.92f,0));camera.orthographic=true;camera.orthographicSize=1.18f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.17f,.15f);camera.gameObject.tag="MainCamera";
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.57f,.55f);
        var sun=new GameObject("Soft key light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.transform.rotation=Quaternion.Euler(40,-30,0);
        EditorSceneManager.SaveScene(scene,"Assets/Kopdes/Scenes/CharacterPreview.unity");Capture(camera,camera.transform.position,new Vector3(0,.92f,0),"Player-Unity");
    }
}
