// Encodes the touch frames to an MP4 with Unity's built-in MediaEncoder (no install).
// Run: write the name to Temp/encode-name.txt, then
//   unity command script eval --file Design/button-spike/unity/EncodeVideo.cs --timeout 300
using System.IO;
using System.Linq;
using UnityEditor.Media;
using UnityEngine;

const string Dir = "Temp/touch-frames";
// The output name comes from Temp/encode-name.txt, for example "unity-touch".
var Out = $"Design/button-spike/exports/{File.ReadAllText("Temp/encode-name.txt").Trim()}.mp4";
var files = Directory.GetFiles(Dir, "f*.png").OrderBy(f => f).ToArray();
var first = new Texture2D(2, 2);
first.LoadImage(File.ReadAllBytes(files[0]));
var attr = new VideoTrackAttributes
{
    frameRate = new MediaRational(30),
    width = (uint)first.width,
    height = (uint)first.height,
    includeAlpha = false,
    bitRateMode = UnityEditor.VideoBitrateMode.High,
};
if (File.Exists(Out)) File.Delete(Out);
using (var enc = new MediaEncoder(Out, attr))
{
    var tex = new Texture2D(first.width, first.height, TextureFormat.RGBA32, false);
    foreach (var f in files)
    {
        var img = new Texture2D(2, 2);
        img.LoadImage(File.ReadAllBytes(f));
        tex.SetPixels32(img.GetPixels32());
        tex.Apply();
        enc.AddFrame(tex);
        Object.DestroyImmediate(img);
    }
}
return $"{files.Length} frames, {first.width}x{first.height} -> {Out}";
