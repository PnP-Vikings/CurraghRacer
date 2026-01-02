# PixelWater Shader - Quick Setup Guide

The shader has been simplified and optimized for smooth, stylized water that matches your game's aesthetic.

## Recommended Settings for Smooth Stylized Water

### Basic Setup (Start Here)
```
Main Colors:
- Base Water Color: Light blue (R: 0.4, G: 0.6, B: 0.8)
- Dark Stripe Color: Darker blue (R: 0.2, G: 0.4, B: 0.6)
- Light Stripe Color: Lighter blue (R: 0.6, G: 0.8, B: 0.9)
- Foam Color: White-ish (R: 0.9, G: 0.95, B: 1.0)
- Transparency: 0.85

Wave Settings:
- Wave Speed: 0.5
- Wave Direction X: 1.0
- Wave Direction Y: 0.25
- Wave Spacing: 1.5
- Wave Strength: 0.5

Stripe Pattern:
- Number of Stripes: 10-15 (controls how many stripes you see)
- Stripe Edge Softness: 0.1 (lower = sharper stripes, higher = smoother)
- Stripe Distortion: 0.3 (adds organic wave movement)

Foam Details:
- Foam Amount: 0.2-0.4
- Foam Detail Scale: 2.0
- Foam Speed: 0.8
- Foam Cutoff: 0.7

Pixelation:
- Pixel Density: 0 (keep at 0 for smooth water, increase for pixelated look)

Style Tweaks:
- Overall Smoothness: 0.5 (higher = softer transitions)
- Color Variation: 0.2 (adds subtle movement)
- Brightness Adjust: 0 to 0.1
```

## Different Styles You Can Achieve

### 1. Calm Lake Water
- Number of Stripes: 5-8
- Wave Speed: 0.2-0.3
- Stripe Edge Softness: 0.2 (very smooth)
- Wave Strength: 0.3
- Foam Amount: 0.1

### 2. Ocean Waves
- Number of Stripes: 15-20
- Wave Speed: 0.7-1.0
- Stripe Edge Softness: 0.05 (sharper)
- Wave Strength: 0.6-0.8
- Foam Amount: 0.4-0.5

### 3. Stylized/Cartoony
- Number of Stripes: 8-12
- Wave Speed: 0.5
- Stripe Edge Softness: 0.08 (visible stripes)
- Wave Strength: 0.5
- Foam Amount: 0.3
- Overall Smoothness: 0.6
- Pixel Density: 30-50 (optional for pixel art style)

## Tips for Best Results

1. **Start Simple**: Begin with the basic setup above, then adjust one parameter at a time
2. **Stripe Count**: This is the most important setting - it controls how dense the wave pattern looks
3. **Smoothness**: Higher values = softer, more organic looking water
4. **Foam**: Keep foam amount low (0.2-0.4) for subtle effect
5. **Wave Direction**: X=1, Y=0.25 creates diagonal waves that look natural
6. **Pixelation**: Only use if you want pixel art style, otherwise keep at 0

## Common Issues

**Water looks too busy?**
- Reduce Number of Stripes (try 6-8)
- Increase Stripe Edge Softness (try 0.15-0.2)
- Reduce Stripe Distortion (try 0.2)

**Water looks too flat?**
- Increase Number of Stripes (try 12-15)
- Increase Wave Strength (try 0.6-0.7)
- Increase Color Variation (try 0.3)

**Transitions too harsh?**
- Increase Stripe Edge Softness
- Increase Overall Smoothness
- Reduce Wave Strength slightly

**Water doesn't move enough?**
- Increase Wave Speed
- Increase Stripe Distortion
- Adjust Wave Direction for different flow

## Color Selection Tips

For your game's style (based on the screenshots):
- Use clear, saturated blues
- Keep dark stripes visible but not too dark (around R:0.05, G:0.4, B:0.7)
- Light stripes should be subtle highlights
- Foam should be bright but not pure white (R:0.9, G:0.95, B:1.0)

