# 📘 MongoDB and the Document Model

## Lesson 1: Overview of the Document Model

---

## 1. Document Model là gì?

MongoDB lưu dữ liệu dưới dạng **document (tài liệu)** thay vì bảng như SQL.

👉 Một **document** là:

- dạng **JSON-like (BSON)**
    
- chứa **key-value**
    

Ví dụ:

```json
{
  "title": "Inception",
  "year": 2010,
  "genres": ["Action", "Sci-Fi"],
  "director": {
    "name": "Christopher Nolan"
  }
}
```

---

## 2. Khác gì so với SQL?

|SQL (Relational)|MongoDB (Document Model)|
|---|---|
|Table|Collection|
|Row|Document|
|Column|Field|
|JOIN|Embed / Reference|

👉 MongoDB **không cần JOIN nhiều như SQL**

---

## 3. Đặc điểm chính của Document Model

### 🔹 1. Flexible Schema (Schema linh hoạt)

Không cần định nghĩa schema trước:

```json
{ "name": "A", "age": 20 }
{ "name": "B", "email": "b@gmail.com" }
```

👉 cùng collection nhưng khác structure vẫn OK

---

### 🔹 2. Nested Data (Dữ liệu lồng nhau)

Có thể nhúng object/array:

```json
{
  "orderId": 1,
  "items": [
    { "product": "A", "qty": 2 },
    { "product": "B", "qty": 1 }
  ]
}
```

👉 thay cho JOIN

---

### 🔹 3. Data gần nhau (Locality)

Dữ liệu liên quan nằm chung 1 document

👉 Query:

- nhanh hơn (ít phải join)
    
- ít round-trip
    

---

### 🔹 4. Polymorphic Data

Document cùng collection có thể khác cấu trúc:

```json
{ "type": "user", "name": "Nam" }
{ "type": "admin", "permissions": ["read", "write"] }
```

---

## 4. Ưu điểm của Document Model

### ✅ Performance tốt

- đọc 1 document là đủ (không join)
    

### ✅ Linh hoạt

- dễ thay đổi structure
    

### ✅ Mapping tự nhiên với object code

- rất hợp với OOP (C#, Java, JS)
    

---

## 5. Khi nào Document Model mạnh nhất?

👉 Khi dữ liệu:

- có quan hệ **1-nested (1-n, 1-1)**
    
- thường được đọc **cùng nhau**
    

Ví dụ:

- user + profile
    
- order + items
    
- blog + comments
    

---

## 6. Trade-off (điểm đánh đổi)

### ❌ Duplicated data (có thể lặp dữ liệu)

- vì embed thay vì reference
    

### ❌ Khó update đồng bộ

- nếu data bị duplicate
    

---

## 7. Tư duy quan trọng

👉 SQL:

> Normalize dữ liệu (tránh lặp)

👉 MongoDB:

> Denormalize (ưu tiên performance đọc)

---

## 8. Tóm tắt 1 dòng (để bạn ghi note)

👉 **Document Model = lưu dữ liệu dạng JSON linh hoạt, cho phép embed dữ liệu liên quan để tối ưu đọc và giảm JOIN**

---
Dưới đây là **recap trọng tâm – dễ nhớ** cho:

# 📘 Lesson 2: Data Types in MongoDB

---

## 1. MongoDB dùng kiểu dữ liệu gì?

MongoDB lưu dữ liệu dưới dạng **BSON (Binary JSON)**  
👉 mở rộng từ JSON → hỗ trợ nhiều kiểu hơn

---

## 2. Các nhóm kiểu dữ liệu chính

### 🔹 1. String

```json
{ "name": "Nam" }
```

- Chuỗi ký tự
    
- giống JSON
    

---

### 🔹 2. Number

MongoDB có nhiều loại số:

|Type|Mô tả|
|---|---|
|int32|số nguyên nhỏ|
|int64|số nguyên lớn|
|double|số thực|
|decimal128|số chính xác cao (tiền tệ)|

👉 Ví dụ:

```json
{ "age": 25, "price": 19.99 }
```

💡 Tip:

- tiền → dùng `decimal128` (tránh sai số)
    

---

### 🔹 3. Boolean

```json
{ "isActive": true }
```

---

### 🔹 4. Array

```json
{ "tags": ["mongodb", "database"] }
```

👉 rất quan trọng trong MongoDB  
→ dùng cho:

- list
    
- many-to-one
    

---

### 🔹 5. Object (Embedded Document)

```json
{
  "address": {
    "city": "HCM",
    "zip": 700000
  }
}
```

👉 dùng để **embed dữ liệu**

---

### 🔹 6. Null

```json
{ "middleName": null }
```

---

## 3. Các kiểu đặc biệt (MongoDB-specific)

### 🔸 1. ObjectId

```json
{ "_id": ObjectId("...") }
```

👉 default primary key

Đặc điểm:

- unique
    
- có timestamp bên trong
    

---

### 🔸 2. Date

```json
{ "createdAt": ISODate("2026-01-01") }
```

👉 lưu thời gian

---

### 🔸 3. Timestamp

- dùng nội bộ (replication, oplog)
    

---

### 🔸 4. Binary Data

- lưu file, image, v.v.
    

---

### 🔸 5. Decimal128

👉 số chính xác cao (quan trọng):

```json
{ "amount": NumberDecimal("99.99") }
```

---

## 4. So sánh nhanh với JSON

|Feature|JSON|BSON|
|---|---|---|
|String|✅|✅|
|Number|1 loại|nhiều loại|
|Date|❌|✅|
|ObjectId|❌|✅|
|Binary|❌|✅|

---

## 5. Tại sao BSON quan trọng?

👉 Vì:

- encode nhanh hơn JSON
    
- hỗ trợ nhiều kiểu hơn
    
- tối ưu cho database
    

---

## 6. Lưu ý quan trọng (thực tế dev)

### ⚠️ 1. Type consistency

```json
{ "age": 25 }     // int
{ "age": "25" }   // string ❌
```

👉 sẽ gây lỗi query

---

### ⚠️ 2. Chọn đúng kiểu số

- tiền → `decimal128`
    
- count → `int`
    

---

### ⚠️ 3. Array query rất mạnh

```json
{ "tags": "mongodb" }
```

👉 tự match trong array

---

## 7. Tóm tắt 

👉 **MongoDB dùng BSON (JSON mở rộng), hỗ trợ nhiều kiểu dữ liệu như ObjectId, Date, Array, giúp lưu trữ linh hoạt và mạnh hơn JSON**


---

# 📘 Data Relationships trong MongoDB

MongoDB vẫn có các kiểu quan hệ giống SQL:

|Quan hệ|Ví dụ|
|---|---|
|1–1|user – profile|
|1–n|order – items|
|n–n|students – courses|

👉 Nhưng cách biểu diễn **khác SQL**  
→ không dùng JOIN, mà dùng:

## 👉 2 cách chính:

- **Embedding (nhúng)**
    
- **Referencing (tham chiếu)**
    

---

# 🔹 1. Embedding (Nhúng)

👉 Lưu dữ liệu liên quan **trong cùng 1 document**

### Ví dụ:

```json
{
  "orderId": 1,
  "customer": "Nam",
  "items": [
    { "product": "A", "qty": 2 },
    { "product": "B", "qty": 1 }
  ]
}
```

---

## ✅ Ưu điểm

### 🚀 1. Query cực nhanh

- chỉ đọc **1 document**
    
- không cần JOIN
    

---

### 🚀 2. Data locality

- dữ liệu liên quan nằm gần nhau
    

---

### 🚀 3. Ít query hơn

- giảm round-trip DB
    

---

## ❌ Nhược điểm

### ⚠️ 1. Data duplication

- có thể bị lặp dữ liệu
    

---

### ⚠️ 2. Document quá lớn

- MongoDB giới hạn **16MB / document** / 100 nested
    

---

### ⚠️ 3. Update khó nếu data lặp

---

# 🔹 2. Referencing (Tham chiếu)

👉 Lưu **id của document khác**

### Ví dụ:

```json
{
  "orderId": 1,
  "customerId": 123
}
```

```json
{
  "_id": 123,
  "name": "Nam"
}
```

---

## ✅ Ưu điểm

### 🚀 1. Tránh duplicate

- dữ liệu nằm 1 nơi
    

---

### 🚀 2. Phù hợp data lớn

- không bị giới hạn document size
    

---

### 🚀 3. Quan hệ phức tạp

- n–n, many collections
    

---

## ❌ Nhược điểm

### ⚠️ 1. Phải query nhiều lần

- hoặc dùng `$lookup` (giống JOIN)
    

---

### ⚠️ 2. Chậm hơn embedding

---

# 🔥 3. Khi nào dùng Embedding vs Referencing?

## 👉 Dùng Embedding khi:

- dữ liệu:
    
    - **luôn đọc cùng nhau**
        
    - quan hệ **1–n nhỏ**
        
- không quá lớn
    

📌 Ví dụ:

- order → items
    
- blog → comments (ít)
    
- user → profile
    

---

## 👉 Dùng Referencing khi:

- dữ liệu:
    
    - **lớn / tăng không giới hạn**
        
    - cần update độc lập
        
- quan hệ:
    
    - n–n
        
    - shared data
        

📌 Ví dụ:

- users ↔ roles
    
- products ↔ categories
    
- posts ↔ tags
    

---

# ⚖️ 4. So sánh nhanh

|Tiêu chí|Embedding|Referencing|
|---|---|---|
|Performance|🔥 nhanh|chậm hơn|
|Complexity|đơn giản|phức tạp hơn|
|Data size|nhỏ–trung bình|lớn|
|Update|khó nếu duplicate|dễ|

---

# 🧠 5. Tư duy quan trọng 
👉 MongoDB ưu tiên:

> **READ performance > WRITE normalization**

---

👉 Nên câu hỏi quan trọng là:

❌ “Quan hệ thế nào?”  
✅ “Data được đọc như thế nào?”

---

# 💡 6. Rule nhớ nhanh (rất đáng giá)

👉 Nếu bạn phải JOIN thường xuyên → bạn đang design sai

👉 Nếu dữ liệu luôn đi cùng nhau → **embed**

👉 Nếu dữ liệu độc lập → **reference**

---

# 🧾 7. Tóm tắt

👉 **Embedding = nhúng dữ liệu để đọc nhanh**  
👉 **Referencing = tách dữ liệu để linh hoạt và tránh trùng lặp**

---
