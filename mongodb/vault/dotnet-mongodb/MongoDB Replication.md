**MongoDB Replication** (sao chép dữ liệu) là cơ chế giúp **nhiều server cùng giữ một bản copy giống nhau của database**, nhằm đảm bảo:

- Không mất dữ liệu khi server bị chết
    
- Hệ thống luôn hoạt động (high availability)
    
- Có thể scale đọc (read scaling)
    

---

# 🧠 Hiểu đơn giản

Bạn có 1 database chính:

```
Server A (Primary)
```

MongoDB sẽ tạo thêm các bản sao:

```
Server A (Primary)  ---> Server B (Secondary)
                     ---> Server C (Secondary)
```

👉 Tất cả đều có **cùng dữ liệu**

---

# ⚙️ Thành phần chính: Replica Set

Replication trong MongoDB được triển khai qua **Replica Set**

Một replica set gồm:

### 1. Primary

- Chỉ có **1 node duy nhất**
    
- Nhận **toàn bộ write (insert/update/delete)**
    
- Ghi log vào **oplog**
    

---

### 2. Secondary

- Có thể có nhiều node
    
- **Copy dữ liệu từ primary**
    
- Không nhận write (mặc định)
    
- Có thể dùng để:
    
    - Read (nếu cấu hình)
        
    - Backup
        
    - Analytics
        

---

### 3. Arbiter (optional)

- **Không lưu dữ liệu**
    
- Chỉ dùng để **bỏ phiếu khi election**
    
- Dùng khi muốn tiết kiệm chi phí
    

---

# 🔁 Cách hoạt động (Flow)

### Bước 1: Write

Client → Primary

```
Client → Primary → ghi data + ghi oplog
```

---

### Bước 2: Replication

Secondary sẽ:

```
Pull oplog từ Primary → replay lại
```

👉 Đây gọi là **asynchronous replication** (không đồng bộ)

---

# ⚠️ Replication Lag là gì?

- Là độ trễ giữa:
    
    - Primary ghi data
        
    - Secondary cập nhật
        

👉 Lag lớn → dữ liệu secondary bị “cũ”

---

# 🔄 Automatic Failover (Điểm cực quan trọng)

Nếu Primary chết:

1. Secondary sẽ **bầu cử (election)**
    
2. Một node trở thành **Primary mới**
    

⏱ Thường mất ~10–12 giây

---

### Ví dụ:

```
Primary (A) ❌ chết

→ B và C vote

→ B trở thành Primary mới
```

👉 App vẫn chạy bình thường (nếu driver handle tốt)

---

# 📖 Read & Write behavior

### Write:

- Luôn vào **Primary**
    

---

### Read:

- Mặc định: từ Primary
    
- Có thể config:
    
    - Read từ Secondary → tăng performance
        

⚠️ Nhưng:

- Secondary có thể **chưa sync kịp → dữ liệu stale**
    

---

# 🔒 Write Concern (quan trọng trong production)

Ví dụ:

```
{ w: "majority" }
```

👉 Nghĩa là:

- Write chỉ thành công khi **đa số node đã nhận data**
    

→ tăng độ an toàn (tránh mất data khi failover)

---

# 🎯 Lợi ích của Replication

### ✅ 1. High Availability

- Server chết → hệ thống vẫn chạy
    

### ✅ 2. Data Redundancy

- Có nhiều bản copy dữ liệu
    

### ✅ 3. Disaster Recovery

- Backup tự nhiên
    

### ✅ 4. Scale Read

- Read từ nhiều node
    

---

# ⚠️ Nhược điểm

### ❌ 1. Eventual Consistency

- Secondary có thể không sync ngay
    

### ❌ 2. Tốn tài nguyên

- Nhiều server hơn
    

### ❌ 3. Complexity

- Phải quản lý election, lag, failover
    

---

# 🧩 Tổng kết

**MongoDB Replication = cơ chế sao chép dữ liệu giữa nhiều server thông qua Replica Set**

- 1 Primary → xử lý write
    
- N Secondary → copy dữ liệu
    
- Có cơ chế election → tự động failover
    
- Replication là **nền tảng bắt buộc cho production**
    
