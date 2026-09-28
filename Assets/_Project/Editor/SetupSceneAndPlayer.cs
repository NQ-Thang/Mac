using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public static class SetupSceneAndPlayer
{
    [MenuItem("Tools/Setup Scene and Player")]
    public static void Execute()
    {
        Debug.Log("=== Starting Automated Setup of Cinemachine, Player & Blockout Scene ===");

        // 1. Setup Player Prefab
        string playerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
        GameObject playerRoot = PrefabUtility.LoadPrefabContents(playerPrefabPath);

        // Ensure Transform & Layer
        playerRoot.layer = LayerMask.NameToLayer("Player");
        playerRoot.transform.localScale = Vector3.one;

        // Ensure Rigidbody2D
        Rigidbody2D rb = playerRoot.GetComponent<Rigidbody2D>();
        if (rb == null) rb = playerRoot.AddComponent<Rigidbody2D>();
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.gravityScale = 1f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Ensure BoxCollider2D
        BoxCollider2D col = playerRoot.GetComponent<BoxCollider2D>();
        if (col == null) col = playerRoot.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.50f, 1.30f);
        col.offset = new Vector2(-0.03f, -0.09f);

        // Ensure child check points
        Transform groundCheck = playerRoot.transform.Find("groundCheck");
        if (groundCheck == null) {
            var go = new GameObject("groundCheck");
            go.transform.SetParent(playerRoot.transform, false);
            groundCheck = go.transform;
        }
        groundCheck.localPosition = new Vector3(0f, -0.74f, 0f);

        Transform wallCheck = playerRoot.transform.Find("wallCheck");
        if (wallCheck == null) {
            var go = new GameObject("wallCheck");
            go.transform.SetParent(playerRoot.transform, false);
            wallCheck = go.transform;
        }
        wallCheck.localPosition = new Vector3(0.28f, 0f, 0f);

        Transform headCheck = playerRoot.transform.Find("headCheck");
        if (headCheck == null) {
            var go = new GameObject("headCheck");
            go.transform.SetParent(playerRoot.transform, false);
            headCheck = go.transform;
        }
        headCheck.localPosition = new Vector3(0f, 0.56f, 0f);

        Transform attackPoint = playerRoot.transform.Find("attackPoint");
        if (attackPoint == null) {
            var go = new GameObject("attackPoint");
            go.transform.SetParent(playerRoot.transform, false);
            attackPoint = go.transform;
        }
        attackPoint.localPosition = new Vector3(0.5f, 0f, 0f);

        // Ensure Visual children: noir and Tail
        Transform noir = playerRoot.transform.Find("noir");
        if (noir == null) {
            var go = new GameObject("noir");
            go.transform.SetParent(playerRoot.transform, false);
            noir = go.transform;
        }
        noir.localPosition = new Vector3(0f, -0.74f, 0f);
        noir.localScale = new Vector3(0.3f, 0.3f, 1f);
        var noirSr = noir.GetComponent<SpriteRenderer>();
        if (noirSr == null) noirSr = noir.gameObject.AddComponent<SpriteRenderer>();
        var noirSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Animations/Player/noir.png");
        if (noirSprite != null) noirSr.sprite = noirSprite;

        Transform tail = playerRoot.transform.Find("Tail");
        if (tail == null) {
            var go = new GameObject("Tail");
            go.transform.SetParent(playerRoot.transform, false);
            tail = go.transform;
        }
        tail.localPosition = new Vector3(-0.03f, -0.33f, 0f);
        tail.localScale = new Vector3(0.25f, 0.25f, 1f);
        var tailSr = tail.GetComponent<SpriteRenderer>();
        if (tailSr == null) tailSr = tail.gameObject.AddComponent<SpriteRenderer>();
        var tailSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Animations/Player/tail.png");
        if (tailSprite != null) tailSr.sprite = tailSprite;
        var tailAnim = tail.GetComponent<Animator>();
        if (tailAnim == null) tailAnim = tail.gameObject.AddComponent<Animator>();
        var tailController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/Player/Tail.controller");
        if (tailController != null) tailAnim.runtimeAnimatorController = tailController;

        // Remove old unused SpriteRenderer from root if present
        var rootSr = playerRoot.GetComponent<SpriteRenderer>();
        if (rootSr != null) UnityEngine.Object.DestroyImmediate(rootSr);

        // Setup Component Hub on Player
        var player = playerRoot.GetComponent<Player>();
        if (player == null) player = playerRoot.AddComponent<Player>();

        var movement = playerRoot.GetComponent<PlayerMovement>();
        if (movement == null) movement = playerRoot.AddComponent<PlayerMovement>();

        var combat = playerRoot.GetComponent<PlayerCombat>();
        if (combat == null) combat = playerRoot.AddComponent<PlayerCombat>();

        var swordTech = playerRoot.GetComponent<PlayerSwordTech>();
        if (swordTech == null) swordTech = playerRoot.AddComponent<PlayerSwordTech>();

        var anima = playerRoot.GetComponent<PlayerAnima>();
        if (anima == null) anima = playerRoot.AddComponent<PlayerAnima>();

        var heal = playerRoot.GetComponent<PlayerHeal>();
        if (heal == null) heal = playerRoot.AddComponent<PlayerHeal>();

        var health = playerRoot.GetComponent<Health>();
        if (health == null) health = playerRoot.AddComponent<Health>();

        // Serialized Object tuning for Entity / Player
        SerializedObject soPlayer = new SerializedObject(player);
        soPlayer.FindProperty("groundCheckPosition").objectReferenceValue = groundCheck;
        soPlayer.FindProperty("groundCheckSize").vector2Value = new Vector2(0.85f, 0.25f);
        soPlayer.FindProperty("groundCheckRadius").floatValue = 0.35f;
        soPlayer.FindProperty("groundCheckLayer").intValue = 64; // Ground
        soPlayer.FindProperty("wallCheckPosition").objectReferenceValue = wallCheck;
        soPlayer.FindProperty("wallCheckSize").vector2Value = new Vector2(0.35f, 1.40f);
        soPlayer.FindProperty("wallCheckRadius").floatValue = 0.35f;
        soPlayer.FindProperty("wallCheckLayer").intValue = 256; // Wall
        soPlayer.FindProperty("useBoxCheck").boolValue = true;
        soPlayer.ApplyModifiedProperties();

        // Serialized Object tuning for PlayerMovement
        SerializedObject soMove = new SerializedObject(movement);
        soMove.FindProperty("moveSpeed").floatValue = 10f;
        soMove.FindProperty("jumpForce").floatValue = 13.5f;
        soMove.FindProperty("wallSlideSpeed").floatValue = 2.5f;
        soMove.FindProperty("wallJumpForce").vector2Value = new Vector2(11f, 13.5f);
        soMove.FindProperty("wallJumpDuration").floatValue = 0.16f;
        soMove.FindProperty("wallCatchDelay").floatValue = 0.06f;
        soMove.FindProperty("fallMultiplier").floatValue = 2.5f;
        soMove.FindProperty("jumpBufferTime").floatValue = 0.15f;
        soMove.FindProperty("jumpCutMultiplier").floatValue = 2.5f;
        soMove.FindProperty("coyoteTime").floatValue = 0.15f;
        soMove.FindProperty("apexThreshold").floatValue = 1.5f;
        soMove.FindProperty("apexGravityMultiplier").floatValue = 0.5f;
        soMove.FindProperty("apexBonusSpeedMultiplier").floatValue = 1.15f;
        soMove.FindProperty("cornerCorrectionDistance").floatValue = 0.38f;
        soMove.FindProperty("cornerCorrectionNudge").floatValue = 0.12f;
        soMove.FindProperty("headCheckDistance").floatValue = 0.35f;
        soMove.FindProperty("headCheckPosition").objectReferenceValue = headCheck;
        soMove.ApplyModifiedProperties();

        // Serialized Object tuning for PlayerSwordTech
        var flyingSwordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/FlyingSword.prefab");
        SerializedObject soSwordTech = new SerializedObject(swordTech);
        soSwordTech.FindProperty("swordPrefab").objectReferenceValue = flyingSwordPrefab;
        soSwordTech.FindProperty("swordPickUpDistance").floatValue = 1.0f;
        soSwordTech.FindProperty("dashSpeed").floatValue = 25f;
        soSwordTech.FindProperty("dashDamage").intValue = 20;
        soSwordTech.FindProperty("dashDamageRadius").floatValue = 0.6f;
        soSwordTech.FindProperty("enemyLayer").intValue = 128; // Layer 7: Enemy
        soSwordTech.FindProperty("dashAnimaCost").intValue = 0;
        soSwordTech.FindProperty("throwAnimaCost").intValue = 0;
        soSwordTech.ApplyModifiedProperties();

        // Serialized Object tuning for PlayerCombat
        SerializedObject soCombat = new SerializedObject(combat);
        soCombat.FindProperty("attackPoint").objectReferenceValue = attackPoint;
        soCombat.FindProperty("attackRange").floatValue = 0.5f;
        soCombat.FindProperty("attackDamage").floatValue = 25f;
        soCombat.FindProperty("enemyLayer").intValue = 128;
        soCombat.FindProperty("attackOffsetDistance").floatValue = 0.75f;
        soCombat.ApplyModifiedProperties();

        // Serialized Object tuning for Health
        SerializedObject soHealth = new SerializedObject(health);
        soHealth.FindProperty("maxHealth").floatValue = 100f;
        soHealth.FindProperty("isPlayer").boolValue = true;
        soHealth.FindProperty("invincibleDuration").floatValue = 1.5f;
        soHealth.FindProperty("flashInterval").floatValue = 0.15f;
        soHealth.FindProperty("knockbackForceX").floatValue = 8f;
        soHealth.FindProperty("knockbackForceY").floatValue = 5f;
        soHealth.FindProperty("knockbackDuration").floatValue = 0.2f;
        soHealth.ApplyModifiedProperties();

        // Save Player Prefab
        PrefabUtility.SaveAsPrefabAsset(playerRoot, playerPrefabPath);
        PrefabUtility.UnloadPrefabContents(playerRoot);
        Debug.Log("Player.prefab successfully updated and saved.");

        // 2. Open and Setup Scene
        string scenePath = "Assets/_Project/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Remove old test objects
        foreach(var oldName in new string[] { "Ground", "Wall", "VoTri", "AnPor", "Level_Blockout", "CameraConfinerBounds", "CM Camera" }) {
            var oldGo = GameObject.Find(oldName);
            if (oldGo != null) UnityEngine.Object.DestroyImmediate(oldGo);
        }

        // Setup Main Camera
        var mainCamGo = GameObject.Find("Main Camera");
        if (mainCamGo == null) {
            mainCamGo = new GameObject("Main Camera");
            mainCamGo.tag = "MainCamera";
        }
        var cam = mainCamGo.GetComponent<Camera>();
        if (cam == null) cam = mainCamGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7.5f;
        mainCamGo.transform.position = new Vector3(0f, 1.5f, -10f);

        var brain = mainCamGo.GetComponent<CinemachineBrain>();
        if (brain == null) brain = mainCamGo.AddComponent<CinemachineBrain>();

        // Setup Player in Scene
        var playerSceneGo = GameObject.Find("Player");
        if (playerSceneGo != null) UnityEngine.Object.DestroyImmediate(playerSceneGo);

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        playerSceneGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerSceneGo.name = "Player";
        playerSceneGo.tag = "Player";
        playerSceneGo.transform.position = new Vector3(0f, 1.5f, 0f);
        playerSceneGo.transform.localScale = new Vector3(2f, 2f, 1f);

        // Setup Floating Sword in Scene
        var swordSceneGo = GameObject.Find("Sword");
        if (swordSceneGo == null) {
            var swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Sword.prefab");
            if (swordPrefab != null) {
                swordSceneGo = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab);
                swordSceneGo.name = "Sword";
            } else {
                swordSceneGo = new GameObject("Sword");
                var sr = swordSceneGo.AddComponent<SpriteRenderer>();
                swordSceneGo.AddComponent<FlyingSword>();
            }
        }
        swordSceneGo.transform.position = new Vector3(-1.2f, 2.5f, 0f);
        swordSceneGo.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

        // Setup CameraConfinerBounds
        var confinerObj = new GameObject("CameraConfinerBounds");
        confinerObj.layer = LayerMask.NameToLayer("Ignore Raycast"); // Layer 2
        var poly = confinerObj.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.points = new Vector2[] {
            new Vector2(-8f, -6f),
            new Vector2(98f, -6f),
            new Vector2(98f, 22f),
            new Vector2(-8f, 22f)
        };

        // Setup CM Camera
        var cmCamGo = new GameObject("CM Camera");
        cmCamGo.transform.position = new Vector3(0f, 1.5f, -10f);

        var cmCamera = cmCamGo.AddComponent<CinemachineCamera>();
        cmCamera.Target.TrackingTarget = playerSceneGo.transform;
        cmCamera.Lens.OrthographicSize = 7.5f;

        var composer = cmCamGo.AddComponent<CinemachinePositionComposer>();
        SerializedObject soComp = new SerializedObject(composer);
        soComp.FindProperty("Damping").vector3Value = new Vector3(0.35f, 0.55f, 0f);
        soComp.FindProperty("Composition.ScreenPosition").vector2Value = new Vector2(0.5f, 0.5f);
        soComp.FindProperty("Composition.DeadZone.Enabled").boolValue = true;
        soComp.FindProperty("Composition.DeadZone.Size").vector2Value = new Vector2(0.12f, 0.18f);
        soComp.FindProperty("TargetOffset").vector3Value = new Vector3(0f, 1.0f, 0f);
        soComp.ApplyModifiedProperties();

        var confiner2D = cmCamGo.AddComponent<CinemachineConfiner2D>();
        confiner2D.BoundingShape2D = poly;

        // 3. Build Blockout Geometry
        var levelRoot = new GameObject("Level_Blockout");
        levelRoot.transform.position = Vector3.zero;

        Sprite squareSprite = null;
        var guids = AssetDatabase.FindAssets("Square t:Sprite");
        if (guids.Length > 0) squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (squareSprite == null) squareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        Material litMat = null;
        var matGuids = AssetDatabase.FindAssets("Sprite-Lit-Default t:Material");
        if (matGuids.Length > 0) litMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matGuids[0]));

        int groundLayer = LayerMask.NameToLayer("Ground");
        int wallLayer = LayerMask.NameToLayer("Wall");

        Color groundColor = new Color(0.23f, 0.29f, 0.35f, 1f);
        Color platformColor = new Color(0.31f, 0.40f, 0.48f, 1f);
        Color wallColor = new Color(0.42f, 0.56f, 0.62f, 1f);
        Color catchFloorColor = new Color(0.17f, 0.20f, 0.23f, 1f);
        Color startColor = new Color(0.25f, 0.55f, 0.35f, 1f);
        Color goalColor = new Color(0.85f, 0.65f, 0.25f, 1f);

        Func<string, Transform, Vector3, Vector2, int, Color, GameObject> CreateBlock = (name, parent, pos, size, layer, color) => {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.layer = layer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            if (litMat != null) sr.material = litMat;
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;

            var bcol = go.AddComponent<BoxCollider2D>();
            bcol.size = size;

            return go;
        };

        // Section 1: Movement
        var s1 = new GameObject("Section1_Movement");
        s1.transform.SetParent(levelRoot.transform, false);
        CreateBlock("Start_Floor", s1.transform, new Vector3(0f, -1f, 0f), new Vector2(8f, 2f), groundLayer, groundColor);
        CreateBlock("Left_Border_Wall", s1.transform, new Vector3(-4.5f, 4f, 0f), new Vector2(1f, 12f), groundLayer, groundColor);
        CreateBlock("Start_Marker", s1.transform, new Vector3(-2f, 0.15f, 0f), new Vector2(1.5f, 0.3f), groundLayer, startColor);
        CreateBlock("Sprint_Floor", s1.transform, new Vector3(7.5f, -1f, 0f), new Vector2(7f, 2f), groundLayer, groundColor);
        CreateBlock("Platform_Step1", s1.transform, new Vector3(12.5f, 1.2f, 0f), new Vector2(3f, 0.6f), groundLayer, platformColor);
        CreateBlock("Platform_Step2", s1.transform, new Vector3(16.5f, 3.2f, 0f), new Vector2(3f, 0.6f), groundLayer, platformColor);
        CreateBlock("Platform_Step3", s1.transform, new Vector3(20.5f, 1.8f, 0f), new Vector2(3f, 0.6f), groundLayer, platformColor);
        CreateBlock("S1_Catch_Floor", s1.transform, new Vector3(16.5f, -3.5f, 0f), new Vector2(13f, 1.5f), groundLayer, catchFloorColor);

        // Section 2: Wall Shaft
        var s2 = new GameObject("Section2_WallShaft");
        s2.transform.SetParent(levelRoot.transform, false);
        CreateBlock("Shaft_Floor", s2.transform, new Vector3(25f, -1f, 0f), new Vector2(6f, 2f), groundLayer, groundColor);
        CreateBlock("Shaft_Left_Wall", s2.transform, new Vector3(22.5f, 5.5f, 0f), new Vector2(1f, 11f), wallLayer, wallColor);
        CreateBlock("Shaft_Right_Wall", s2.transform, new Vector3(27f, 5f, 0f), new Vector2(1f, 10f), wallLayer, wallColor);
        CreateBlock("Shaft_Top_Platform", s2.transform, new Vector3(31f, 10f, 0f), new Vector2(7f, 0.6f), groundLayer, platformColor);

        // Section 3: Sword Dash Abyss
        var s3 = new GameObject("Section3_SwordDash");
        s3.transform.SetParent(levelRoot.transform, false);
        CreateBlock("Launch_Ledge", s3.transform, new Vector3(35f, 10f, 0f), new Vector2(3f, 0.6f), groundLayer, platformColor);
        CreateBlock("Abyss_Target_Wall", s3.transform, new Vector3(43f, 12f, 0f), new Vector2(1f, 4.6f), wallLayer, wallColor);
        CreateBlock("Abyss_Landing_Platform", s3.transform, new Vector3(46f, 10f, 0f), new Vector2(5f, 0.6f), groundLayer, platformColor);
        CreateBlock("High_Target_Wall", s3.transform, new Vector3(51.5f, 16.5f, 0f), new Vector2(1f, 4f), wallLayer, wallColor);
        CreateBlock("High_Rooftop_Platform", s3.transform, new Vector3(55.5f, 15f, 0f), new Vector2(7f, 0.6f), groundLayer, platformColor);
        CreateBlock("S3_Catch_Floor", s3.transform, new Vector3(45f, 1.5f, 0f), new Vector2(25f, 1.5f), groundLayer, catchFloorColor);
        CreateBlock("Pit_Return_LeftWall", s3.transform, new Vector3(36.5f, 5.5f, 0f), new Vector2(0.8f, 7f), wallLayer, wallColor);
        CreateBlock("Pit_Return_RightWall", s3.transform, new Vector3(40.5f, 5.5f, 0f), new Vector2(0.8f, 7f), wallLayer, wallColor);

        // Section 4: Descent & Goal
        var s4 = new GameObject("Section4_Descent_Goal");
        s4.transform.SetParent(levelRoot.transform, false);
        CreateBlock("Descent_Platform_1", s4.transform, new Vector3(62.5f, 13f, 0f), new Vector2(3.5f, 0.6f), groundLayer, platformColor);
        CreateBlock("Descent_Platform_2", s4.transform, new Vector3(67.5f, 9.5f, 0f), new Vector2(3.5f, 0.6f), groundLayer, platformColor);
        CreateBlock("Descent_Platform_3", s4.transform, new Vector3(72.5f, 6f, 0f), new Vector2(3.5f, 0.6f), groundLayer, platformColor);
        CreateBlock("Descent_Platform_4", s4.transform, new Vector3(77.5f, 2.5f, 0f), new Vector2(3.5f, 0.6f), groundLayer, platformColor);
        CreateBlock("Goal_Sprint_Floor", s4.transform, new Vector3(85f, -1f, 0f), new Vector2(16f, 2f), groundLayer, groundColor);
        CreateBlock("Goal_Marker_Pedestal", s4.transform, new Vector3(89f, 0.2f, 0f), new Vector2(2.5f, 0.4f), groundLayer, goalColor);
        CreateBlock("Goal_Pillar_Left", s4.transform, new Vector3(87.8f, 1.8f, 0f), new Vector2(0.4f, 2.8f), groundLayer, goalColor);
        CreateBlock("Goal_Pillar_Right", s4.transform, new Vector3(90.2f, 1.8f, 0f), new Vector2(0.4f, 2.8f), groundLayer, goalColor);
        CreateBlock("Goal_Arch_Top", s4.transform, new Vector3(89f, 3.4f, 0f), new Vector2(3f, 0.4f), groundLayer, goalColor);
        CreateBlock("Right_Border_Wall", s4.transform, new Vector3(93.5f, 4f, 0f), new Vector2(1f, 12f), groundLayer, groundColor);

        // Mark Dirty and Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("=== Automated Setup Completed Successfully! ===");
    }
}
