# 🧠 Lesson 2: MongoDB Architecture Overview – Recap

## 1. Tổng quan kiến trúc MongoDB

MongoDB là **distributed database** → thiết kế để:

- Scale lớn (horizontal scaling)
    
- Đảm bảo high availability
    
- Xử lý lượng data lớn
    

---

## 2. Các thành phần chính

### 🔹 1. mongod (Database Server)

- Process chính chạy database
    
- Lưu trữ data
    
- Xử lý request (query, write)
    

👉 Hiểu đơn giản: **1 instance MongoDB**

---

### 🔹 2. mongos (Router – dùng trong sharding)

- Điều hướng request đến đúng shard
    
- Client chỉ connect vào mongos (không connect trực tiếp shard)
    

---

### 🔹 3. Config Servers

- Lưu metadata của cluster (sharding)
    
- Biết data nằm ở shard nào
    

---

## 3. Replica Set (High Availability)

👉 Replica Set = nhiều mongod chứa cùng data

- **Primary**
    
    - Nhận read/write
        
- **Secondary**
    
    - Copy data từ primary
        
- **Failover**
    
    - Nếu primary chết → secondary lên thay
        

👉 Mục tiêu: **không downtime**

---

## 4. Sharding (Horizontal Scaling)

👉 Dùng khi data quá lớn cho 1 server

- Chia data thành nhiều phần (shards)
    
- Mỗi shard là 1 replica set
    

📌 Thành phần:

- Shard (data node)
    
- mongos (router)
    
- config servers
    

👉 Mục tiêu: **scale + performance**

---

## 5. Data Flow 

Client → mongos → shard phù hợp → trả kết quả

(hoặc nếu không sharding: Client → mongod)

---

## 6. Key Ideas 

- MongoDB là **distributed system**
    
- Có 2 cơ chế chính:
    
    - Replica Set → High Availability
        
    - Sharding → Scalability
        
- Tách rõ:
    
    - Storage (mongod)
        
    - Routing (mongos)
        
    - Metadata (config server)
        

---

## 7. Tóm tắt

👉 **MongoDB architecture = mongod + replica set + sharding (mongos + config servers)**

---
