# 📘 Connecting to a MongoDB Database Using the MongoDB Shell (`mongosh`)

---

## 1. `mongosh` là gì?

👉 `mongosh` (MongoDB Shell) là **command-line tool** để:

- kết nối database
    
- chạy query
    
- quản lý MongoDB
    

---

## 2. Kết nối cơ bản (Local)

Nếu MongoDB chạy local:

```bash
mongosh
```

👉 mặc định:

- host: `localhost`
    
- port: `27017`
    

---

## 3. Kết nối bằng connection string

```bash
mongosh "mongodb://localhost:27017"
```

---

## 4. Kết nối có username/password

```bash
mongosh "mongodb://username:password@localhost:27017"
```

---

## 5. Kết nối MongoDB Atlas (cloud)

Ví dụ:

```bash
mongosh "mongodb+srv://user:pass@cluster0.xxxx.mongodb.net/"
```

---

## 6. Sau khi connect – làm gì?

### 🔹 Xem database

```js
show dbs
```

---

### 🔹 Chọn database

```js
use mydb
```

---

### 🔹 Xem collection

```js
show collections
```

---

### 🔹 Query thử

```js
db.users.find()
```

---

## 7. Cách mongosh hoạt động

👉 Flow:

```
App / mongosh
      ↓
   mongos (nếu sharding)
      ↓
 MongoDB server
```

---

## 8. Kết nối trong Sharded Cluster

⚠️ Quan trọng:

👉 **KHÔNG connect trực tiếp vào shard**

👉 luôn connect qua:

```bash
mongos
```

---

## 9. Một số option hay dùng

### 🔹 Chọn database ngay khi connect

```bash
mongosh "mongodb://localhost:27017/mydb"
```

---

### 🔹 SSL / TLS (Atlas thường dùng)

```bash
mongosh "mongodb+srv://..."
```
**`+srv` trong `mongodb+srv://` là cách kết nối MongoDB thông qua DNS SRV record**  
→ giúp **không cần ghi rõ host/port từng node**

---

### 🔹 File script

```bash
mongosh script.js
```

---

## 10. Lỗi thường gặp

### ❌ Connection refused

- MongoDB chưa chạy
    

---

### ❌ Authentication failed

- sai user/pass
    
- sai auth database
    

---

### ❌ Network timeout

- sai IP whitelist (Atlas)
    

---

## 11. Tóm tắt 

👉 **`mongosh` là tool CLI để connect và thao tác MongoDB bằng connection string (local hoặc Atlas)**

---

## 12. Tip thực tế (dev)

- Dev local → dùng:
    

```bash
mongosh
```

- Production / Atlas → copy connection string
    
- Luôn test:
    

```js
db.runCommand({ ping: 1 })
```

---

👉 **`+srv` trong `mongodb+srv://` là cách kết nối MongoDB thông qua DNS SRV record**  
→ giúp bạn **không cần ghi rõ host/port từng node**

# 🔍 1. So sánh nhanh

### ❌ Cách cũ (không `+srv`)

```bash
mongodb://host1:27017,host2:27017,host3:27017/mydb
```

👉 phải tự liệt kê tất cả server

---

### ✅ Cách mới (`+srv`)

```bash
mongodb+srv://cluster0.xxxx.mongodb.net/mydb
```

👉 chỉ cần **1 domain**

---

# ⚙️ 2. `+srv` hoạt động như thế nào?

```bash
mongodb+srv://cluster0.mongodb.net
```

👉 MongoDB driver sẽ:

1. Query DNS (SRV record)
    
2. Lấy danh sách:
    
    - các node (replica set / cluster)
        
    - port
        
3. Tự động connect đúng cluster
    

---

# 💡 3. Lợi ích của `+srv`

### ✅ 1. Gọn hơn

- không cần list nhiều host
    

---

### ✅ 2. Tự động cấu hình

- replica set
    
- TLS/SSL
    
- options
    

---

### ✅ 3. Dễ scale

- thêm node → không cần sửa connection string
    

---

# 📦 4. Thường dùng ở đâu?

👉 Chủ yếu trong:

- MongoDB Atlas (cloud)
    

Ví dụ thực tế:

```bash
mongodb+srv://user:pass@cluster0.abcd.mongodb.net/
```

---

# ⚠️ 5. Lưu ý

### ❗ 1. Cần DNS support

- môi trường phải resolve được SRV
    

---

### ❗ 2. Không custom port

- SRV tự quyết định port
    

---

### ❗ 3. Driver phải hỗ trợ

- MongoDB driver mới đều support
    

---

# 🧠 6. Hiểu bản chất (rất dễ nhớ)

👉

- `mongodb://` → bạn tự chỉ server
    
- `mongodb+srv://` → **DNS chỉ server giúp bạn**
    

---

# 🧾 7. Tóm tắt

👉 **`+srv` = dùng DNS để tự động tìm và connect đến toàn bộ MongoDB cluster**
