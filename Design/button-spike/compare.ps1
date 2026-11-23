# Compares the HTML and Unity screens of the button spike.
# Reports the button box, the label box and the mean color difference near the button.
param(
  [string]$A = "$PSScriptRoot\exports\screen-html.png",
  [string]$B = "$PSScriptRoot\exports\screen-unity.png"
)
Add-Type -AssemblyName System.Drawing

function Measure-Screen([string]$path) {
  $bmp = [System.Drawing.Bitmap]::FromFile($path)
  # Search only the bottom-right area, where the button is.
  $x0 = 1400; $y0 = 850
  $btn = @{ l = 9999; t = 9999; r = -1; b = -1 }
  $lab = @{ l = 9999; t = 9999; r = -1; b = -1 }
  for ($y = $y0; $y -lt $bmp.Height; $y++) {
    for ($x = $x0; $x -lt $bmp.Width; $x++) {
      $c = $bmp.GetPixel($x, $y)
      # Button: gold face or brown lip (red clearly above blue).
      if ($c.R -gt 110 -and $c.R -gt $c.B + 60) {
        if ($x -lt $btn.l) { $btn.l = $x }; if ($x -gt $btn.r) { $btn.r = $x }
        if ($y -lt $btn.t) { $btn.t = $y }; if ($y -gt $btn.b) { $btn.b = $y }
      }
    }
  }
  # Label: dark ink pixels inside the face (not the lip at the bottom).
  for ($y = $btn.t + 10; $y -le $btn.b - 22; $y++) {
    for ($x = $btn.l + 26; $x -le $btn.r - 26; $x++) {
      $c = $bmp.GetPixel($x, $y)
      if ($c.R -lt 150 -and $c.G -lt 100) {
        if ($x -lt $lab.l) { $lab.l = $x }; if ($x -gt $lab.r) { $lab.r = $x }
        if ($y -lt $lab.t) { $lab.t = $y }; if ($y -gt $lab.b) { $lab.b = $y }
      }
    }
  }
  [pscustomobject]@{ Bmp = $bmp; Button = $btn; Label = $lab }
}

function Fmt($r) { "x {0}-{1}  y {2}-{3}  size {4}x{5}" -f $r.l, $r.r, $r.t, $r.b, ($r.r - $r.l + 1), ($r.b - $r.t + 1) }

$ma = Measure-Screen $A
$mb = Measure-Screen $B
"HTML   button: " + (Fmt $ma.Button)
"Unity  button: " + (Fmt $mb.Button)
"HTML   label:  " + (Fmt $ma.Label)
"Unity  label:  " + (Fmt $mb.Label)

# Mean absolute difference (0-255) in the button area and in a background sample.
function MeanDiff($l, $t, $r, $b) {
  $sum = 0; $n = 0
  for ($y = $t; $y -le $b; $y += 1) { for ($x = $l; $x -le $r; $x += 1) {
    $p = $ma.Bmp.GetPixel($x, $y); $q = $mb.Bmp.GetPixel($x, $y)
    $sum += [Math]::Abs($p.R - $q.R) + [Math]::Abs($p.G - $q.G) + [Math]::Abs($p.B - $q.B); $n += 3 } }
  [Math]::Round($sum / $n, 2)
}
$bb = $ma.Button
"Mean diff, button area: " + (MeanDiff $bb.l $bb.t $bb.r $bb.b)
"Mean diff, background sample (800-1000, 400-600): " + (MeanDiff 800 400 1000 600)
$ma.Bmp.Dispose(); $mb.Bmp.Dispose()
