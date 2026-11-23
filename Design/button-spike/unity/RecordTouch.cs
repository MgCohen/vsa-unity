// Records a touch on the spike button, one PNG for each frame.
// Needs Play Mode in UISpike.unity and a 1920 x 1080 Game view.
// Run: unity command script eval --file Design/button-spike/unity/RecordTouch.cs --timeout 300
using System.IO;
using TMPro;
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
es.SetSelectedGameObject(null);
button.interactable = true;

// A touch mark: a soft white circle. It is not part of the design, only for the video.
var touch = new GameObject("TouchMark", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
touch.SetParent(canvas, false);
touch.anchorMin = touch.anchorMax = Vector2.zero;
var touchImg = touch.GetComponent<Image>();
touchImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
touchImg.color = new Color(1, 1, 1, 0.55f);
touchImg.raycastTarget = false;

// A caption with the uGUI state.
var cap = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
cap.SetParent(canvas, false);
cap.anchorMin = cap.anchorMax = cap.pivot = new Vector2(0, 0);
cap.anchoredPosition = new Vector2(64, 64);
cap.sizeDelta = new Vector2(1200, 60);
var capText = cap.GetComponent<TextMeshProUGUI>();
capText.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UISpike/Fonts/Inter-SemiBold SDF.asset");
capText.fontSize = 36;
capText.color = Color.white;
capText.raycastTarget = false;

int clicks = 0;
button.onClick.AddListener(() => clicks++);

Vector2 start = new Vector2(1250, 560);
Vector2 target = new Vector2(1536 + 160, 64 + 12 + 48);   // centre of the face
Vector2 away = new Vector2(1250, 300);
var ped = new PointerEventData(es) { button = PointerEventData.InputButton.Left };

string StateName()
{
    if (!button.interactable) return "Disabled";
    var sprite = button.image.overrideSprite != null ? button.image.overrideSprite.name : button.image.sprite.name;
    return sprite.Replace("button-", "");
}

int frame = 0;
async System.Threading.Tasks.Task Shot()
{
    capText.text = $"uGUI sprite: {StateName()}    onClick: {clicks}";
    ScreenCapture.CaptureScreenshot($"{Dir}/f{frame:D4}.png");
    int f = Time.frameCount;
    await Wait.Until(() => Time.frameCount > f + 1);
    frame++;
}

async System.Threading.Tasks.Task Move(Vector2 a, Vector2 b, int n, float size)
{
    for (int i = 1; i <= n; i++)
    {
        float t = i / (float)n; t = t * t * (3 - 2 * t);
        touch.anchoredPosition = Vector2.Lerp(a, b, t);
        touch.sizeDelta = Vector2.one * size;
        await Shot();
    }
}
async System.Threading.Tasks.Task Hold(int n) { for (int i = 0; i < n; i++) await Shot(); }

touch.anchoredPosition = start; touch.sizeDelta = Vector2.one * 56;
await Hold(15);
await Move(start, target, 20, 56);
ExecuteEvents.Execute(button.gameObject, ped, ExecuteEvents.pointerEnterHandler);
await Hold(15);
ped.pressPosition = target; ped.pointerPress = button.gameObject;
ExecuteEvents.Execute(button.gameObject, ped, ExecuteEvents.pointerDownHandler);
touch.sizeDelta = Vector2.one * 80;
await Hold(20);
ExecuteEvents.Execute(button.gameObject, ped, ExecuteEvents.pointerUpHandler);
ExecuteEvents.Execute(button.gameObject, ped, ExecuteEvents.pointerClickHandler);
touch.sizeDelta = Vector2.one * 56;
await Hold(20);
ExecuteEvents.Execute(button.gameObject, ped, ExecuteEvents.pointerExitHandler);
await Move(target, away, 20, 56);
await Hold(15);
button.interactable = false;
await Hold(25);

Object.Destroy(touch.gameObject); Object.Destroy(cap.gameObject);
return $"{frame} frames, clicks {clicks}";
