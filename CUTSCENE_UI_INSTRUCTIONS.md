# Instruksi Menambahkan Tombol Back to Home di Cutscene

## Langkah 1: Buka Scene HumanWin.unity
1. Di Unity Editor, buka scene `Assets/Scenes/Cutscene/HumanWin.unity`

## Langkah 2: Buat UI Canvas dan Button
1. **Buat Canvas:**
   - Klik kanan di Hierarchy → UI → Canvas
   - Canvas otomatis akan membuat EventSystem (jika belum ada, biarkan yang sudah ada)

2. **Tambahkan CutsceneUIController:**
   - Pilih Canvas di Hierarchy
   - Di Inspector, klik "Add Component"
   - Cari dan tambahkan script "CutsceneUIController"
   - Script ini akan otomatis unlock cursor saat scene dimulai

3. **Buat Button:**
   - Klik kanan pada Canvas di Hierarchy → UI → Button - TextMeshPro
   - Rename button menjadi "BackToHomeButton"
   - Posisikan button (contoh: bottom-left corner):
     - Pilih button di Hierarchy
     - Di RectTransform:
       - Anchor Presets: Bottom-Left
       - Pos X: 150, Pos Y: 50
       - Width: 200, Height: 60

4. **Atur Text Button:**
   - Expand "BackToHomeButton" di Hierarchy
   - Pilih child "Text (TMP)"
   - Di TextMeshPro component, ubah text menjadi: "Back to Home"
   - Atur font size, alignment, dll sesuai keinginan

5. **Hubungkan Button ke Function:**
   - Pilih "BackToHomeButton" di Hierarchy
   - Di Inspector, scroll ke Button component
   - Di section "On Click ()":
     - Klik tombol "+"
     - Drag object "Canvas" ke field yang muncul
     - Di dropdown function, pilih: CutsceneUIController → BackToHome()

6. **Save Scene:**
   - Ctrl+S atau File → Save

## Langkah 3: Ulangi untuk TigerWin.unity
1. Buka scene `Assets/Scenes/Cutscene/TigerWin.unity`
2. Ulangi Langkah 2 (semua sub-langkah yang sama)
3. Save scene

## Langkah 4: Test
1. Masuk ke Play Mode
2. Trigger kondisi win/lose untuk masuk ke cutscene
3. Verifikasi:
   - Cursor terlihat dan tidak terkunci
   - Button "Back to Home" muncul di layar
   - Klik button akan kembali ke StartMenu

## Alternatif: Buat Prefab (Lebih Efisien)
Jika ingin lebih mudah, Anda bisa:
1. Buat Canvas dengan button di HumanWin seperti instruksi di atas
2. Drag Canvas dari Hierarchy ke folder Assets/Prefabs untuk membuat prefab
3. Nama prefab: "CutsceneUICanvas"
4. Di TigerWin, cukup drag prefab "CutsceneUICanvas" ke Hierarchy
5. Save kedua scene

## Troubleshooting
- **Cursor masih terkunci:** Pastikan CutsceneUIController sudah terpasang di Canvas dan script ada di folder Scripts/Utility
- **Button tidak bisa diklik:** Pastikan EventSystem ada di scene dan GraphicRaycaster terpasang di Canvas
- **Error saat klik button:** Pastikan function BackToHome() sudah terhubung dengan benar di Button component
