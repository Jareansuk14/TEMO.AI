# Admin Host Configuration (TEMO.AI)

เอกสารนี้อธิบายวิธี **เพิ่ม / ลบ / แก้ไข Admin Backend Host** สำหรับโปรแกรม TEMO.AI  
Host ปัจจุบัน: `temo-backend.onrender.com`

---

## ภาพรวมระบบป้องกัน

Admin host ถูกเก็บแบบเข้ารหัส และตรวจสอบ **2 จุดอิสระ** — ต้องแก้ทั้งสองจุดพร้อมกัน host ใหม่ถึงจะใช้งานได้

| จุด | ไฟล์ | หน้าที่ |
|-----|------|--------|
| **1** | `Services/AdminEndpointProtector.cs` | ถอด host + สร้าง URL endpoint |
| **2** | `Services/AdminCertificatePinning.cs` | ตรวจ host ตอน HTTPS + pin certificate |

`AdminHttpClientFactory` จะ cross-check ว่า host จากจุดที่ 1 ตรงกับจุดที่ 2 ก่อนสร้าง HttpClient  
`AuthApiService` เรียก login ผ่าน `HostConfig.AdminLoginUrl` เท่านั้น (ไม่มี hardcoded URL)

---

## ไฟล์ที่เกี่ยวข้อง

```
TEMO.AI/
  Services/
    AdminEndpointProtector.cs    ← จุดที่ 1 (host + endpoints)
    AdminCertificatePinning.cs   ← จุดที่ 2 (host hash + SPKI pin)
    AdminHttpClientFactory.cs    ← สร้าง HttpClient (ไม่ต้องแก้ host ที่นี่)
    HostConfig.cs                ← เรียกผ่าน AdminEndpointProtector (ไม่ต้องใส่ URL ตรงๆ)
    AuthApiService.cs            ← login API
    RuntimeGuard.cs              ← anti-debug (Release)
    ToolDetectionService.cs      ← ตรวจ RE tools + background monitor
  build/
    confuser.crproj              ← ConfuserEx obfuscation config
  build-velopack.bat             ← Release build + obfuscate + Velopack
tools/
  admin-host-gen/                ← สคริปต์ generate ค่าเข้ารหัส
  fetch-spki-pin.ps1             ← ดึง SPKI pin จาก host จริง
  install-confuser.ps1           ← ติดตั้ง ConfuserEx (เรียกจาก build script)
```

---

## การเข้ารหัส / ถอดรหัส (ทำความเข้าใจ)

### 1) Host (AdminEndpointProtector)

```
KeyPartA + KeyPartB + KeyPartC
        ↓ XOR รวมกัน
        ↓ PBKDF2 (12,000 rounds, KeySalt)
        ↓ AES-256 key
HostCipher + HostIv → ถอดรหัส → hostname จริง
        ↓
SHA256(hostname) ต้องตรงกับ HostHash
```

- **HostCipher / HostIv** — host ที่เข้ารหัสด้วย AES
- **HostHash** — SHA256 ของ host plain text (กันสวม host อื่น)
- **IntegrityHash** — hash ของ key + cipher blobs (กันแก้ไขไฟล์)

### 2) Endpoint paths (AdminEndpointProtector)

Path แต่ละตัว เช่น `/api/auth/login` เก็บเป็น byte array ที่ XOR ด้วย **seed ต่างกัน**:

```
plain[i] XOR (seed + i*5 + i%7) = encoded[i]
```

ถ้า **แก้แค่ host ไม่แก้ path** — ไม่ต้องเปลี่ยนค่า `Seg*` ในไฟล์

### 3) Host lock จุดที่ 2 (AdminCertificatePinning)

```
SHA256(hostname) XOR 0x7E ทุก byte = AllowedHostHash
```

เก็บแยกจากจุดที่ 1 โดยเจตนา — patch จุดใดจุดหนึ่งอย่างเดียวยังใช้ไม่ได้

### 4) Certificate SPKI pin

```
SHA256(certificate public key) → hex string → XOR ด้วย (0xC3 + i*11) = SpkiPin
```

ใช้กัน MITM แม้ host ถูกต้อง แต่ cert ไม่ใช่ของ server จริง

---

## วิธีแก้ไข Host (เปลี่ยน domain)

### ขั้นตอนที่ 1 — Generate ค่าใหม่

จาก root โปรเจกต์ (`New/`):

```powershell
dotnet run --project tools/admin-host-gen/admin-host-gen.csproj -- temo-backend.onrender.com
```

แทน `temo-backend.onrender.com` ด้วย host ใหม่ (ไม่ใส่ `https://`)

สคริปต์จะพิมพ์ค่าที่ต้องนำไปใส่:
- `HostIv`, `HostCipher`, `HostHash`, `IntegrityHash`
- `AllowedHostHash`

### ขั้นตอนที่ 2 — อัปเดตจุดที่ 1

ไฟล์: `Services/AdminEndpointProtector.cs`

แทนที่ 4 array:
- `HostIv`
- `HostCipher`
- `HostHash`
- `IntegrityHash`

### ขั้นตอนที่ 3 — อัปเดตจุดที่ 2 (host hash)

ไฟล์: `Services/AdminCertificatePinning.cs`

แทนที่:
- `AllowedHostHash`

### ขั้นตอนที่ 4 — อัปเดต SPKI pin (หลัง host ขึ้น HTTPS แล้ว)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/fetch-spki-pin.ps1 -HostName temo-backend.onrender.com
```

นำค่า `SpkiPin` ไปใส่ใน `Services/AdminCertificatePinning.cs`

> ถ้า Render หมุน certificate ใหม่ ต้องรันขั้นตอนนี้ซ้ำ

### ขั้นตอนที่ 5 — Build และทดสอบ

```powershell
dotnet build TEMO.AI/TEMO.AI.csproj
```

ทดสอบ:
- Login ผ่านหน้า LoginWindow
- Auto-update (Velopack)

Release build:

```powershell
cd TEMO.AI
.\build-velopack.bat
```

(build script จะ: `dotnet build` → ConfuserEx obfuscate → single-file publish → Velopack pack)

---

## วิธีเพิ่ม Endpoint ใหม่

1. เพิ่ม path ในสคริปต์ `tools/admin-host-gen/Program.cs` (ฟังก์ชัน `PrintPath`)
2. รัน generator เพื่อดู byte array ของ path ใหม่
3. เพิ่ม `(byte Seed, byte[] Data) SegXxx` ใน `AdminEndpointProtector.cs`
4. เพิ่ม property ใน `AdminEndpointProtector` และ `HostConfig.cs`
5. เรียกใช้จาก service ที่ต้องการ (ผ่าน `AdminHttpClientFactory.Create()`)

---

## วิธีลบ Endpoint

1. ลบ `Seg*` และ property ที่เกี่ยวข้องใน `AdminEndpointProtector.cs`
2. ลบ property ใน `HostConfig.cs`
3. ลบการเรียกใช้ใน service / UI

---

## Checklist เมื่อเปลี่ยน Host

- [ ] รัน `admin-host-gen` ด้วย host ใหม่
- [ ] อัปเดต `AdminEndpointProtector.cs` (4 ค่า)
- [ ] อัปเดต `AdminCertificatePinning.cs` → `AllowedHostHash`
- [ ] Deploy host ใหม่ + HTTPS พร้อมใช้
- [ ] รัน `fetch-spki-pin.ps1` → อัปเดต `SpkiPin`
- [ ] Build + ทดสอบ login
- [ ] Release build ผ่าน `build-velopack.bat`

---

## ข้อควรระวัง

| หัวข้อ | รายละเอียด |
|--------|------------|
| แก้จุดเดียวไม่พอ | ต้อง sync ทั้ง `AdminEndpointProtector` และ `AdminCertificatePinning` |
| DEBUG vs RELEASE | DEBUG ข้าม cert pinning / integrity บางส่วน — ทดสอบ Release ก่อนปล่อย |
| ConfuserEx | หลัง obfuscate ต้องทดสอบ login อีกครั้ง |
| Host ไม่มี HTTPS | ดึง SPKI pin ไม่ได้จนกว่า host จะ live |
| X-Client-Type | Request ส่ง header `X-Client-Type: TEMO.AI` — backend ต้องรองรับ |

---

## Host / Endpoint ปัจจุบันของ TEMO.AI

| รายการ | ค่า |
|--------|-----|
| Host | `temo-backend.onrender.com` |
| API base | `/api` |
| Login | `/api/auth/login` |

---

## Security stack (สรุป)

| ชั้น | รายละเอียด |
|------|------------|
| ToolDetectionService | สแกน RE/debug tools ตอน startup + ทุก 1 นาที |
| RuntimeGuard | ตรวจ debugger (Release) |
| AdminEndpointProtector | Host + path obfuscation |
| AdminCertificatePinning | TLS SPKI pin + dual host lock |
| ConfuserEx | Obfuscate release assembly (`build/confuser.crproj`) |
