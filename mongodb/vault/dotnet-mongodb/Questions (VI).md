# 📘 MongoDB Associate Developer – Câu Hỏi Ôn Luyện

> Tài liệu ôn thi dựa trên **MongoDB Associate Developer Exam Guide** 53 câu hỏi | 75 phút | $150 USD | Không được mang tài liệu tham khảo

---

## 📌 Thông Tin Kỳ Thi

|Thông tin|Chi tiết|
|---|---|
|Số câu hỏi|53 câu|
|Loại câu hỏi|Multiple Choice & Multiple Response|
|Thời gian|75 phút|
|Lệ phí|$150 USD|
|Hình thức|Online – có giám thị (Proctored)|
|Điều kiện|Không yêu cầu|

---

## Section 1: MONGODB OVERVIEW AND THE DOCUMENT MODEL (8%)

### 1.1 – Các kiểu dữ liệu BSON mà MongoDB hỗ trợ

**Q1.** MongoDB BSON hỗ trợ những kiểu dữ liệu nào sau đây? _(Chọn tất cả đáp án đúng)_

- [x] A. String
- [x] B. Integer (32-bit & 64-bit)
- [x] C. Double
- [x] D. Boolean
- [x] E. Date
- [x] F. ObjectId
- [x] G. Array
- [x] H. Embedded Document (Object)
- [x] I. Null
- [x] J. Binary Data
- [x] K. Regular Expression
- [x] L. Decimal128

<details> <summary>💡 Đáp án</summary>

**Tất cả các đáp án trên đều đúng (A–L).**

BSON (Binary JSON) là định dạng nhị phân MongoDB sử dụng để lưu trữ document. Các kiểu dữ liệu BSON phổ biến bao gồm: String, Int32, Int64, Double, Decimal128, Boolean, Date, ObjectId, Array, Embedded Document, Null, Binary Data, Regular Expression, Timestamp.

</details>

---

**Q2.** Kiểu dữ liệu nào được MongoDB tự động gán cho trường `_id` nếu không được chỉ định?

- [ ] A. String
- [ ] B. Integer
- [x] C. ObjectId
- [ ] D. UUID

<details> <summary>💡 Đáp án</summary>

**C. ObjectId**

Nếu không chỉ định `_id`, MongoDB sẽ tự động tạo một giá trị `ObjectId` – một chuỗi 12-byte bao gồm timestamp, machine id, process id, và một số ngẫu nhiên.

</details>

---

### 1.2 – Documents có hình dạng khác nhau trong cùng một Collection

**Q3.** Ba document sau có thể cùng tồn tại trong một collection MongoDB không?

```json
{ "_id": 1, "name": "Alice", "age": 30 }
{ "_id": 2, "name": "Bob", "email": "bob@example.com" }
{ "_id": 3, "product": "Laptop", "price": 999.99 }
```

- [ ] A. Không, vì các document phải có cùng schema
- [x] B. Có, vì MongoDB là schema-flexible (không yêu cầu schema cố định)
- [ ] C. Không, vì document thứ 3 thiếu trường `name`
- [ ] D. Có, nhưng cần phải khai báo schema trước

<details> <summary>💡 Đáp án</summary>

**B. Có, vì MongoDB là schema-flexible**

MongoDB không yêu cầu các document trong cùng một collection phải có cùng cấu trúc (schema). Đây là một trong những ưu điểm lớn của document model.

</details>

---

**Q4.** Document nào sau đây KHÔNG thể tồn tại cùng collection với document `{ "_id": 1, "name": "Alice" }`?

- [ ] A. `{ "_id": 2, "score": 100 }`
- [ ] B. `{ "_id": 1, "email": "test@example.com" }`
- [ ] C. `{ "_id": 3, "tags": ["mongodb", "database"] }`
- [ ] D. `{ "_id": 4 }`

<details> <summary>💡 Đáp án</summary>

**B. `{ "_id": 1, "email": "test@example.com" }`**

`_id` phải là duy nhất trong một collection. Document B có `_id: 1` trùng với document đã tồn tại, nên sẽ gây lỗi duplicate key.

</details>

---

## Section 2: CRUD (51%)

### 2.1 – Insert Commands

**Q5.** Lệnh nào sau đây là lệnh insert hợp lệ?

- [ ] A. `db.users.insertOne({ name: "Alice", age: 25 })`
- [ ] B. `db.users.insert({ name: "Alice", age: 25 })`
- [ ] C. `db.users.insertMany([{ name: "Alice" }, { name: "Bob" }])`
- [ ] D. `db.users.add({ name: "Alice" })`

<details> <summary>💡 Đáp án</summary>

**A và C** là đúng.

- `insertOne()` – chèn một document
- `insertMany()` – chèn nhiều document
- `insert()` – deprecated (không khuyến khích dùng)
- `add()` – không tồn tại trong MongoDB

</details>

---

**Q6.** Khi sử dụng `insertMany()`, nếu một document trong mảng gặp lỗi (ví dụ: trùng `_id`), điều gì xảy ra theo mặc định?

- [ ] A. Tất cả document đều bị rollback
- [ ] B. Các document trước lỗi được insert, các document sau lỗi bị bỏ qua
- [ ] C. Tất cả document đều được insert, lỗi bị bỏ qua
- [ ] D. Chỉ document bị lỗi bị bỏ qua, phần còn lại tiếp tục

<details> <summary>💡 Đáp án</summary>

**B. Các document trước lỗi được insert, các document sau lỗi bị bỏ qua**

Mặc định `insertMany()` dùng `ordered: true`, nên khi gặp lỗi sẽ dừng lại. Nếu dùng `{ ordered: false }`, MongoDB sẽ tiếp tục insert các document còn lại dù có lỗi.

</details>

---

### 2.2 – Update không dùng Update Operators

**Q7.** Cho document hiện tại: `{ "_id": 1, "name": "Alice", "age": 30 }`. Sau khi chạy lệnh:

```js
db.users.replaceOne({ _id: 1 }, { name: "Bob" })
```

Document sẽ trở thành:

- [ ] A. `{ "_id": 1, "name": "Bob", "age": 30 }`
- [ ] B. `{ "_id": 1, "name": "Bob" }`
- [ ] C. `{ "name": "Bob" }`
- [ ] D. Lỗi vì thiếu trường `age`

<details> <summary>💡 Đáp án</summary>

**B. `{ "_id": 1, "name": "Bob" }`**

Khi không dùng update operators, `replaceOne()` sẽ thay thế toàn bộ document bằng document mới. Trường `age` sẽ bị xóa. `_id` được giữ lại.

</details>

---

### 2.3 – Sử dụng $set

**Q8.** Cho document: `{ "_id": 1, "name": "Alice", "age": 30 }`. Sau khi chạy:

```js
db.users.updateOne({ _id: 1 }, { $set: { age: 31, city: "Hanoi" } })
```

Document sẽ là:

- [ ] A. `{ "_id": 1, "age": 31, "city": "Hanoi" }`
- [ ] B. `{ "_id": 1, "name": "Alice", "age": 31 }`
- [ ] C. `{ "_id": 1, "name": "Alice", "age": 31, "city": "Hanoi" }`
- [ ] D. `{ "_id": 1, "name": "Alice", "age": 30, "city": "Hanoi" }`

<details> <summary>💡 Đáp án</summary>

**C. `{ "_id": 1, "name": "Alice", "age": 31, "city": "Hanoi" }`**

`$set` chỉ cập nhật/thêm các trường được chỉ định. Các trường khác (`name`) không bị ảnh hưởng.

</details>

---

### 2.4 – Upsert

**Q9.** Lệnh nào thực hiện upsert – insert nếu document không tồn tại, update nếu đã tồn tại?

- [ ] A. `db.users.updateOne({ name: "Alice" }, { $set: { age: 25 } })`
- [ ] B. `db.users.updateOne({ name: "Alice" }, { $set: { age: 25 } }, { upsert: true })`
- [ ] C. `db.users.insertOrUpdate({ name: "Alice" }, { age: 25 })`
- [ ] D. `db.users.upsert({ name: "Alice" }, { age: 25 })`

<details> <summary>💡 Đáp án</summary>

**B.**

Upsert được kích hoạt bằng option `{ upsert: true }` trong `updateOne()` hoặc `updateMany()`.

</details>

---

### 2.5 – Update nhiều Documents

**Q10.** Lệnh nào cập nhật trường `status: "active"` cho TẤT CẢ document có `age` lớn hơn 18?

- [ ] A. `db.users.updateOne({ age: { $gt: 18 } }, { $set: { status: "active" } })`
- [ ] B. `db.users.updateMany({ age: { $gt: 18 } }, { $set: { status: "active" } })`
- [ ] C. `db.users.update({ age: { $gt: 18 } }, { $set: { status: "active" } })`
- [ ] D. `db.users.updateAll({ age: { $gt: 18 } }, { $set: { status: "active" } })`

<details> <summary>💡 Đáp án</summary>

**B. `updateMany()`**

`updateOne()` chỉ cập nhật document đầu tiên phù hợp. `updateMany()` cập nhật tất cả document phù hợp.

</details>

---

### 2.6 – findAndModify

**Q11.** `findAndModify()` khác gì so với `updateOne()`?

- [ ] A. `findAndModify()` trả về document trước hoặc sau khi update
- [ ] B. `findAndModify()` nhanh hơn `updateOne()`
- [ ] C. `findAndModify()` không hỗ trợ upsert
- [ ] D. Không có sự khác biệt

<details> <summary>💡 Đáp án</summary>

**A.**

`findAndModify()` là một atomic operation vừa tìm, vừa update, và trả về document (trước hoặc sau khi sửa tùy option `new`). Điều này hữu ích trong môi trường concurrent.

</details>

---

### 2.7 – Delete

**Q12.** Lệnh nào xóa TẤT CẢ document có `status: "inactive"`?

- [ ] A. `db.users.deleteOne({ status: "inactive" })`
- [ ] B. `db.users.deleteMany({ status: "inactive" })`
- [ ] C. `db.users.remove({ status: "inactive" })`
- [ ] D. `db.users.drop({ status: "inactive" })`

<details> <summary>💡 Đáp án</summary>

**B. `deleteMany()`**

- `deleteOne()` – xóa document đầu tiên phù hợp
- `deleteMany()` – xóa tất cả document phù hợp
- `drop()` – xóa toàn bộ collection

</details>

---

### 2.8 – Find với Equality Constraint

**Q13.** Lệnh nào tìm document có `age = 25`?

- [ ] A. `db.users.find({ age: 25 })`
- [ ] B. `db.users.find({ age: { $equal: 25 } })`
- [ ] C. `db.users.query({ age: 25 })`
- [ ] D. `db.users.search({ age: 25 })`

<details> <summary>💡 Đáp án</summary>

**A. `db.users.find({ age: 25 })`**

Đây là cú pháp equality constraint cơ bản trong MongoDB.

</details>

---

### 2.9 – Query trên Array Field

**Q14.** Collection có document: `{ "tags": ["mongodb", "database", "nosql"] }`. Lệnh nào khớp document này?

- [ ] A. `db.col.find({ tags: "mongodb" })`
- [ ] B. `db.col.find({ tags: ["mongodb"] })`
- [ ] C. `db.col.find({ tags: { $all: ["mongodb", "nosql"] } })`
- [ ] D. Cả A và C

<details> <summary>💡 Đáp án</summary>

**D. Cả A và C**

- `{ tags: "mongodb" }` – tìm document có array `tags` chứa phần tử "mongodb"
- `{ tags: { $all: ["mongodb", "nosql"] } }` – tìm document có array chứa cả "mongodb" lẫn "nosql"
- `{ tags: ["mongodb"] }` – tìm array khớp CHÍNH XÁC `["mongodb"]` (không khớp)

</details>

---

### 2.10 – Relational Operators

**Q15.** Operator nào sau đây KHÔNG phải relational operator trong MongoDB?

- [ ] A. `$gt`
- [ ] B. `$gte`
- [ ] C. `$lt`
- [ ] D. `$between`
- [ ] E. `$ne`

<details> <summary>💡 Đáp án</summary>

**D. `$between`**

MongoDB không có operator `$between`. Để query khoảng giá trị, dùng kết hợp `$gte` và `$lte`:

```js
db.col.find({ age: { $gte: 18, $lte: 30 } })
```

</details>

---

### 2.11 – $in Operator

**Q16.** Lệnh nào tìm tất cả user có `city` là "Hanoi" hoặc "HCMC"?

- [ ] A. `db.users.find({ city: { $or: ["Hanoi", "HCMC"] } })`
- [ ] B. `db.users.find({ city: { $in: ["Hanoi", "HCMC"] } })`
- [ ] C. `db.users.find({ $or: [{ city: "Hanoi" }, { city: "HCMC" }] })`
- [ ] D. Cả B và C

<details> <summary>💡 Đáp án</summary>

**D. Cả B và C đều đúng**

`$in` và `$or` đều cho kết quả như nhau trong trường hợp này, nhưng `$in` ngắn gọn hơn khi so sánh cùng một field với nhiều giá trị.

</details>

---

### 2.12 – $elemMatch

**Q17.** Tại sao cần dùng `$elemMatch` khi query array of objects?

- [ ] A. Để tìm chính xác toàn bộ array
- [ ] B. Để đảm bảo các điều kiện phải khớp trên CÙNG một phần tử trong array
- [ ] C. Để đếm số phần tử trong array
- [ ] D. Để sort array

<details> <summary>💡 Đáp án</summary>

**B.**

Ví dụ: `{ scores: { $elemMatch: { type: "math", score: { $gte: 90 } } } }` đảm bảo rằng phải có một phần tử trong `scores` vừa có `type: "math"` VÀ `score >= 90`. Nếu không dùng `$elemMatch`, MongoDB có thể khớp khi các điều kiện thỏa mãn ở các phần tử khác nhau.

</details>

---

### 2.13 – Logical Operators

**Q18.** Operator nào là logical operator trong MongoDB? _(Chọn tất cả đúng)_

- [ ] A. `$and`
- [ ] B. `$or`
- [ ] C. `$not`
- [ ] D. `$nor`
- [ ] E. `$xor`

<details> <summary>💡 Đáp án</summary>

**A, B, C, D**

MongoDB có 4 logical operators: `$and`, `$or`, `$not`, `$nor`. Không có `$xor`.

</details>

---

### 2.14 – Sort và Limit

**Q19.** Cho collection `products`. Lệnh nào lấy 3 sản phẩm có `price` cao nhất?

- [ ] A. `db.products.find().limit(3).sort({ price: -1 })`
- [ ] B. `db.products.find().sort({ price: -1 }).limit(3)`
- [ ] C. `db.products.find().sort({ price: 1 }).limit(3)`
- [ ] D. `db.products.find({ limit: 3, sort: { price: -1 } })`

<details> <summary>💡 Đáp án</summary>

**B. (A cũng cho kết quả tương tự)**

Sort `-1` là descending (giá cao → thấp), `1` là ascending (giá thấp → cao). Dù viết `limit` trước hay sau `sort`, MongoDB luôn thực hiện sort trước.

</details>

---

### 2.15 – Projection

**Q20.** Projection nào sau đây KHÔNG hợp lệ (trừ trường hợp `_id`)?

- [ ] A. `{ name: 1, age: 1 }`
- [ ] B. `{ name: 0, age: 0 }`
- [ ] C. `{ name: 1, age: 0 }`
- [ ] D. `{ _id: 0, name: 1 }`

<details> <summary>💡 Đáp án</summary>

**C. `{ name: 1, age: 0 }` – KHÔNG hợp lệ**

Trong MongoDB, không thể trộn lẫn inclusion (1) và exclusion (0) trong cùng một projection, **ngoại trừ** `_id` có thể đặt `0` cùng với các trường `1`.

</details>

---

### 2.16 – Cursor

**Q21.** Làm thế nào để lấy tất cả kết quả từ một cursor trong MongoDB shell?

- [ ] A. `cursor.getAll()`
- [ ] B. `cursor.toArray()`
- [ ] C. `cursor.forEach(doc => printjson(doc))`
- [ ] D. Cả B và C

<details> <summary>💡 Đáp án</summary>

**D. Cả B và C**

`toArray()` trả về toàn bộ kết quả dưới dạng mảng. `forEach()` duyệt qua từng document.

</details>

---

### 2.17 – Đếm Documents

**Q22.** Lệnh nào đếm số document có `status: "active"`?

- [ ] A. `db.users.count({ status: "active" })`
- [ ] B. `db.users.countDocuments({ status: "active" })`
- [ ] C. `db.users.find({ status: "active" }).count()`
- [ ] D. Cả B và C đều được khuyến nghị

<details> <summary>💡 Đáp án</summary>

**D. Cả B và C**

`countDocuments()` là phương thức được khuyến nghị hiện nay. `count()` đã deprecated. Ngoài ra còn có `estimatedDocumentCount()` để đếm nhanh (không filter).

</details>

---

### 2.18 – Search Index

**Q23.** Để tạo Atlas Search Index cho collection `articles` trên trường `content`, lệnh nào là đúng?

- [ ] A. `db.articles.createIndex({ content: "text" })`
- [ ] B. Tạo Search Index qua Atlas UI hoặc Atlas Search API với `mappings`
- [ ] C. `db.articles.createSearchIndex({ content: 1 })`
- [ ] D. `db.articles.ensureIndex({ content: "search" })`

<details> <summary>💡 Đáp án</summary>

**B.**

Atlas Search Index được tạo thông qua Atlas UI, Atlas CLI, hoặc Data API – không phải qua `createIndex()` thông thường. Nó sử dụng Apache Lucene bên dưới.

</details>

---

### 2.19 – Search Query

**Q24.** Lệnh nào thực hiện full-text search với Atlas Search?

- [ ] A. `db.articles.find({ $text: { $search: "mongodb" } })`
- [ ] B. `db.articles.aggregate([{ $search: { index: "default", text: { query: "mongodb", path: "content" } } }])`
- [ ] C. `db.articles.find({ content: /mongodb/ })`
- [ ] D. `db.articles.search({ query: "mongodb" })`

<details> <summary>💡 Đáp án</summary>

**B.**

Atlas Search sử dụng aggregation pipeline với stage `$search`. Đây khác với `$text` operator thông thường.

</details>

---

### 2.20 – Aggregation với $match và $group

**Q25.** Cho collection `orders`. Pipeline sau trả về gì?

```js
db.orders.aggregate([
  { $match: { status: "completed" } },
  { $group: { _id: "$customerId", total: { $sum: "$amount" } } }
])
```

- [ ] A. Tất cả đơn hàng completed
- [ ] B. Tổng số tiền mỗi khách hàng với đơn hàng completed
- [ ] C. Số lượng đơn hàng completed theo khách hàng
- [ ] D. Tổng số tiền tất cả đơn hàng

<details> <summary>💡 Đáp án</summary>

**B.**

`$match` lọc chỉ giữ đơn hàng `completed`. `$group` nhóm theo `customerId` và tính tổng `amount`.

</details>

---

### 2.21 – Aggregation với $lookup

**Q26.** `$lookup` trong aggregation pipeline dùng để làm gì?

- [ ] A. Tìm kiếm full-text
- [ ] B. Join dữ liệu từ collection khác (tương tự LEFT OUTER JOIN)
- [ ] C. Lọc document theo điều kiện
- [ ] D. Tạo index mới

<details> <summary>💡 Đáp án</summary>

**B.**

`$lookup` thực hiện một LEFT OUTER JOIN với collection khác trong cùng database.

```js
{
  $lookup: {
    from: "products",
    localField: "productId",
    foreignField: "_id",
    as: "productDetails"
  }
}
```

</details>

---

### 2.22 – Aggregation với $out

**Q27.** Stage `$out` trong aggregation pipeline làm gì?

- [ ] A. Xuất kết quả ra file CSV
- [ ] B. Ghi kết quả aggregation vào một collection mới (hoặc ghi đè collection cũ)
- [ ] C. Trả về kết quả dưới dạng cursor
- [ ] D. Tạo view từ kết quả aggregation

<details> <summary>💡 Đáp án</summary>

**B.**

`$out` phải là stage cuối cùng trong pipeline. Nó ghi kết quả vào collection được chỉ định, thay thế toàn bộ nội dung nếu collection đã tồn tại.

</details>

---

## Section 3: INDEXES (17%)

### 3.1 – Index cải thiện Collection Scan

**Q28.** Query `db.users.find({ email: "alice@example.com" })` đang làm collection scan. Index nào giúp cải thiện?

- [ ] A. `db.users.createIndex({ name: 1 })`
- [ ] B. `db.users.createIndex({ email: 1 })`
- [ ] C. `db.users.createIndex({ age: 1 })`
- [ ] D. Không có index nào giúp được

<details> <summary>💡 Đáp án</summary>

**B. `{ email: 1 }`**

Index phải khớp với trường được dùng trong filter của query để MongoDB có thể sử dụng nó.

</details>

---

### 3.2 – Index cho Array Field

**Q29.** Query `db.posts.find({ tags: "mongodb" })` đang collection scan. Index nào phù hợp?

- [ ] A. `db.posts.createIndex({ tags: "text" })`
- [ ] B. `db.posts.createIndex({ tags: 1 })`
- [ ] C. `db.posts.createIndex({ "tags.$": 1 })`
- [ ] D. Không thể đánh index trên array field

<details> <summary>💡 Đáp án</summary>

**B. `{ tags: 1 }`**

MongoDB tự động tạo **Multikey Index** khi đánh index trên array field, cho phép index từng phần tử trong array.

</details>

---

### 3.3 – Index cho Sort

**Q30.** Query `db.products.find().sort({ category: 1, price: -1 })` đang collection scan. Index nào phù hợp nhất?

- [ ] A. `db.products.createIndex({ category: 1 })`
- [ ] B. `db.products.createIndex({ price: -1 })`
- [ ] C. `db.products.createIndex({ category: 1, price: -1 })`
- [ ] D. `db.products.createIndex({ price: -1, category: 1 })`

<details> <summary>💡 Đáp án</summary>

**C. `{ category: 1, price: -1 }`**

Compound index phải khớp thứ tự và hướng sort. Index `{ category: 1, price: -1 }` phù hợp với sort `{ category: 1, price: -1 }`.

</details>

---

### 3.4 – Số lượng Index trong Collection

**Q31.** MongoDB tự động tạo index nào cho mỗi collection?

- [ ] A. Không tạo index nào
- [ ] B. Index trên trường đầu tiên của document
- [ ] C. Index trên trường `_id`
- [ ] D. Index trên tất cả trường

<details> <summary>💡 Đáp án</summary>

**C. Index trên `_id`**

MongoDB tự động tạo một unique index trên `_id` cho mỗi collection. Đây là index mặc định và không thể bị xóa.

</details>

---

### 3.5 – Đánh đổi khi dùng Index

**Q32.** Nhược điểm của việc tạo quá nhiều index là gì? _(Chọn tất cả đúng)_

- [ ] A. Tốc độ write (insert/update/delete) chậm hơn
- [ ] B. Tốn thêm dung lượng lưu trữ
- [ ] C. Tốc độ read chậm hơn
- [ ] D. Index phải được cập nhật mỗi khi có write operation

<details> <summary>💡 Đáp án</summary>

**A, B, D**

Index giúp tăng tốc read nhưng làm chậm write vì mỗi write operation phải cập nhật tất cả index liên quan. Index cũng tốn RAM và disk space.

</details>

---

### 3.6 – Explain Plan

**Q33.** Khi chạy `explain("executionStats")`, dấu hiệu nào cho thấy có vấn đề về performance?

- [ ] A. `COLLSCAN` trong `winningPlan`
- [ ] B. `docsExamined` rất lớn so với `nReturned`
- [ ] C. `IXSCAN` trong `winningPlan`
- [ ] D. Cả A và B

<details> <summary>💡 Đáp án</summary>

**D. Cả A và B**

- `COLLSCAN` = collection scan, không dùng index → tiềm ẩn vấn đề performance
- `docsExamined >> nReturned` = MongoDB đọc nhiều document nhưng trả về ít → thiếu index phù hợp
- `IXSCAN` = index scan, là dấu hiệu tốt

</details>

---

## Section 4: DATA MODELING (4%)

### 4.1 – Embedded vs Linked Relationships

**Q34.** Khi nào nên EMBED document thay vì LINK (reference)?

- [ ] A. Dữ liệu con thường xuyên được truy cập cùng với dữ liệu cha
- [ ] B. Dữ liệu con có thể grow không giới hạn (unbounded)
- [ ] C. Dữ liệu con được chia sẻ giữa nhiều document cha
- [ ] D. Dữ liệu con rất ít khi được truy cập

<details> <summary>💡 Đáp án</summary>

**A.**

Nên embed khi dữ liệu con luôn được đọc cùng với cha (giảm số lần query). Nên dùng reference khi: dữ liệu có thể grow không giới hạn, dữ liệu được dùng chung, hoặc dữ liệu cần được cập nhật độc lập.

</details>

---

### 4.2 – Anti-patterns

**Q35.** Đâu là anti-pattern trong MongoDB data modeling? _(Chọn tất cả đúng)_

- [ ] A. Massive arrays (array với hàng ngàn phần tử trong document)
- [ ] B. Bloated documents (document quá lớn, vượt 16MB)
- [ ] C. Embedding dữ liệu thường được đọc cùng nhau
- [ ] D. Unnecessary indexes
- [ ] E. Referencing thay vì embedding khi dữ liệu luôn cần nhau

<details> <summary>💡 Đáp án</summary>

**A, B, D, E**

C là best practice, không phải anti-pattern. Các anti-pattern phổ biến: massive arrays, bloated documents, unnecessary indexes, unbounded documents, case-insensitive queries không có index.

</details>

---

## Section 5: TOOLS AND TOOLING (2%)

### 5.1 – Atlas Sample Dataset & Data Explorer

**Q36.** Sau khi load Atlas Sample Dataset, làm thế nào để xem document đầu tiên trong collection `sample_mflix.movies`?

- [ ] A. Dùng MongoDB Compass
- [ ] B. Vào Atlas UI → Browse Collections → chọn database → chọn collection
- [ ] C. Chạy `db.movies.findOne()` trong mongosh
- [ ] D. Tất cả các cách trên đều đúng

<details> <summary>💡 Đáp án</summary>

**D.**

Atlas Data Explorer (trên Atlas UI), MongoDB Compass, và mongosh đều là công cụ hợp lệ để xem dữ liệu.

</details>

---

## Section 6: DRIVERS (18%)

### 6.1 & 6.2 – Driver là gì & Cách kết nối

**Q37.** MongoDB Driver là gì?

- [ ] A. Một GUI để quản lý database
- [ ] B. Thư viện cho phép ứng dụng giao tiếp với MongoDB qua programming language cụ thể
- [ ] C. Một công cụ backup dữ liệu
- [ ] D. Plugin để tích hợp MongoDB với IDE

<details> <summary>💡 Đáp án</summary>

**B.**

MongoDB cung cấp official drivers cho nhiều ngôn ngữ: Python, Node.js, Java, C#, Go, v.v. Driver chuyển đổi các lệnh trong code thành giao thức MongoDB Wire Protocol.

</details>

---

### 6.3 – URI String / Connection String

**Q38.** URI kết nối MongoDB Atlas có dạng nào?

- [ ] A. `mongodb://username:password@host:port/database`
- [ ] B. `mongodb+srv://username:password@cluster.mongodb.net/database`
- [ ] C. `http://cluster.mongodb.net/database`
- [ ] D. `jdbc:mongodb://cluster.mongodb.net`

<details> <summary>💡 Đáp án</summary>

**B.**

`mongodb+srv://` là format cho Atlas cluster (dùng DNS SRV records). `mongodb://` là format thông thường cho standalone/replica set.

</details>

---

**Q39.** Các thành phần của URI string MongoDB gồm những gì? _(Chọn tất cả đúng)_

- [ ] A. Scheme (`mongodb://` hoặc `mongodb+srv://`)
- [ ] B. Username và Password
- [ ] C. Host và Port
- [ ] D. Database name
- [ ] E. Connection options (authSource, tls, v.v.)

<details> <summary>💡 Đáp án</summary>

**Tất cả A, B, C, D, E**

Cấu trúc đầy đủ: `scheme://[username:password@]host[:port][/database][?options]`

</details>

---

### 6.4 – Connection Pooling

**Q40.** Connection Pooling trong MongoDB Driver mang lại lợi ích gì?

- [ ] A. Tái sử dụng các kết nối đã có, giảm overhead tạo kết nối mới
- [ ] B. Tự động backup dữ liệu
- [ ] C. Tăng tốc độ đọc/ghi bằng cách cache dữ liệu
- [ ] D. Mã hóa kết nối

<details> <summary>💡 Đáp án</summary>

**A.**

Connection pool duy trì một tập hợp kết nối sẵn sàng, tránh phải tạo kết nối mới cho mỗi request (tốn thời gian). `MongoClient` nên được khởi tạo một lần và dùng xuyên suốt ứng dụng.

</details>

---

### 6.5 – Insert Syntax (Python ví dụ)

**Q41.** Cú pháp insert một document trong Python driver?

- [ ] A. `collection.insert({ "name": "Alice" })`
- [ ] B. `collection.insert_one({ "name": "Alice" })`
- [ ] C. `collection.add({ "name": "Alice" })`
- [ ] D. `collection.save({ "name": "Alice" })`

<details> <summary>💡 Đáp án</summary>

**B. `insert_one()`**

Python driver dùng `insert_one()` và `insert_many()`. (Node.js: `insertOne()`, `insertMany()`)

</details>

---

### 6.6 – Update Syntax

**Q42.** Trong Node.js driver, lệnh nào update tất cả document có `age < 18` thêm trường `isMinor: true`?

- [ ] A. `collection.updateOne({ age: { $lt: 18 } }, { $set: { isMinor: true } })`
- [ ] B. `collection.updateMany({ age: { $lt: 18 } }, { $set: { isMinor: true } })`
- [ ] C. `collection.update({ age: { $lt: 18 } }, { isMinor: true })`
- [ ] D. `collection.setMany({ age: { $lt: 18 } }, { isMinor: true })`

<details> <summary>💡 Đáp án</summary>

**B. `updateMany()`**

</details>

---

### 6.7 – Delete Syntax

**Q43.** Cú pháp xóa một document trong Node.js driver?

- [ ] A. `collection.remove({ _id: id })`
- [ ] B. `collection.deleteOne({ _id: id })`
- [ ] C. `collection.drop({ _id: id })`
- [ ] D. `collection.destroy({ _id: id })`

<details> <summary>💡 Đáp án</summary>

**B. `deleteOne()`**

Các method hiện đại: `deleteOne()`, `deleteMany()`.

</details>

---

### 6.8 – Find Syntax

**Q44.** Sự khác biệt giữa `findOne()` và `find()` trong driver?

- [ ] A. `findOne()` trả về document đầu tiên phù hợp; `find()` trả về cursor
- [ ] B. `findOne()` nhanh hơn `find()`
- [ ] C. `find()` chỉ trả về một document
- [ ] D. Không có sự khác biệt

<details> <summary>💡 Đáp án</summary>

**A.**

`findOne()` → trả về trực tiếp một document (hoặc null). `find()` → trả về cursor cần iterate để lấy kết quả.

</details>

---

### 6.9 – Aggregation Pipeline Syntax

**Q45.** Trong Node.js driver, cách chạy aggregation pipeline?

```js
const pipeline = [
  { $match: { status: "active" } },
  { $group: { _id: "$city", count: { $sum: 1 } } }
];
```

- [ ] A. `collection.aggregate(pipeline)`
- [ ] B. `collection.find(pipeline)`
- [ ] C. `collection.run(pipeline)`
- [ ] D. `collection.execute(pipeline)`

<details> <summary>💡 Đáp án</summary>

**A. `collection.aggregate(pipeline)`**

</details>

---

### 6.10 – MQL vs Aggregation Framework Syntax

**Q46.** Điểm khác nhau chính giữa MQL và Aggregation Framework?

- [ ] A. MQL chỉ dùng để read, Aggregation dùng để write
- [ ] B. Aggregation Framework dùng pipeline stages ($match, $group, $sort...) để transform dữ liệu; MQL dùng operators trực tiếp trong find()
- [ ] C. MQL nhanh hơn Aggregation Framework
- [ ] D. Aggregation Framework không hỗ trợ filter

<details> <summary>💡 Đáp án</summary>

**B.**

MQL (MongoDB Query Language) dùng trong `find()`, `updateOne()`, v.v. Aggregation Framework xử lý dữ liệu qua nhiều stages tuần tự, mạnh hơn cho transform và analytics.

</details>

---

## 🎯 Câu Hỏi Tổng Hợp

**Q47.** Bạn có collection `inventory` với document:

```json
{ "_id": 1, "item": "pen", "qty": 100, "tags": ["office", "school"] }
```

Lệnh nào tăng `qty` lên 50 và thêm tag "stationery"?

- [ ] A. `db.inventory.updateOne({ _id: 1 }, { qty: 150, tags: ["office", "school", "stationery"] })`
- [ ] B. `db.inventory.updateOne({ _id: 1 }, { $set: { qty: 150 }, $push: { tags: "stationery" } })`
- [ ] C. `db.inventory.updateOne({ _id: 1 }, { $inc: { qty: 50 }, $push: { tags: "stationery" } })`
- [ ] D. `db.inventory.updateOne({ _id: 1 }, { $add: { qty: 50 }, $append: { tags: "stationery" } })`

<details> <summary>💡 Đáp án</summary>

**C.**

- `$inc: { qty: 50 }` – tăng giá trị qty lên 50 (không phải set = 50)
- `$push: { tags: "stationery" }` – thêm một phần tử vào array

</details>

---

**Q48.** Index nào hỗ trợ query `{ age: { $gt: 25 }, city: "Hanoi" }` tốt nhất?

- [ ] A. `{ age: 1 }`
- [ ] B. `{ city: 1 }`
- [ ] C. `{ city: 1, age: 1 }` (equality field đứng trước)
- [ ] D. `{ age: 1, city: 1 }`

<details> <summary>💡 Đáp án</summary>

**C. `{ city: 1, age: 1 }`**

ESR Rule (Equality, Sort, Range): trường equality nên đứng đầu index, tiếp theo là sort, cuối cùng là range. `city` là equality, `age` là range.

</details>

---

**Q49.** Document trong MongoDB có kích thước tối đa là bao nhiêu?

- [ ] A. 4 MB
- [ ] B. 8 MB
- [ ] C. 16 MB
- [ ] D. 32 MB

<details> <summary>💡 Đáp án</summary>

**C. 16 MB**

Mỗi BSON document trong MongoDB có giới hạn tối đa 16 megabytes.

</details>

---

**Q50.** Khi nào nên dùng `$unwind` trong aggregation pipeline?

- [ ] A. Để loại bỏ các document trùng lặp
- [ ] B. Để "mở" một array field ra thành nhiều document riêng biệt
- [ ] C. Để kết hợp hai collection
- [ ] D. Để sắp xếp kết quả

<details> <summary>💡 Đáp án</summary>

**B.**

`$unwind: "$tags"` sẽ tạo ra một document cho mỗi phần tử trong array `tags`. Ví dụ: document với `tags: ["a", "b"]` sẽ thành 2 documents riêng biệt.

</details>

---

**Q51.** Atlas Search khác gì so với MongoDB Text Search thông thường?

- [ ] A. Atlas Search dùng Apache Lucene, hỗ trợ fuzzy search, autocomplete, relevance scoring mạnh hơn
- [ ] B. Atlas Search nhanh hơn nhưng ít tính năng hơn
- [ ] C. Text Search mạnh hơn Atlas Search
- [ ] D. Không có sự khác biệt

<details> <summary>💡 Đáp án</summary>

**A.**

Atlas Search được xây dựng trên Apache Lucene, hỗ trợ: fuzzy matching, autocomplete, highlight, faceted search, relevance scoring tùy chỉnh – mạnh hơn nhiều so với `$text` operator thông thường.

</details>

---

**Q52.** `$group` stage yêu cầu bắt buộc trường gì?

- [ ] A. `$sum`
- [ ] B. `_id`
- [ ] C. `$count`
- [ ] D. `$field`

<details> <summary>💡 Đáp án</summary>

**B. `_id`**

`$group` bắt buộc phải có `_id` để xác định tiêu chí nhóm. Có thể đặt `_id: null` để nhóm tất cả document lại.

</details>

---

**Q53.** Khi sử dụng `MongoClient` trong ứng dụng, best practice là gì?

- [ ] A. Tạo `MongoClient` mới cho mỗi request
- [ ] B. Tạo một `MongoClient` duy nhất khi khởi động ứng dụng và tái sử dụng
- [ ] C. Tạo `MongoClient` mới cho mỗi database operation
- [ ] D. Tạo `MongoClient` mới cho mỗi collection

<details> <summary>💡 Đáp án</summary>

**B.**

`MongoClient` quản lý connection pool nội bộ. Tạo một instance duy nhất và tái sử dụng sẽ tận dụng connection pooling, giảm latency và tránh lãng phí tài nguyên.

</details>

---

## 📊 Tổng Kết Các Domain

|Section|Chủ đề|Tỷ lệ|Số câu ước tính|
|---|---|---|---|
|1|MongoDB Overview & Document Model|8%|~4 câu|
|2|CRUD|51%|~27 câu|
|3|Indexes|17%|~9 câu|
|4|Data Modeling|4%|~2 câu|
|5|Tools & Tooling|2%|~1 câu|
|6|Drivers|18%|~10 câu|

---

## 📚 Tài Nguyên Học Tập

- **MongoDB University** – Free learning paths tại university.mongodb.com
- **MongoDB Documentation** – docs.mongodb.com
- **Practice Questions** – Associate Developer Practice Questions (trên trang MongoDB)
- **Community Forums** – mongodb.com/community/forums

---

_Chúc bạn ôn thi hiệu quả và đạt chứng chỉ MongoDB Associate Developer! 🍀_