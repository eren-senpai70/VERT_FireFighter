using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vert.Firefighter.Training;

public static class HallwayTrainingSceneBuilder
{
    const string ScenePath = "Assets/Scenes/FireHallwayTraining.unity";
    const string MaterialsPath = "Assets/Materials/HallwayTraining";

    [MenuItem("Firefighter Training/Build Hallway Blockout")]
    public static void BuildHallwayBlockout()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var bootstrap = GameObject.Find("Hallway Training Bootstrap");
        if (bootstrap != null)
            Object.DestroyImmediate(bootstrap);

        DestroyRoot("Dummy Training Hallway");
        DestroyRoot("Training Instructions");
        DestroyRoot("Fire Target");
        DestroyRoot("Touch Grab Dummy Fire Extinguisher");
        DestroyRoot("Hallway Directional Light");
        for (var i = 1; i <= 4; i++) DestroyRoot($"Ceiling Light {i}");

        var origin = GameObject.Find("Hallway Quest XR Origin");
        if (origin == null)
            throw new System.InvalidOperationException("Hallway Quest XR Origin was not found in the training scene.");

        var mats = MakeMaterials();
        BuildHallway(mats);
        var prompt = BuildPrompt(origin.GetComponentInChildren<Camera>(true));
        var fire = BuildFire(mats, prompt);
        BuildExtinguisher(mats, prompt, fire);
        AddHand(Find(origin.transform, "Left Controller"), true);
        AddHand(Find(origin.transform, "Right Controller"), false);
        BuildLights(mats);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Hallway training blockout is saved as editable scene objects.");
    }

    sealed class Mats
    {
        public Material wall, floor, trim, door, red, metal, bin, flame, innerFlame, suppressant, lightPanel;
    }

    static Mats MakeMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialsPath))
            AssetDatabase.CreateFolder("Assets/Materials", "HallwayTraining");

        return new Mats
        {
            wall = Material("Wall", new Color(0.68f, 0.71f, 0.72f)),
            floor = Material("Floor", new Color(0.22f, 0.25f, 0.27f)),
            trim = Material("SafetyYellow", new Color(0.95f, 0.68f, 0.11f)),
            door = Material("DummyDoor", new Color(0.35f, 0.39f, 0.42f)),
            red = Material("ExtinguisherRed", new Color(0.8f, 0.035f, 0.025f)),
            metal = Material("DarkMetal", new Color(0.13f, 0.15f, 0.17f)),
            bin = Material("BurningCabinet", new Color(0.19f, 0.21f, 0.22f)),
            flame = Material("FlameOrange", new Color(1f, 0.18f, 0.02f), true),
            innerFlame = Material("FlameYellow", new Color(1f, 0.72f, 0.04f), true),
            suppressant = Material("SuppressionPowder", new Color(0.92f, 0.96f, 1f)),
            lightPanel = Material("CeilingLightPanel", new Color(1f, 0.88f, 0.68f), true)
        };
    }

    static Material Material(string name, Color color, bool emission = false)
    {
        var path = $"{MaterialsPath}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.5f);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void BuildHallway(Mats m)
    {
        var hall = new GameObject("Dummy Training Hallway");
        Block("Floor", hall.transform, new Vector3(0, -0.15f, 1), new Vector3(4, 0.3f, 16), m.floor);
        Block("Ceiling", hall.transform, new Vector3(0, 3.25f, 1), new Vector3(4, 0.2f, 16), m.wall);
        Block("Left Wall", hall.transform, new Vector3(-2.05f, 1.55f, 1), new Vector3(0.2f, 3.2f, 16), m.wall);
        Block("Right Wall", hall.transform, new Vector3(2.05f, 1.55f, 1), new Vector3(0.2f, 3.2f, 16), m.wall);
        Block("Hallway End", hall.transform, new Vector3(0, 1.55f, 9), new Vector3(4.2f, 3.2f, 0.2f), m.wall);
        Block("Left Safety Stripe", hall.transform, new Vector3(-1.93f, 0.025f, 1), new Vector3(0.035f, 0.02f, 15.6f), m.trim);
        Block("Right Safety Stripe", hall.transform, new Vector3(1.93f, 0.025f, 1), new Vector3(0.035f, 0.02f, 15.6f), m.trim);
        for (var i = 0; i < 3; i++)
        {
            var z = -2.8f + i * 4.3f;
            Block($"Left Dummy Door {i + 1}", hall.transform, new Vector3(-1.91f, 1.05f, z), new Vector3(0.035f, 2.1f, 1.3f), m.door);
            Block($"Right Dummy Door {i + 1}", hall.transform, new Vector3(1.91f, 1.05f, z), new Vector3(0.035f, 2.1f, 1.3f), m.door);
        }
        Block("Extinguisher Wall Mount", hall.transform, new Vector3(1.88f, 1.28f, -1.25f), new Vector3(0.12f, 1.05f, 0.65f), m.trim);
        Block("Burning Equipment Cabinet", hall.transform, new Vector3(0, 0.48f, 5.65f), new Vector3(1.1f, 0.95f, 0.85f), m.bin);
        Block("Cabinet Top", hall.transform, new Vector3(0, 1f, 5.65f), new Vector3(1.15f, 0.08f, 0.9f), m.metal);
    }

    static HallwayTrainingPrompt BuildPrompt(Camera camera)
    {
        var canvasObject = new GameObject("Training Instructions", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        canvasObject.transform.SetParent(camera.transform, false);
        canvasObject.transform.localPosition = new Vector3(0, -0.18f, 1.25f);
        canvasObject.transform.localScale = Vector3.one * 0.001f;
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 230);
        var panel = new GameObject("Prompt Background", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.045f, 0.84f);
        var textObject = new GameObject("Prompt Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(35, 15); rect.offsetMax = new Vector2(-35, -15);
        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 39; text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.text = "Use the left stick to move. Walk to the extinguisher and touch it, then squeeze a trigger to pick it up.";
        var prompt = canvasObject.AddComponent<HallwayTrainingPrompt>();
        prompt.Initialize(text);
        return prompt;
    }

    static FireTrainingTarget BuildFire(Mats m, HallwayTrainingPrompt prompt)
    {
        var root = new GameObject("Fire Target");
        root.transform.position = new Vector3(0, 1.08f, 5.6f);
        var a = Primitive("Outer Flame Left", PrimitiveType.Sphere, root.transform, new Vector3(-0.25f, 0.02f, 0), new Vector3(0.36f, 0.75f, 0.36f), m.flame, false);
        var b = Primitive("Outer Flame Right", PrimitiveType.Sphere, root.transform, new Vector3(0.2f, 0.04f, 0.08f), new Vector3(0.32f, 0.62f, 0.32f), m.flame, false);
        var c = Primitive("Inner Flame", PrimitiveType.Sphere, root.transform, new Vector3(0, -0.06f, -0.08f), new Vector3(0.2f, 0.5f, 0.22f), m.innerFlame, false);
        var particleObject = new GameObject("Flame Particles");
        particleObject.transform.SetParent(root.transform, false);
        var particles = particleObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.75f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.36f, 0.025f), new Color(1f, 0.84f, 0.12f));
        var emission = particles.emission; emission.rateOverTime = 22f;
        var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 18f; shape.radius = 0.12f;
        particleObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.flame;
        var target = root.AddComponent<FireTrainingTarget>();
        target.Configure(new[] { a, b, c }, particles, prompt);
        return target;
    }

    static void BuildExtinguisher(Mats m, HallwayTrainingPrompt prompt, FireTrainingTarget fire)
    {
        var root = new GameObject("Touch Grab Dummy Fire Extinguisher");
        root.transform.position = new Vector3(1.52f, 1.18f, -1.25f);
        var capsule = root.AddComponent<CapsuleCollider>(); capsule.direction = 1; capsule.radius = 0.18f;
        capsule.height = 0.78f; capsule.center = new Vector3(0, 0.02f, 0);
        var touch = root.AddComponent<BoxCollider>(); touch.size = new Vector3(0.5f, 0.9f, 0.5f); touch.isTrigger = true;
        var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        var interactable = root.AddComponent<TouchGrabExtinguisher>();
        Primitive("Tank", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.2f, 0.42f, 0.2f), m.red, false);
        Primitive("Base", PrimitiveType.Cylinder, root.transform, new Vector3(0, -0.41f, 0), new Vector3(0.19f, 0.04f, 0.19f), m.metal, false);
        Primitive("Valve", PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.46f, 0), new Vector3(0.07f, 0.08f, 0.07f), m.metal, false);
        Primitive("Handle", PrimitiveType.Cube, root.transform, new Vector3(0, 0.56f, 0), new Vector3(0.3f, 0.05f, 0.13f), m.metal, false);
        Primitive("Lever", PrimitiveType.Cube, root.transform, new Vector3(0, 0.62f, 0), new Vector3(0.2f, 0.035f, 0.1f), m.red, false);
        Primitive("Nozzle", PrimitiveType.Capsule, root.transform, new Vector3(0.13f, 0.5f, 0.16f), new Vector3(0.045f, 0.17f, 0.045f), m.metal, false, Quaternion.Euler(90, 0, 0));
        Primitive("Hose", PrimitiveType.Capsule, root.transform, new Vector3(-0.13f, 0.37f, 0.08f), new Vector3(0.025f, 0.17f, 0.025f), m.metal, false, Quaternion.Euler(22, 0, 35));
        var sprayOrigin = new GameObject("Spray Origin").transform;
        sprayOrigin.SetParent(root.transform, false); sprayOrigin.localPosition = new Vector3(0.13f, 0.5f, 0.24f);
        var sprayObject = new GameObject("Suppressant Spray"); sprayObject.transform.SetParent(sprayOrigin, false);
        var spray = sprayObject.AddComponent<ParticleSystem>();
        var main = spray.main; main.loop = true; main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f); main.startSpeed = new ParticleSystem.MinMaxCurve(2.1f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f); main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.93f, 0.97f, 1f, 0.8f));
        var emission = spray.emission; emission.rateOverTime = 85f;
        var shape = spray.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 8f; shape.radius = 0.025f;
        sprayObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.suppressant;
        interactable.SetSprayOrigin(sprayOrigin); interactable.SetSprayParticles(spray);
        interactable.SetPrompt(prompt); interactable.SetFireTarget(fire);
    }

    static void AddHand(Transform controller, bool left)
    {
        // The previous hand prototype disabled all renderers on the controller,
        // which hid the actual controller model. Remove only that prototype and
        // restore the XR Controller Visual renderers.
        foreach (var oldName in new[]
                 {
                     left ? "Left Hand Visual" : "Right Hand Visual",
                     "Extinguisher Touch Point"
                 })
        {
            var oldObject = controller.Find(oldName);
            if (oldObject != null)
                Object.DestroyImmediate(oldObject.gameObject);
        }

        foreach (var renderer in controller.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;

        var hand = new GameObject("Extinguisher Touch Point");
        hand.transform.SetParent(controller, false);
        var handScript = hand.AddComponent<QuestTrainingHand>();
        handScript.Initialize(left);
        var touch = hand.GetComponent<SphereCollider>();
        touch.isTrigger = true;
        touch.radius = 0.13f;
        touch.center = new Vector3(0f, 0f, -0.035f);
    }

    static Transform Find(Transform root, string name)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            if (transform.name == name) return transform;
        throw new System.InvalidOperationException($"Controller '{name}' is missing from the XR Origin.");
    }

    static void DestroyRoot(string objectName)
    {
        var existing = GameObject.Find(objectName);
        if (existing != null)
            Object.DestroyImmediate(existing);
    }

    static void BuildLights(Mats mats)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.48f, 0.53f, 0.6f);
        RenderSettings.ambientEquatorColor = new Color(0.29f, 0.31f, 0.34f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.115f, 0.1f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.fog = false;

        var sunObject = new GameObject("Hallway Directional Light");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.lightUnit = LightUnit.Lux;
        sun.intensity = 45f;
        sun.color = new Color(0.72f, 0.8f, 1f);
        sun.shadows = LightShadows.None;
        sunObject.transform.rotation = Quaternion.Euler(48, -28, 0);
        var fixturePositions = new[] { -4.5f, -0.6f, 3.3f, 7.2f };
        for (var i = 0; i < fixturePositions.Length; i++)
        {
            var lamp = new GameObject($"Ceiling Light {i + 1}");
            lamp.transform.position = new Vector3(0, 3.02f, fixturePositions[i]);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.lightUnit = LightUnit.Lumen;
            light.color = new Color(1f, 0.88f, 0.73f);
            light.intensity = 1100f;
            light.range = 8f;
            light.shadows = LightShadows.None;
            Primitive("Visible Ceiling Fixture", PrimitiveType.Cube, lamp.transform,
                new Vector3(0f, 0.08f, 0f), new Vector3(0.85f, 0.045f, 0.24f), mats.lightPanel, false);
        }
    }

    static GameObject Block(string name, Transform parent, Vector3 p, Vector3 s, Material material)
        => Primitive(name, PrimitiveType.Cube, parent, p, s, material, true);

    static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 p, Vector3 s,
        Material material, bool keepCollider, Quaternion? rotation = null)
    {
        var instance = GameObject.CreatePrimitive(type); instance.name = name; instance.transform.SetParent(parent, false);
        instance.transform.localPosition = p; instance.transform.localScale = s; instance.transform.localRotation = rotation ?? Quaternion.identity;
        instance.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Object.DestroyImmediate(instance.GetComponent<Collider>());
        return instance;
    }
}
