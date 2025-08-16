# ChatOverlay

**ChatOverlay** เป็นแอป WPF สำหรับแสดงข้อความจาก **YouTube Live Chat** แบบ overlay บนหน้าจอ  
สามารถใช้ได้กับการสตรีม โดยไม่บังการทำงานของแอปอื่น และยังรองรับการแสดงผล Super Chat, Super Sticker และ emoji

---

## ✨ ฟีเจอร์

- ดึงข้อความสดจาก YouTube Live ผ่าน **YouTube Data API v3**  
- แสดงผล **Super Chat / Super Sticker** พร้อมสีตาม tier  
- แสดง **avatar** ของผู้ส่ง และ highlight พิเศษสำหรับ **moderator, sponsor, owner**  
- รองรับ emoji และ custom emoji  
- Overlay แบบ **transparent** (กดผ่านได้ ไม่รบกวนหน้าต่างอื่น)  
- ข้อความใหม่จะถูกแสดงด้านล่าง ข้อความเก่าจะเลื่อนขึ้น  
- Overlay จะ **ล่องหนอัตโนมัติ** หากไม่มีข้อความใหม่ภายในเวลาที่กำหนด  
- ใช้ **Velopack** จัดการ assets (เช่น emoji, icons, config)

---

## 📦 การติดตั้ง

1. **Clone โปรเจกต์**

```bash
git clone https://github.com/TineTheUnc/ChatOverlay.git
cd ChatOverlay
```

2. **ติดตั้ง dependencies**

- .NET 7.0 (หรือใหม่กว่า ที่รองรับ WPF)
- Google.Apis.YouTube.v3
- Grpc.Net.Client
- Velopack

3. **สร้างไฟล์ `client_secret.json`** จาก Google Cloud Console  
   - ดาวน์โหลด OAuth2 client ID  
   - วางไว้ในโฟลเดอร์โปรเจกต์ หรือใช้ปุ่ม **Import client_secret** ในโปรแกรม  

---

## ▶️ วิธีใช้งาน

1. เปิดโปรแกรม `ChatOverlay.exe`  
2. กด **Import client_secret** เพื่อโหลดไฟล์ `client_secret.json`  
3. กด **Authorization** เพื่อเชื่อมบัญชี YouTube  
4. กรอก **Live ID** ของสตรีมในช่อง input  
5. กด **Start** เพื่อเริ่มดึงข้อความแชท  
6. หน้าต่าง Overlay จะปรากฏบนจอโดยอัตโนมัติ  

> ข้อความเก่าจะเลื่อนขึ้น ข้อความใหม่จะอยู่ด้านล่าง  
> หากไม่มีข้อความใหม่ในเวลาที่กำหนด Overlay จะค่อย ๆ หายไป  

---

## 📂 โครงสร้างไฟล์

```
ChatOverlay/
├─ ChatOverlay.csproj
├─ MainWindow.xaml        # UI หลัก
├─ MainWindow.xaml.cs     # โค้ดหลัก
├─ Chat.xaml              # Overlay UI
├─ Chat.xaml.cs           # โค้ดแสดง overlay
├─ Assets/                # เก็บ emoji, icon, asset อื่น ๆ
├─ App.xaml
├─ App.xaml.cs
```

---

## ⚙️ การปรับแต่ง

- **ขนาด Overlay** → ปรับ `MaxWidth` ใน `Chat.xaml.cs`  
- **เวลา Overlay ล่องหน** → ปรับค่าที่ `AutoCloseWindow(milliseconds)`  
- **สี Super Chat / Super Sticker** → แก้ไขได้ใน `Chat.xaml.cs`  

---

## 📜 License

Distributed under the **MIT License**.  
ดูรายละเอียดได้ในไฟล์ [LICENSE](LICENSE)
