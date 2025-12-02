# Win/Lose State Implementation - Setup Guide

## Overview
Sistem win/lose state telah berhasil diimplementasikan untuk game SumateranRoar. Game memiliki dua kondisi akhir:

- **Human Win**: Semua human players berhasil masuk ke ExitGate
- **Tiger Win**: Semua human players mati

## Files Yang Dibuat/Diubah

### 1. **GameStateManager.cs** (NEW)
Path: `Assets/Scripts/Manager/GameStateManager.cs`

Script NetworkBehaviour yang mengelola state game, termasuk:
- Tracking jumlah human, human yang exit, dan human yang mati
- Deteksi kondisi win/lose
- Load scene cutscene (HumanWin atau TigerWin)
- Singleton pattern untuk akses global

### 2. **GamePlayController.cs** (MODIFIED)
Perubahan:
- Menambahkan field `gameStateManagerPrefab` untuk di-spawn
- Method `SpawnGameStateManager()` untuk spawn/create GameStateManager
- Register setiap player (human/tiger) ke GameStateManager saat spawn
- GameStateManager di-spawn sebelum spawn players

### 3. **ExitGate.cs** (MODIFIED)
Perubahan:
- Import `Unity.Netcode`
- Method `Interact()` sekarang memanggil `GameStateManager.Instance.OnHumanExited(clientId)` saat human berhasil keluar
- Validasi bahwa hanya human yang bisa keluar (tiger ditolak)

### 4. **PlayerHealth.cs** (MODIFIED)
Perubahan:
- Method `OnPlayerDeath()` sekarang memanggil `GameStateManager.Instance.OnHumanDied(clientId)` saat human mati
- Hanya di-call di server untuk mencegah duplikasi

### 5. **EditorBuildSettings.asset** (MODIFIED)
Perubahan:
- Menambahkan `Assets/Scenes/Cutscene/HumanWin.unity` ke build settings
- Menambahkan `Assets/Scenes/Cutscene/TigerWin.unity` ke build settings

## Setup Instructions

### 1. Create GameStateManager Prefab (Recommended)

1. Di Unity Editor, buat Empty GameObject baru: `GameObject > Create Empty`
2. Rename menjadi "GameStateManager"
3. Add Component: `Network Object`
4. Add Component: `GameStateManager` (script yang baru dibuat)
5. Drag GameObject ke folder `Assets/Prefabs/` untuk membuat prefab
6. Delete dari scene (prefab akan di-spawn oleh GamePlayController)

### 2. Assign GameStateManager Prefab ke GamePlayController

1. Di scene Maps1 (atau scene gameplay Anda), cari GameObject yang memiliki `GamePlayController` component
2. Di Inspector, cari field "Game State Manager Prefab"
3. Drag prefab GameStateManager yang baru dibuat ke field tersebut

**Catatan**: Jika Anda tidak assign prefab, GamePlayController akan otomatis membuat GameStateManager secara dynamic saat runtime. Namun menggunakan prefab lebih direkomendasikan.

### 3. Verify Scene Names

Pastikan nama scene di GameStateManager cocok dengan nama file scene Anda:
- Default: `HumanWin` dan `TigerWin`
- Jika nama scene berbeda, edit di Inspector:
  1. Select GameStateManager prefab (atau instance di scene)
  2. Edit field "Human Win Scene Name" dan "Tiger Win Scene Name"

### 4. Test Win/Lose Conditions

**Testing Human Win:**
1. Start multiplayer session (Host + Client)
2. Pastikan ada RepairableObjective di scene
3. Repair semua objectives
4. Semua human players masuk ke ExitGate
5. Scene HumanWin.unity akan ter-load

**Testing Tiger Win:**
1. Start multiplayer session
2. Tiger attack semua human sampai health = 0
3. Setelah semua human mati, scene TigerWin.unity akan ter-load

## How It Works

### Flow Diagram

```
[Game Start] 
    ↓
[GamePlayController spawns GameStateManager]
    ↓
[Players spawned & registered to GameStateManager]
    ↓
[Gameplay begins]
    ↓
┌─────────────────────────────────────┐
│  Human Actions:                     │
│  - Repair objectives                │
│  - Try to exit through ExitGate     │
│  - Avoid tiger attacks              │
└─────────────────────────────────────┘
    ↓
┌──────────────── OR ────────────────┐
│                                     │
[Human exits gate]          [Human dies (health = 0)]
    ↓                                 ↓
[GameStateManager.OnHumanExited()] [GameStateManager.OnHumanDied()]
    ↓                                 ↓
[humansExited++]              [humansDead++]
    ↓                                 ↓
[Check: All exited?]          [Check: All dead?]
    ↓                                 ↓
[YES → Load HumanWin]         [YES → Load TigerWin]
```

### Network Synchronization

- **Server Authority**: GameStateManager hanya update state di server
- **NetworkVariable**: State (totalHumans, humansExited, humansDead) di-sync ke semua clients
- **Scene Loading**: NetworkManager.SceneManager.LoadScene() ensures semua clients load scene yang sama
- **ServerRpc**: Client dapat trigger events via ServerRpc jika diperlukan

## Troubleshooting

### GameStateManager tidak spawn
**Problem**: Console error "GameStateManager tidak ditemukan"
**Solution**: 
- Pastikan GameStateManager prefab di-assign di GamePlayController Inspector
- Atau script akan otomatis create jika prefab kosong

### Scene tidak ter-load saat win/lose
**Problem**: Win condition terpenuhi tapi scene tidak berubah
**Solution**:
- Pastikan scene HumanWin.unity dan TigerWin.unity ada di Build Settings
- Cek nama scene di GameStateManager Inspector cocok dengan nama file
- Cek console untuk error messages

### Human yang sudah exit masih bisa diserang
**Problem**: Gameplay logic issue
**Solution**: 
- Add logic di ExitGate untuk disable/destroy player GameObject setelah exit
- Atau teleport player ke safe zone yang tidak bisa diakses tiger

### Count tidak akurat
**Problem**: humansExited + humansDead ≠ totalHumans
**Solution**:
- Pastikan RegisterPlayer() dipanggil untuk setiap player spawn
- Cek console log untuk tracking registration

## Additional Features (Optional)

### 1. UI Display Progress
Tambahkan UI untuk menampilkan progress:
```csharp
// Example UI Script
public class GameProgressUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI progressText;
    
    void Update()
    {
        if (GameStateManager.Instance != null)
        {
            int total = GameStateManager.Instance.GetTotalHumans();
            int exited = GameStateManager.Instance.GetHumansExited();
            int dead = GameStateManager.Instance.GetHumansDead();
            int living = GameStateManager.Instance.GetLivingHumans();
            
            progressText.text = $"Humans: {living} alive | {exited} escaped | {dead} dead";
        }
    }
}
```

### 2. Return to Main Menu from Cutscene
Tambahkan button di HumanWin/TigerWin scene:
```csharp
public void ReturnToMainMenu()
{
    if (NetworkManager.Singleton != null)
    {
        NetworkManager.Singleton.Shutdown();
    }
    SceneManager.LoadScene("MainMenu");
}
```

### 3. Replay Functionality
Tambahkan button untuk replay game:
```csharp
public void PlayAgain()
{
    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Maps1", LoadSceneMode.Single);
    }
}
```

## Notes

- GameStateManager menggunakan Singleton pattern untuk akses mudah dari script lain
- Semua state changes di-handle di server untuk prevent cheating
- Scene transitions menggunakan NetworkManager.SceneManager untuk network sync
- Tiger tidak dapat exit melalui ExitGate (validasi di ExitGate.Interact())
- Human death trigger delay 3 detik (sesuai death animation duration di PlayerHealth)

## Future Improvements

1. **Partial Win Condition**: Win jika ≥ 50% humans escape
2. **Time Limit**: Auto tiger win jika waktu habis
3. **Statistics Tracking**: Track kill count, escape time, etc.
4. **Replay System**: Save dan replay game sessions
5. **Spectator Mode**: Dead players jadi spectator
6. **Victory/Defeat Animations**: Custom animations di cutscene scenes
7. **Sound Effects**: Add victory/defeat sound effects
8. **Particle Effects**: Celebratory effects saat win

---

**Created**: December 2025
**Version**: 1.0
**Compatible with**: Unity Netcode for GameObjects
