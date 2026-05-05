**MongoDB** là một hệ quản trị cơ sở dữ liệu **NoSQL dạng document**
MongoDB = NoSQL + Document + Flexible Schema + Scale tốt

---

# MongoDB Atlas – Key Features (Trọng tâm)

## 1. Khái niệm

MongoDB Atlas là **dịch vụ MongoDB chạy trên cloud, fully managed**.
 **MongoDB Atlas = MongoDB + Cloud + Auto quản lý + Scale + Bảo mật + Monitoring**

- Không cần cài đặt
    
- Không cần quản lý server
    
- MongoDB lo vận hành cho bạn
---

## 2. Core Features 

### 🔹 1. Managed Database 

- Auto provisioning (tạo cluster nhanh)
    
- Auto patching & updates
    
- Không cần DevOps để maintain DB

---

### 🔹 2. Cluster Management

- Tạo / scale cluster dễ dàng
    
- Chọn cloud provider: AWS, Azure, GCP
    
- Chọn region (gần user để giảm latency)

---

### 🔹 3. Auto Scaling

- Tự động tăng/giảm tài nguyên (CPU, RAM, storage)
    
- Tránh quá tải khi traffic tăng

---

### 🔹 4. High Availability (Replica Set)

- Dữ liệu được replicate nhiều node
    
- Nếu 1 node down → node khác thay thế
    
- Đảm bảo uptime cao

---

### 🔹 5. Backup & Restore

- Backup tự động
    
- Point-in-time recovery
    
- Khôi phục dữ liệu khi có sự cố

---

### 🔹 6. Security

- Authentication (user/password)
    
- IP Whitelist
    
- Encryption (at rest + in transit)
    
- Role-based access control (RBAC)

---

### 🔹 7. Monitoring & Alerts

- Dashboard theo dõi:
    
    - CPU
        
    - Memory
        
    - Query performance
        
- Alert khi có vấn đề (email, webhook)

---

### 🔹 8. Data Distribution (Sharding)

- Tự động phân tán dữ liệu nhiều server
    
- Scale cho hệ thống lớn

---

### 🔹 9. Global Clusters

- Deploy multi-region
    
- User ở đâu → đọc data gần đó (low latency)

---

### 🔹 10. Built-in Tools

Atlas cung cấp thêm tools:

- Data Explorer (UI xem dữ liệu)
    
- Charts (vẽ dashboard)
    
- Search (full-text search)
    
- Data API (gọi DB qua HTTP)
    

---

## 3. Khi nào nên dùng Atlas?

- Không muốn quản lý server
    
- Làm SaaS / production system
    
- Cần scale nhanh
    
- Team nhỏ (ít DevOps)