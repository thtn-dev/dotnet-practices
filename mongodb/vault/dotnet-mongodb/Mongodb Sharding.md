Hiểu đơn giản: **Sharding trong MongoDB = chia nhỏ dữ liệu ra nhiều máy để xử lý**.

---

## 1. Sharding là gì?

**Sharding** là kỹ thuật **horizontal scaling (scale ngang)**  
→ Thay vì nhét tất cả dữ liệu vào **1 server**, ta:

👉 **chia dữ liệu thành nhiều phần nhỏ**  
👉 mỗi phần lưu trên **một server (shard)** khác nhau

💡 Mục tiêu:

- Lưu được **data rất lớn**
    
- Xử lý **nhiều request cùng lúc**
    
- Tránh quá tải 1 máy
    

---

## 2. Ví dụ dễ hiểu

Giả sử bạn có 1 app với **100 triệu user**

❌ Nếu không sharding:

- 1 server phải:
    
    - lưu toàn bộ data
        
    - xử lý toàn bộ query  
        → dễ **CPU full / RAM full / disk bottleneck**
        

✅ Nếu dùng sharding:

- Server A: user id 1 → 30 triệu
    
- Server B: user id 30 → 60 triệu
    
- Server C: user id 60 → 100 triệu
    

👉 mỗi server chỉ xử lý **một phần nhỏ workload**

---

## 3. Kiến trúc Sharding trong MongoDB

Một hệ sharding gồm 3 thành phần chính:

### 1. **Shard**

- Là nơi **lưu dữ liệu thật**
    
- Mỗi shard thường là **replica set** (để đảm bảo HA)
    

---

### 2. **mongos (Query Router)**

- Là **cổng vào**
    
- App chỉ connect tới đây
    
- Nó sẽ:
    
    - nhận query
        
    - quyết định gửi đến shard nào
        

👉 giống như **API Gateway cho database**

---

### 3. **Config Server**

- Lưu:
    
    - metadata (dữ liệu nằm ở shard nào)
        
    - cấu hình cluster
        

---

## 4. Shard Key (Cực kỳ quan trọng)

**Shard key = field dùng để chia dữ liệu**

Ví dụ:

```js
{ userId: 123 }
```

👉 chọn `userId` làm shard key

MongoDB sẽ dùng nó để:

- quyết định document nằm ở shard nào
    
- route query
    

---

### ⚠️ Chọn shard key sai = toang

Ví dụ shard key = `createdAt` (tăng dần):

👉 tất cả data mới → dồn vào 1 shard  
→ **hot shard (bottleneck)**

---

## 5. Cách MongoDB chia dữ liệu

### 1. Range-based (theo khoảng)

```
Shard A: userId 1–1000
Shard B: userId 1001–2000
```

✔ tốt cho query range  
❌ dễ lệch tải

---

### 2. Hashed

```
hash(userId) → phân phối đều
```

✔ data phân bố đều  
❌ query range khó tối ưu

---

## 6. Chunk & Balancer

- Data được chia thành **chunk (block nhỏ)**
    
- MongoDB có **balancer** chạy nền:
    
    - tự động di chuyển chunk giữa các shard
        
    - đảm bảo **cân bằng dữ liệu**
        

---

## 7. Ưu điểm của Sharding

### 🚀 Scale cực tốt

- thêm server = tăng capacity
    

### ⚡ Tăng performance

- nhiều shard → xử lý song song
    

### 💾 Tăng storage

- không bị giới hạn 1 máy
    

---

## 8. Nhược điểm

### ❌ Phức tạp

- setup khó hơn replica set
    

### ❌ Query có thể chậm

Nếu không có shard key:

```
→ query phải chạy ALL shards (scatter-gather)
```

---

### ❌ Khó thiết kế

- phải chọn shard key chuẩn ngay từ đầu
    

---

## 9. Khi nào nên dùng Sharding?

Chỉ nên dùng khi:

- Data **rất lớn** (hàng trăm GB → TB)
    
- Traffic cao
    
- 1 server không chịu nổi
    

👉 Nếu chưa tới mức đó:  
➡️ dùng **Replica Set là đủ**

---

## 10. So sánh nhanh

|Feature|Replica Set|Sharding|
|---|---|---|
|Mục tiêu|High Availability|Scale dữ liệu|
|Data|giống nhau|chia nhỏ|
|Complexity|thấp|cao|

---

## Kết luận

👉 **Sharding = chia dữ liệu + workload ra nhiều máy**

- Giải quyết bài toán:
    
    - Big Data
        
    - High throughput
        
- Nhưng:
    
    - phải thiết kế kỹ (đặc biệt shard key)
        

---
