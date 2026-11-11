# Simulasi Pertanian 3D

Game simulasi bertani 3D bergaya *blocky* yang dibuat dengan **Unity 6** dan **C#**.
Beli benih, tanam, siram, panen, lalu jual hasil panen untuk mendapatkan skor, dan
gunakan skor itu untuk membeli benih yang lebih langka (Common → Uncommon → Rare → Legendary).

Ada dua cara menjalankan game ini:

- **[Cara 1 – Langsung main (tanpa Unity, offline)](#cara-1--langsung-main-tanpa-unity-offline)**
- **[Cara 2 – Buka project di Unity](#cara-2--buka-project-di-unity)**

---

## Cara 1 – Langsung main (tanpa Unity, offline)

Kebutuhan: Windows 10/11 64-bit.

1. Buka halaman **[Releases](../../releases)** repository ini.
2. Unduh file **`SimulasiPertanian-Windows.zip`**.
3. Klik kanan file ZIP → **Extract All…** → ekstrak ke folder mana saja.
4. Buka folder hasil ekstrak, lalu klik dua kali **`SimulasiPertanian.exe`**.
5. Jika muncul peringatan *Windows protected your PC*, klik **More info** → **Run anyway**
   (muncul karena game belum memiliki tanda tangan digital).

> Jangan memindahkan `SimulasiPertanian.exe` sendirian. File `.exe` harus tetap satu
> folder dengan `SimulasiPertanian_Data`, `MonoBleedingEdge`, dan file `.dll` lainnya.

---

## Cara 2 – Buka project di Unity

### 1. Pasang Unity

1. Unduh dan pasang **Unity Hub** dari https://unity.com/download.
2. Buka Unity Hub dan masuk dengan akun Unity (akun Personal gratis sudah cukup).
3. Tab **Installs** → **Install Editor** → pilih **Unity 6000.6.4f1**.
   Jika tidak ada di daftar, cari versi tersebut di tab **Archive**.

### 2. Unduh project

- **Download ZIP:** klik tombol hijau **Code** → **Download ZIP**, lalu ekstrak.
- **atau Git:**
  ```bash
  git clone https://github.com/MasPutram/Simulasi-Pertanian-3D.git
  ```

### 3. Buka di Unity Hub

1. Unity Hub → tab **Projects** → **Add** → **Add project from disk**.
2. Pilih folder project (yang berisi `Assets`, `Packages`, `ProjectSettings`).
3. Klik project untuk membukanya.

> Pembukaan pertama memakan waktu **± 5–15 menit** karena Unity meng-import semua aset.
> Tunggu sampai selesai.

### 4. Jalankan

1. Di jendela **Project**, buka `Assets/Scenes/` lalu klik dua kali **SampleScene**.
2. Tekan tombol **Play** (▶) di atas Unity Editor.
3. Di menu utama, klik **PLAY**.
4. Jika tampilan terpotong, atur slider **Scale** di jendela Game ke **1x**.

### (Opsional) Membuat file `.exe` sendiri

Di Unity Editor pilih menu **Farm → Build → Windows (.exe)**.
Hasilnya ada di folder `Build/Windows/`.

---

## Cara Bermain

| Tombol | Fungsi |
|---|---|
| **W A S D** / panah | Berjalan |
| **Scroll mouse** | Zoom kamera |
| **1 – 9** / klik slot | Pegang benih atau buah dari tas |
| **E** / klik plot | Tanam, siram, atau panen di plot terdekat |
| **Tab** / ikon pot | Panel sisa waktu panen |
| **F1** | Panel admin (benih gratis, tumbuhkan semua, +1000 skor, atur waktu & cuaca) |

1. Mulai dengan **skor 20** (skor = uang).
2. Berdiri di alas **rak BENIH** → toko terbuka → beli benih.
3. Tekan angka slot benih → berdiri di dekat plot kosong → tekan **E** untuk menanam.
4. Tekan **E** saat tanaman tumbuh untuk **menyiram** (5 detik lebih cepat).
5. Saat tanaman matang, tekan **E** untuk **memanen**.
6. Berdiri di alas **rak SELL** → jual buah → skor bertambah.
7. Kumpulkan skor sampai bisa membeli **Semangka (Legendary)**.

| Rarity | Tanaman | Waktu tumbuh | Harga benih | Harga jual |
|---|---|---|---|---|
| Common | Wortel | 15 detik | 5 | 12 |
| Common | Tomat | 20 detik | 8 | 18 |
| Uncommon | Stroberi | 30 detik | 20 | 45 |
| Uncommon | Jagung | 35 detik | 30 | 65 |
| Rare | Labu | 50 detik | 70 | 150 |
| Legendary | Semangka | 80 detik | 250 | 600 |
