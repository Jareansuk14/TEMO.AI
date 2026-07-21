# TEMO.AI

WPF desktop app สำหรับสร้างและ deploy Astro site ด้วย AI

## เอกสาร Admin Host

วิธีเปลี่ยน / ป้องกัน backend host (`temo-backend.onrender.com`) ดูที่ [ADMIN_HOST.md](./ADMIN_HOST.md)

## Build Release

```powershell
cd TEMO.AI
.\build-velopack.bat
```

Output: `TEMO.AI/Releases/`

## Development

```powershell
cd TEMO.AI
dotnet run
```

ต้องมี Node.js ติดตั้งในเครื่อง (โปรแกรมจะช่วยติดตั้งอัตโนมัติถ้ายังไม่มี)
