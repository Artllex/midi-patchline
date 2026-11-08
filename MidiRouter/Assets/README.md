# Grafiki instrumentów

Źródła: C:\Users\arkad\Pictures\SYNTHS.
Pliki źródłowe pozostają bez zmian.
Przygotowanie: wbudowane imagegen, edycja background-extraction, przezroczysty PNG.
Grafiki są osadzone jako zasoby WPF.

Mapowanie:
- JP8080.jpg -> jp8080.png -> JP-8080
- Kobol.jpg -> kobol.png -> Kobol
- Model D.webp -> model-d.png -> Model D
- System 8.webp -> system-8.png -> Roland System-8

Prompt dla każdego pliku (w miejscu {file} podano jego nazwę):
Edit target: the provided photograph of {file}. Remove only the background, external shadow/reflection and empty margins. Return this exact synthesizer isolated on genuine transparent alpha, tightly framed with tiny even padding. Preserve entire instrument, proportions, perspective, original knobs, labels, lettering, colors and all white areas inside the instrument (including piano keys). Do not redesign, redraw or add anything. Asset for a Windows MIDI router instrument thumbnail.

Uwaga: obrazy przetworzone generatywnie; nie są kopią piksel w piksel zdjęć źródłowych.

Nowe grafiki (imagegen, background-extraction):
CZ-5000.jpg -> cz5000.png
Neutron.png -> neutron.png
Virus TI.jpg -> virus.png

Prompt: Background extraction edit of {file}. Keep the exact whole synthesizer and its proportions, labels, controls, colors, keys and perspective. Remove background, shadow and reflection outside instrument. Tight crop with tiny even padding. True transparent alpha PNG for application thumbnail. Do not redesign instrument.

ROLI.jpg -> roli.png -> źródło ROLI Seaboard 2.
Obróbka: imagegen, background-extraction.
Prompt: Background-extraction edit of this exact ROLI Seaboard 2 product photo. Remove only the white background and empty margins, isolate the whole controller on genuine transparent alpha. Tight crop with tiny even padding. Preserve exact proportions, top view, dark surfaces, all keys, controls, markings and logo. Do not redesign or add details. Asset for Windows MIDI router.

Nektar Impact GXP61.jpg -> nektar.png -> drugie źródło (ID b).
Obróbka: wbudowane imagegen.
Prompt: Background-extraction edit. Isolate this exact Nektar Impact GXP61 keyboard controller. Remove white background, external shadows and empty margins; tight crop, tiny padding, true transparent alpha PNG. Preserve whole instrument, proportions, perspective, all piano keys including white keys, controls, labels and logo. No redesign.

JD-800.jpg -> jd800.png oraz jd800-cutout.png w Pictures/SYNTHS.
ROLI Seaboard 2.png -> roli.png (podmiana) oraz roli-cutout.png w Pictures/SYNTHS.
Poprzednie wycięcie ROLI zachowane jako roli-v1-cutout.png w Pictures/SYNTHS.
Obróbka: wbudowane imagegen, background-extraction.
Prompt: Background-extraction edit of {file}. Isolate the exact whole instrument, remove external background and shadows, crop all empty margins leaving tiny even padding. Preserve the original proportions, perspective, exact instrument details, logo, controls, colors and all white piano keys. For ROLI preserve existing alpha and the exact supplied appearance. Genuine transparent alpha PNG, no redesign or additions.

Nord Rack 2.jpg -> nord.png oraz Pictures/SYNTHS/nord-rack2-cutout.png.
Obróbka: wbudowane imagegen.
Prompt: Background extraction edit of this exact Nord Rack photo. Isolate the complete red synthesizer, remove white background, external shadow and all empty margins, tight crop with tiny even padding. Preserve original proportions, perspective, exact panel, knobs, lettering and colors. Genuine transparent alpha PNG, no redesign.

Ikona MIDI Patchline: brand.png, app.ico.
Prompt: Create one simple flat modern Windows app icon for MIDI Patchline, a MIDI routing utility. A crisp minimalist abstract patch connection symbol: one input node on the left branches through a short geometrically bent path into two output nodes on the right. Bold even stroke weight, beautifully balanced, instantly recognizable at 16 pixels. Emerald green main path and muted warm orange secondary output. Clean flat vector-like shapes, no text, no letters, no gradients, no shadows, no glow, no 3D, no background plate. Isolated centered icon on true transparent background, symbol fills 88 percent of square canvas, generous optical spacing, professional desktop software identity.

Ikona v2: symbol połączeń z podpisem MIDI pod spodem.
Przygotowano przez imagegen. Referencje: dotychczasowa brand.png oraz dostarczony wzór liternictwa MIDI.
Prompt: Preserve the green and orange routing symbol and colors. Uniformly scale it down enough to place a centered geometric bold MIDI wordmark underneath within square canvas. Recreate lettering from supplied reference without background or watermark, silver-white with fine dark outline. Transparent alpha, no shadow or extra text.
