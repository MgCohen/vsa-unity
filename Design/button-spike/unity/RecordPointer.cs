// Records a touch through the real uGUI input path: ScriptedInput -> StandaloneInputModule
// -> raycast -> events. Nothing calls the button directly.
// Needs Play Mode in UISpike.unity and a 1920 x 1080 Game view.
// Run: unity command script eval --file Design/button-spike/unity/RecordPointer.cs --timeout 300
using System.Collections.Generic;
using System.IO;
using TMPro;
using UISpike;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

const string Dir = "Temp/touch-frames";
if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
Directory.CreateDirectory(Dir);

var canvas = GameObject.Find("Canvas").GetComponent<RectTransform>();
var button = GameObject.Find("PlayButton").GetComponent<Button>();
var es = EventSystem.current;
var module = es.GetComponent<StandaloneInputModule>();
var input = es.gameObject.AddComponent<ScriptedInput>();
module.inputOverride = input;
es.SetSelectedGameObject(null);
button.interactable = true;

var touch = new GameObject("TouchMark", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
touch.SetParent(canvas, false);
touch.anchorMin = touch.anchorMax = Vector2.zero;
var touchImg = touch.GetComponent<Image>();
touchImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
touchImg.color = new Color(1, 1, 1, 0.55f);
touchImg.raycastTarget = false;

var cap = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
cap.SetParent(canvas, false);
cap.anchorMin = cap.anchorMax = cap.pivot = Vector2.zero;
cap.anchoredPosition = new Vector2(64, 64);
cap.sizeDelta = new Vector2(1400, 110);
var capText = cap.GetComponent<TextMeshProUGUI>();
capText.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UISpike/Fonts/Inter-SemiBold SDF.asset");
capText.fontSize = 32;
capText.color = Color.white;
capText.raycastTarget = false;

int clicks = 0;
button.onClick.AddListener(() => clicks++);

// Screen = canvas units here, because the Game view is 1920 x 1080.
Vector2 start = new Vector2(1250, 560);
Vector2 miss = new Vector2(1500, 124);          // 36 px left of the button
Vector2 target = new Vector2(1696, 124);        // centre of the face
Vector2 away = new Vector2(1250, 300);
Vector2 pos = start;

string Under()
{
    var hits = new List<RaycastResult>();
    es.RaycastAll(new PointerEventData(es) { position = pos }, hits);
    return hits.Count > 0 ? hits[0].gameObject.name : "nothing";
}
string Sprite() => !button.interactable ? "disabled"
    : (button.image.overrideSprite != null ? button.image.overrideSprite.name : button.image.sprite.name).Replace("button-", "");

int frame = 0;
async System.Threading.Tasks.Task Shot()
{
    input.MoveTo(pos);
    touch.anchoredPosition = pos;
    int f = Time.frameCount;
    await Wait.Until(() => Time.frameCount > f);       // the module reads the input in this frame
    capText.text = $"real input path\nunder pointer: {Under()}   sprite: {Sprite()}   onClick: {clicks}";
    ScreenCapture.CaptureScreenshot($"{Dir}/f{frame:D4}.png");
    f = Time.frameCount;
    await Wait.Until(() => Time.frameCount > f + 1);
    frame++;
}
async System.Threading.Tasks.Task Move(Vector2 b, int n)
{
    var a = pos;
    for (int i = 1; i <= n; i++) { float t = i / (float)n; t = t * t * (3 - 2 * t); pos = Vector2.Lerp(a, b, t); await Shot(); }
}
async System.Threading.Tasks.Task Hold(int n) { for (int i = 0; i < n; i++) await Shot(); }
async System.Threading.Tasks.Task Tap(int hold)
{
    input.Press(); touch.sizeDelta = Vector2.one * 80; await Shot();
    await Hold(hold);
    input.Release(); touch.sizeDelta = Vector2.one * 56; await Shot();
}

touch.sizeDelta = Vector2.one * 56;
await Hold(10);
await Move(miss, 18);
await Tap(8);              // a tap outside the button: no click expected
int clicksAfterMiss = clicks;
await Hold(8);
await Move(target, 10);
await Hold(12);            // hover
await Tap(16);             // a tap on the button
await Hold(16);
await Move(away, 18);
await Hold(10);
button.interactable = false;
await Hold(18);

module.inputOverride = null;
Object.Destroy(input);
Object.Destroy(touch.gameObject); Object.Destroy(cap.gameObject);
return $"{frame} frames, clicks after miss {clicksAfterMiss}, clicks at end {clicks}";
