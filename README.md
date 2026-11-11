# Simulasi Pertanian

Game simulasi bertani 3D bergaya *blocky*, dibuat dengan **Unity 6** dan **C#** untuk tugas
Ujian Tengah Semester mata kuliah **Pemrograman Mobile Lanjut**,
Teknik Informatika, Universitas Muhammadiyah Purwokerto.

Pemain membeli benih, menanam, menyiram, memanen, lalu menjual hasil panen untuk
mendapatkan skor. Skor dipakai untuk membeli benih dengan *rarity* lebih tinggi
(Common, Uncommon, Rare, Legendary).

---

## Daftar Isi

1. [Fitur](#fitur)
2. [Kebutuhan Sistem](#kebutuhan-sistem)
3. [Tutorial Pemasangan](#tutorial-pemasangan)
4. [Cara Bermain](#cara-bermain)
5. [Struktur Project](#struktur-project)
6. [Masalah yang Sering Muncul](#masalah-yang-sering-muncul)
7. [Sumber Aset](#sumber-aset)

---

## Fitur

- Main menu dengan latar kebun yang di-blur dan musik latar
- Toko benih otomatis terbuka saat berdiri di area rak benih (beli +1 / +5)
- Tas / hotbar 9 slot untuk benih dan buah (tombol 1–9)
- 6 tanaman dengan 5 fase pertumbuhan: Wortel, Tomat, Stroberi, Jagung, Labu, Semangka
- Menyiram tanaman dengan animasi gembor dan partikel air
- Panen, lalu jual di rak jual (jual yang dipegang / jual semua)
- Siklus siang (2 menit) dan malam (1 menit) dengan bar hitung mundur
- Animasi karakter: berjalan, memegang benih di depan dada, mengangkat buah di atas kepala
- Panel sisa waktu panen dan panel admin untuk pengujian

| Rarity | Tanaman | Waktu tumbuh | Harga benih | Harga jual |
|---|---|---|---|---|
| Common | Wortel | 15 detik | 5 | 12 |
| Common | Tomat | 20 detik | 8 | 18 |
| Uncommon | Stroberi | 30 detik | 20 | 45 |
| Uncommon | Jagung | 35 detik | 30 | 65 |
| Rare | Labu | 50 detik | 70 | 150 |
| Legendary | Semangka | 80 detik | 250 | 600 |

---

## Kebutuhan Sistem

| Komponen | Keterangan |
|---|---|
| Unity Editor | **6000.6.4f1** (Unity 6). Versi Unity 6 lain kemungkinan bisa, tetapi belum diuji |
| Unity Hub | Versi terbaru |
| Sistem operasi | Windows 10/11 (sudah diuji), macOS / Linux seharusnya bisa |
| RAM | Minimal 8 GB |
| Ruang disk | ± 3 GB (project + folder `Library` yang dibuat Unity) |
| Git | Opsional, hanya jika memakai cara clone |

---

## Tutorial Pemasangan

### Langkah 1 – Pasang Unity Hub dan Unity Editor

1. Unduh dan pasang **Unity Hub** dari https://unity.com/download.
2. Buka Unity Hub, masuk dengan akun Unity (akun gratis / Personal sudah cukup).
3. Buka tab **Installs** → klik **Install Editor**.
4. Pilih **Unity 6000.6.4f1**. Jika tidak ada di daftar, buka tab **Archive** →
   *download archive* dan cari versi tersebut.
5. (Opsional) Centang modul **Android Build Support** jika ingin membuat APK.

### Langkah 2 – Unduh project dari GitHub

Pilih **salah satu** cara berikut.

**Cara A – Download ZIP (paling mudah, tanpa Git)**

1. Di halaman repository ini, klik tombol hijau **Code** → **Download ZIP**.
2. Ekstrak file ZIP ke folder yang mudah ditemukan, misalnya `D:\UnityProjects\`.
3. Pastikan di dalam folder hasil ekstrak terdapat folder `Assets`, `Packages`,
   dan `ProjectSettings`.

**Cara B – Clone dengan Git**

```bash
git clone <URL-repository-ini>
```

### Langkah 3 – Buka project di Unity Hub

1. Buka Unity Hub → tab **Projects** → klik **Add** → **Add project from disk**.
2. Pilih folder project (folder yang berisi `Assets`, `Packages`, `ProjectSettings`).
3. Jika muncul peringatan versi Editor, pilih **6000.6.4f1**.
4. Klik project untuk membukanya.

> **Pembukaan pertama akan lama (± 5–15 menit).** Unity sedang membuat ulang folder
> `Library` dan meng-import semua aset. Ini normal, tunggu sampai selesai.

### Langkah 4 – Buka scene utama

1. Di jendela **Project**, buka folder `Assets/Scenes/`.
2. Klik dua kali **SampleScene**.

### Langkah 5 – Jalankan game

1. Tekan tombol **Play** (▶) di bagian atas Unity Editor.
2. Di menu utama, klik **PLAY**.
3. Agar tampilan pas, di toolbar jendela **Game** atur ukuran ke **16:9** dan geser
   slider **Scale** ke **1x**.

### (Opsional) Langkah 6 – Build ke Android

1. **File → Build Profiles** → pilih **Android** → **Switch Platform**.
2. Hubungkan HP Android (mode developer + USB debugging aktif), lalu klik **Build And Run**.

> Catatan: kontrol sentuh belum tersedia (masih memakai keyboard dan mouse),
> sehingga build Android saat ini hanya untuk pengujian tampilan.

---

## Cara Bermain

| Tombol | Fungsi |
|---|---|
| **W A S D** / panah | Berjalan |
| **Scroll mouse** | Zoom kamera |
| **1 – 9** / klik slot | Pegang benih atau buah dari tas |
| **E** / klik plot | Tanam, siram, atau panen di plot terdekat |
| **Tab** / ikon pot | Buka panel sisa waktu panen |
| **F1** | Panel admin (beli semua benih, tumbuhkan semua, +1000 skor, atur waktu & cuaca) |

Alur permainan:

1. Mulai dengan **skor 20** (skor = uang).
2. Berdiri di alas **rak BENIH** → toko terbuka → beli benih.
3. Pegang benih (tombol angka) → berdiri di dekat plot kosong → tekan **E** untuk menanam.
4. Tekan **E** lagi saat tanaman tumbuh untuk **menyiram** (+5 detik lebih cepat).
5. Saat tanaman matang, tekan **E** untuk **memanen**. Buah masuk ke tas.
6. Berdiri di alas **rak SELL** → jual buah → skor bertambah.
7. Kumpulkan skor untuk membeli benih langka sampai **Semangka (Legendary)**.

---

## Struktur Project

```
Assets/
├── Data/            Data tanaman (ScriptableObject PlantData)
├── Materials/       Material Standard untuk tanaman, alat, dan lingkungan
├── Resources/Audio/ Musik latar dan efek suara
├── Scenes/          SampleScene (scene utama)
├── Scripts/         Script C# runtime
│   └── Editor/      Alat bantu editor (menu "Farm" di Unity)
└── UI/              Tekstur bingkai UI yang dibuat lewat script
```

Script utama:

| Script | Fungsi |
|---|---|
| `GameManager.cs` | Skor, tas benih/buah, beli dan jual |
| `PlotManager.cs` | Status plot dan fase pertumbuhan |
| `PlayerController.cs`, `PlayerAnimator.cs` | Gerak dan animasi pose karakter |
| `PlayerInteraction.cs` | Interaksi tombol E dan deteksi area rak |
| `WeatherManager.cs`, `DayNightBarUI.cs` | Siklus siang/malam dan pencahayaan |
| `SeedSelectionUI.cs`, `InventoryUI.cs` | Toko benih, hotbar, panel jual |
| `MainMenu.cs`, `UIManager.cs` | Menu utama dan HUD |

Desain tanaman dapat diubah langsung di scene melalui objek `Design_[Nama]`
(fase tumbuh) dan `seed_[Nama]` / `fruit_[Nama]` (model yang dipegang).
Objek-objek ini sengaja dinonaktifkan; aktifkan sementara untuk mengedit.

---

## Masalah yang Sering Muncul

| Masalah | Solusi |
|---|---|
| Muncul dialog **Safe Mode** / error merah di Console | Pastikan versi Editor **6000.6.4f1**, lalu tutup dan buka ulang project |
| Scene kosong / hanya langit | Buka `Assets/Scenes/SampleScene` (Langkah 4) |
| Tampilan game terpotong | Atur slider **Scale** di jendela Game ke **1x** |
| Tidak ada suara | Pastikan tombol **Mute Audio** di toolbar jendela Game tidak aktif |
| Pesan *Scene Backup Detected* | Muncul jika Unity sebelumnya tertutup paksa; pilih **Delete** jika scene normal |

---

## Sumber Aset

- **Model 3D** (karakter, tanaman, rak, gembor, lingkungan) dan **tekstur UI**:
  dibuat sendiri di Unity.
- **Musik latar dan efek suara**: dari
  [Grow a Garden Wiki – Music and SFX](https://growagarden.fandom.com/wiki/Music_and_SFX).
  Hak cipta milik pengembang Grow a Garden (Roblox); digunakan hanya untuk keperluan
  tugas akademik non-komersial. Suara menyiram disintesis melalui script.
- **Referensi gaya** (inspirasi visual saja): Grow a Garden (Roblox) dan Stardew Valley.
- Pengembangan dibantu asisten AI (Claude Code); seluruh hasil diuji oleh anggota kelompok.

---

## Tim

| Nama | NIM | Peran |
|---|---|---|
| [Nama Anggota 1] | [NIM] | [Peran] |
| [Nama Anggota 2] | [NIM] | [Peran] |
