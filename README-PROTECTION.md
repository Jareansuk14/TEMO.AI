# TEMO.AI — คู่มือ Vault, การเข้ารหัส และการดูแลรักษา

เอกสารนี้สำหรับ **TEMO.AI** — vault แบบ **slim** (7 slots) ใช้ algorithm เดียวกับ LCA/LineAPIBot แต่เก็บแค่ auth + update + client header

> หลักการ XOR / UrlCipher / vault-tool อธิบายละเอียด: [LCA/README-PROTECTION.md §2](../LCA/README-PROTECTION.md#2-ระบบเข้ารหัสทำงานอย่างไร)

---

## สารบัญ

1. [ภาพรวม](#1-ภาพรวม)
2. [ระบบเข้ารหัส (สรุป)](#2-ระบบเข้ารหัส-สรุป)
3. [ไฟล์สำคัญ](#3-ไฟล์สำคัญ)
4. [ตาราง Vk — ค่าจริงทุก slot](#4-ตาราง-vk--ค่าจริงทุก-slot)
5. [คู่มือปฏิบัติการ](#5-คู่มือปฏิบัติการ)
6. [JSON DTO และ Obfuscar](#6-json-dto-และ-obfuscar)
7. [Build Release + Obfuscar](#7-build-release--obfuscar)
8. [ToolDetection](#8-tooldetection)
9. [สิ่งที่ยังไม่ได้ใส่ vault](#9-สิ่งที่ยังไม่ได้ใส่-vault)
10. [แก้ปัญหา / FAQ](#10-แก้ปัญหา--faq)
11. [Checklist ทดสอบ](#11-checklist-ทดสอบ)

---

## 1. ภาพรวม

TEMO.AI เป็นแอปสร้างเว็บด้วย AI — **ไม่มี** LINE API, OBS, captcha, phone-data  
Protection เน้น:

- เข้ารหัส URL backend + GitHub update
- Login-first + token persist
- ToolDetection + Obfuscar

```
AuthApiService / AppUpdateService
    │
    └─► VaultGate.Get(Vk.Vn) → VaultCore.Blobs[n] → UrlCipher.Decode
```

---

## 2. ระบบเข้ารหัส (สรุป)

| หัวข้อ | ค่า |
|--------|-----|
| Algorithm | XOR repeating key บน UTF-8 |
| Tool key | `Lca.Url.Vault.2026` (ใน `tools/vault-tool.py`) |
| Runtime | `Services/UrlCipher.cs` (masked key — เหมือน LCA) |
| Slots | `Vk.V0` … `Vk.V6` (7 ช่อง) |

```bash
cd TEMO.AI

# อ่าน
python tools/vault-tool.py decode "0x24, 0x17, ..."

# เขียน
python tools/vault-tool.py encode "https://temo-backend.onrender.com/api/auth/login" --index 2
```

**สูตร:** `plain[i] = blob[i] XOR key[i % 18]`

---

## 3. ไฟล์สำคัญ

| ไฟล์ | หน้าที่ |
|------|---------|
| `Services/Vk.cs` | enum `V0`–`V6` |
| `Services/VaultCore.cs` | `byte[][] Blobs` (7 entries) |
| `Services/VaultGate.cs` | `Get(Vk)` |
| `Services/UrlCipher.cs` | XOR decode |
| `Services/AuthApiService.cs` | login + validate (V2, V3, V5, V6) |
| `Services/AppUpdateService.cs` | Velopack (V4) |
| `Services/TokenManager.cs` | เก็บ token `%AppData%\TEMO.AI\token.json` |
| `Services/AuthSession.cs` | restore session ตอน startup |
| `Models/AuthDtos.cs` | login DTO |
| `tools/vault-tool.py` | encode/decode |
| `Obfuscar.xml` / `build-velopack.bat` | release |

---

## 4. ตาราง Vk — ค่าจริงทุก slot

> decode จาก `VaultCore.cs` ปัจจุบัน

| Vk | ค่าปัจจุบัน | Consumer |
|----|-------------|----------|
| `V0` | `https://temo-backend.onrender.com` | อ้างอิง base |
| `V1` | `https://temo-backend.onrender.com/api` | สำรอง / อ้างอิง |
| `V2` | `…/api/auth/login` | `AuthApiService.LoginAsync` |
| `V3` | `…/api/auth/profile` | `AuthApiService.ValidateTokenAsync` |
| `V4` | `https://github.com/Jareansuk14/TEMO.AI` | `AppUpdateService` (Velopack) |
| `V5` | `X-Client-Type` | header name — `AuthApiService` |
| `V6` | `TEMO.AI` | header value — `AuthApiService` |

**Flow auth:**

```
Startup → AuthSession.TryRestoreSessionAsync()
       → ValidateTokenAsync()  GET V3 + Bearer token
       → ถ้า OK → MainWindow | ไม่ OK → LoginWindow

Login → POST V2 { username, password, hwid }
     → TokenManager.SetToken() → MainWindow
```

---

## 5. คู่มือปฏิบัติการ

### 5.1 อ่านค่า (Decode)

```bash
cd TEMO.AI
python tools/vault-tool.py decode "0x24, 0x17, 0x15, 0x5E, ..."
```

หา consumer:

```bash
rg "Vk\.V" Services/
```

### 5.2 แก้ไขค่าเดิม (Edit)

1. decode slot ที่จะแก้
2. encode ค่าใหม่
3. แทนใน `VaultCore.cs` → `Blobs[n]`
4. `dotnet build TEMO.AI.csproj -c Release`
5. release: `build-velopack.bat`

**เปลี่ยน backend ทั้งชุด:**

```bash
python tools/vault-tool.py encode "https://NEW.onrender.com" --index 0
python tools/vault-tool.py encode "https://NEW.onrender.com/api" --index 1
python tools/vault-tool.py encode "https://NEW.onrender.com/api/auth/login" --index 2
python tools/vault-tool.py encode "https://NEW.onrender.com/api/auth/profile" --index 3
```

**เปลี่ยน GitHub update repo (V4):**

```bash
python tools/vault-tool.py encode "https://github.com/Jareansuk14/TEMO.AI" --index 4
```

> `build-velopack.bat` มี `REPO_URL=` แยก — ต้อง sync กับ V4 ด้วยมือ

**เปลี่ยน client header (V6):**

```bash
python tools/vault-tool.py encode "TEMO.AI" --index 6
```

### 5.3 เพิ่มค่าใหม่ (Add)

TEMO.AI ปัจจุบันมี 7 slots — ถ้าต้องเพิ่ม (เช่น API ใหม่):

1. เพิ่ม `V7` ใน `Vk.cs`
2. encode → append `Blobs[7]`
3. ใช้ `VaultGate.Get(Vk.V7)` ใน service
4. อัปเดต README §4

> ถ้า slot เยอะขึ้นเรื่อยๆ พิจารณา sync โครงสร้างกับ LCA (36 slots)

### 5.4 ลบค่า (Remove)

- อย่าลบ slot กลาง — index จะ shift
- deprecate: ลบ consumer + เก็บ blob
- TEMO.AI มี slot น้อย — ลบถาวรทำได้ถ้า renumber `Vk` + `Blobs` + grep ครบ

### 5.5 ย้าย plain string → vault

```csharp
// ❌
await Http.PostAsJsonAsync("https://temo-backend.../api/auth/login", payload);

// ✅
await Http.PostAsJsonAsync(VaultGate.Get(Vk.V2), payload);
```

---

## 6. JSON DTO และ Obfuscar

| ไฟล์ | ใช้เมื่อ |
|------|---------|
| `Models/AuthDtos.cs` | `AuthLoginRequestDto`, `AuthErrorResponseDto` |

**ห้าม** anonymous type กับ request/response ที่ serialize ไป server

Namespace `TEMO.AI.Models` ถูก skip ใน `Obfuscar.xml`

---

## 7. Build Release + Obfuscar

```bash
dotnet build TEMO.AI.csproj -c Release
build-velopack.bat
```

| ตั้งค่า | ค่า |
|--------|-----|
| Target | `net9.0-windows` |
| GitHub (script) | `REPO_URL=https://github.com/Jareansuk14/TEMO.AI` |
| Vault (V4) | ต้องตรงกับ repo ที่ Velopack ใช้ |

**Skip:** `TEMO.AI.Models`, `TEMO.AI.Views`, `TEMO.AI.Ai`, App, MainWindow, LoginWindow  
**Obfuscate:** `VaultCore`, `UrlCipher`

---

## 8. ToolDetection

- `Program.Main` + `App.OnStartup` + timer 60s
- เจอ RE tool → "Error 404" → shutdown + cleanup `%Temp%\.net\TEMO.AI*`

---

## 9. สิ่งที่ยังไม่ได้ใส่ vault

| รายการ | ไฟล์ |
|--------|------|
| Template zip URL | `Services/TemplateStore.cs` |
| GitHub URL ใน build script | `build-velopack.bat` |
| OpenAI API key | settings ในแอป (user-provided) |

---

## 10. แก้ปัญหา / FAQ

| อาการ | แก้ |
|-------|-----|
| Login ไม่ได้ | decode V2, ตรวจ backend + HWID |
| เปิดแอปแล้ว login ใหม่ทุกครั้ง | V3 validate fail → ตรวจ endpoint `/api/auth/profile` |
| Update ไม่ work | sync V4 กับ `REPO_URL` ใน bat |
| Decode garbage | copy hex ไม่ครบ / key ไม่ sync |
| แอปไม่เปิด | ปิด dnSpy/ILSpy |

| ถาม | ตอบ |
|-----|-----|
| Login URL? | `AuthApiService` → V2 |
| Validate? | `AuthApiService` → V3 |
| Update repo? | `AppUpdateService` → V4 |
| Client header? | V5 (name) + V6 (value) |

---

## 11. Checklist ทดสอบ

- [ ] Login สำเร็จ
- [ ] ปิดเปิดแอป → restore session (ไม่ต้อง login ใหม่)
- [ ] Token หมดอายุ → กลับ LoginWindow
- [ ] Velopack update (V4 = GitHub repo)
- [ ] ไม่มี RE tool บนเครื่อง
- [ ] **หลัง `build-velopack.bat`**

---

## เปรียบเทียบกับ LCA / LineAPIBot

| | LCA / LineAPIBot | TEMO.AI |
|--|------------------|---------|
| จำนวน slot | 36 (V0–V35) | 7 (V0–V6) |
| LINE / OBS / captcha | ✅ | ❌ |
| Heartbeat / phone data | ✅ | ❌ |
| Login + token | ✅ | ✅ |
| ToolDetection | ✅ | ✅ |
| Algorithm | XOR + UrlCipher | เหมือนกัน |
