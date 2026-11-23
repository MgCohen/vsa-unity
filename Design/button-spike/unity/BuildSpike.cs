// One-time builder for the button spike. Run it with:
//   unity command script run --file Design/button-spike/unity/BuildSpike.cs --entry BuildSpike.<Step>
// Each number below comes by hand from tokens.css. It is not an importer.
// Unit rule: the canvas reference size is 1920 x 1080, the same as screen.html,
// so 1 CSS px = 1 canvas unit.
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BuildSpike
{
    const string Root = "Assets/UISpike";
    const string Art = Root + "/Art";

    // tokens.css, by hand
    const float ButtonWidth = 320f;        // --button-width
    const float FaceHeight = 96f;          // --button-face-height
    const float Depth = 12f;               // --button-depth (--prim-size-3)
    const float Radius = 22f;              // --button-radius (--prim-radius-lg)
    const float ScreenMargin = 64f;        // --sem-screen-margin (--prim-size-16)
    const float FontSize = 40f;            // --button-font-size
    const float LetterSpacingPx = 2f;      // --button-letter-spacing
    static readonly Color32 Label = new Color32(0x3a, 0x1d, 0x00, 0xff);         // --prim-ink-900
    static readonly Color32 LabelDisabled = new Color32(0x6b, 0x6b, 0x6b, 0xff); // --prim-gray-600

    // Step 1: TextMesh Pro shaders and settings.
    public static string ImportTmp()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return "TMP essentials already in the project";
        string pkg = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
        AssetDatabase.ImportPackage(pkg, false);
        return "import started: " + pkg;
    }

    // Step 2: sprite settings and the font asset.
    public static string PrepareAssets()
    {
        foreach (var name in new[] { "bg", "button-normal", "button-hover", "button-pressed", "button-focus", "button-disabled" })
        {
            string path = $"{Art}/{name}.png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 2048;
            // 9-slice border (left, bottom, right, top): the radius, plus the lip at the bottom.
            ti.spriteBorder = name == "bg" ? Vector4.zero : new Vector4(Radius, Radius + Depth, Radius, Radius);
            ti.SaveAndReimport();
        }

        string fontAssetPath = $"{Root}/Fonts/Inter-SemiBold SDF.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath) == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{Root}/Fonts/Inter-SemiBold.ttf");
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            fa.name = "Inter-SemiBold SDF";
            AssetDatabase.CreateAsset(fa, fontAssetPath);
            fa.atlasTextures[0].name = "Inter-SemiBold Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = "Inter-SemiBold Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            AssetDatabase.SaveAssets();
        }
        return "sprites and font asset ready";
    }

    // Step 3: the screen.
    public static string BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Background: full screen.
        var bg = NewUi("Background", canvasGo.transform);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = Sprite("bg");
        bgImg.raycastTarget = false;

        // Button: bottom-right corner, margin 64 (CSS right/bottom: 64px).
        var btnRt = NewUi("PlayButton", canvasGo.transform);
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(1, 0);
        btnRt.pivot = new Vector2(1, 0);
        btnRt.anchoredPosition = new Vector2(-ScreenMargin, ScreenMargin);
        btnRt.sizeDelta = new Vector2(ButtonWidth, FaceHeight + Depth);   // 320 x 108
        var img = btnRt.gameObject.AddComponent<Image>();
        img.sprite = Sprite("button-normal");
        img.type = Image.Type.Sliced;
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.SpriteSwap;
        btn.spriteState = new SpriteState
        {
            highlightedSprite = Sprite("button-hover"),   // CSS :hover
            pressedSprite = Sprite("button-pressed"),     // CSS :active
            selectedSprite = Sprite("button-focus"),      // CSS :focus-visible
            disabledSprite = Sprite("button-disabled"),   // CSS :disabled
        };

        // Label: the face area only (the lip is the bottom 12 px).
        var labelRt = NewUi("Label", btnRt);
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(0, Depth);
        labelRt.offsetMax = Vector2.zero;
        var tmp = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{Root}/Fonts/Inter-SemiBold SDF.asset");
        tmp.text = "Play";
        tmp.fontStyle = FontStyles.UpperCase;                       // CSS text-transform: uppercase
        tmp.fontSize = FontSize;
        tmp.characterSpacing = LetterSpacingPx / FontSize * 100f;   // TMP unit = 1/100 em, so 2 px at 40 px = 5
        tmp.color = Label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        EditorSceneManager.SaveScene(scene, $"{Root}/UISpike.unity");
        return "scene saved: " + Root + "/UISpike.unity";
    }

    static RectTransform NewUi(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{name}.png");
}
