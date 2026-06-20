# Bulk CV Upload - API Endpoint Specification

## 1. Overview

Bulk Upload cho phép người dùng upload nhiều CV cùng lúc.

Frontend thực hiện:

* Chia danh sách file thành các chunk.
* Mỗi chunk gửi tối đa 5 file đồng thời.
* Sau khi server nhận thành công, frontend kết nối WebSocket để nhận trạng thái xử lý.

Backend thực hiện:

* Validate file.
* Lưu file gốc.
* Tạo Batch Upload.
* Đưa từng file vào Background Job Queue.
* Chuyển PDF/DOCX sang Markdown.
* Gửi Markdown cho AI để trích xuất thông tin.
* Kiểm tra duplicate.
* Lưu metadata vào PostgreSQL.
* Gửi tiến trình xử lý qua WebSocket.
* Nếu một file lỗi, ghi nhận lỗi và tiếp tục xử lý các file còn lại.

---

# 2. Upload Bulk CV

## Endpoint

```http
POST /api/v1/cvs/bulk-upload
```

## Authentication

```text
Authorization: Bearer <JWT_TOKEN>
```

## Content-Type

```text
multipart/form-data
```

## Request

| Field     | Type        | Required | Description                                                   |
| --------- | ----------- | -------- | ------------------------------------------------------------- |
| files     | IFormFile[] | Yes      | Danh sách file PDF/DOCX của một chunk upload.                 |
| requestId | string      | Yes      | Idempotency key của request.                                  |
| batchId   | string      | No       | Batch Id đã tạo trước đó. Nếu null, backend tự tạo batch mới. |

### Example Request

```http
POST /api/v1/cvs/bulk-upload
Content-Type: multipart/form-data

requestId: 5ef51af8-0af8-4c8f-8b5d-3c7b27f6ad4a
batchId: batch_01

files: cv01.pdf
files: cv02.pdf
files: cv03.pdf
files: cv04.docx
files: cv05.pdf
```

## Response

```json
{
  "success": true,
  "message": "Files accepted for processing.",
  "data": {
    "batchId": "batch_01",
    "acceptedFiles": 5,
    "totalAcceptedFiles": 20,
    "websocketEndpoint": "/ws/upload-progress/batch_01"
  }
}
```

### HTTP Status

| Status                    | Description                           |
| ------------------------- | ------------------------------------- |
| 202 Accepted              | Server đã nhận file và đưa vào queue. |
| 400 Bad Request           | File không hợp lệ.                    |
| 401 Unauthorized          | Chưa đăng nhập.                       |
| 413 Payload Too Large     | Kích thước file vượt giới hạn.        |
| 500 Internal Server Error | Lỗi hệ thống.                         |

---

# 3. WebSocket - Upload Progress

## Endpoint

```text
GET /ws/upload-progress/{batchId}
```

Frontend mở WebSocket ngay sau khi nhận được `batchId`.

Backend sẽ gửi trạng thái realtime của từng file và toàn bộ batch.

## Event: File Started

```json
{
  "type": "FILE_STARTED",
  "batchId": "batch_01",
  "fileId": "item_001",
  "fileName": "cv01.pdf",
  "status": "PROCESSING"
}
```

## Event: File Completed

```json
{
  "type": "FILE_COMPLETED",
  "batchId": "batch_01",
  "fileId": "item_001",
  "fileName": "cv01.pdf",
  "status": "COMPLETED",
  "candidateName": "Nguyen Van A"
}
```

## Event: File Failed

```json
{
  "type": "FILE_FAILED",
  "batchId": "batch_01",
  "fileId": "item_002",
  "fileName": "cv02.pdf",
  "status": "FAILED",
  "errorCode": "AI_EXTRACTION_ERROR",
  "errorMessage": "Unable to extract candidate information."
}
```

## Event: Batch Progress

`progress` được tính theo:

```text
(completedFiles + failedFiles + cancelledFiles) / totalFiles
```

```json
{
  "type": "BATCH_PROGRESS",
  "batchId": "batch_01",
  "totalFiles": 100,
  "completedFiles": 55,
  "failedFiles": 3,
  "cancelledFiles": 0,
  "processingFiles": 2,
  "pendingFiles": 40,
  "progress": 58
}
```

## Event: Batch Completed

```json
{
  "type": "BATCH_COMPLETED",
  "batchId": "batch_01",
  "totalFiles": 100,
  "completedFiles": 95,
  "failedFiles": 5,
  "cancelledFiles": 0
}
```

## Event: Batch Cancelling

```json
{
  "type": "BATCH_CANCELLING",
  "batchId": "batch_01"
}
```

## Event: Batch Cancelled

```json
{
  "type": "BATCH_CANCELLED",
  "batchId": "batch_01",
  "completedFiles": 40,
  "failedFiles": 2,
  "cancelledFiles": 58
}
```

---

# 4. Get Batch Upload Status

Cho phép frontend đồng bộ lại trạng thái khi người dùng refresh trang hoặc mất kết nối WebSocket.

## Endpoint

```http
GET /api/v1/cvs/bulk-upload/{batchId}
```

## Response

```json
{
  "success": true,
  "data": {
    "batchId": "batch_01",
    "status": "PROCESSING",
    "totalFiles": 100,
    "completedFiles": 55,
    "failedFiles": 3,
    "cancelledFiles": 0,
    "processingFiles": 2,
    "pendingFiles": 40,
    "progress": 58
  }
}
```

### Batch Status

| Status     | Description                    |
| ---------- | ------------------------------ |
| PENDING    | Batch vừa được tạo.            |
| PROCESSING | Đang xử lý.                    |
| CANCELLING | Đang dừng các file chưa xử lý. |
| COMPLETED  | Đã xử lý xong toàn bộ.         |
| CANCELLED  | Batch đã bị hủy.               |

---

# 5. Cancel Batch Upload

Người dùng có thể hủy các file chưa được xử lý.

## Endpoint

```http
POST /api/v1/cvs/bulk-upload/{batchId}/cancel
```

## Business Rules

* Chỉ các file ở trạng thái `PENDING` mới bị hủy.
* File đang `PROCESSING` vẫn tiếp tục chạy đến khi hoàn thành.
* File đã `COMPLETED` hoặc `FAILED` giữ nguyên.
* Sau khi hủy, Worker không lấy thêm file mới từ batch này.

## Response

```json
{
  "success": true,
  "message": "Batch cancellation requested.",
  "data": {
    "batchId": "batch_01",
    "status": "CANCELLING"
  }
}
```

### HTTP Status

| Status          | Description                      |
| --------------- | -------------------------------- |
| 200 OK          | Yêu cầu hủy thành công.          |
| 400 Bad Request | Batch không hợp lệ.              |
| 404 Not Found   | Không tìm thấy batch.            |
| 409 Conflict    | Batch đã hoàn thành hoặc đã hủy. |

---

# 6. File Processing State Machine

Mỗi file được xử lý độc lập.

```text
PENDING
   │
   ▼
UPLOADED
   │
   ▼
PROCESSING
   ├────────────► FAILED
   ├────────────► CANCELLED
   │
   ▼
CONVERTED_TO_MD
   │
   ▼
AI_EXTRACTED
   │
   ▼
COMPLETED
```

---

# 7. Progress Display Recommendation

Frontend nên hiển thị hai giai đoạn riêng biệt:

## Upload Progress

Phản ánh việc gửi file từ Frontend lên Backend.

```text
Uploading...
17 / 100 files uploaded
```

## Processing Progress

Phản ánh số file đã được xử lý hoàn tất ở Backend.

```text
Processing...
58 / 100 files processed

Success: 55
Failed: 3
Remaining: 42
```

Thanh progress xử lý sử dụng:

```text
processedFiles = completedFiles + failedFiles + cancelledFiles
progress = processedFiles / totalFiles
```

---

# 8. Design Principles

* Upload API hỗ trợ idempotency thông qua `requestId`.
* File CV gốc không được chỉnh sửa hoặc xóa.
* Mọi xử lý AI đều chạy trong Background Job.
* Một file lỗi không được làm dừng toàn bộ Batch Upload.
* Frontend sử dụng WebSocket để nhận realtime progress.
* WebSocket chỉ dùng để thông báo trạng thái, không thực hiện thao tác điều khiển.
* Metadata luôn được lưu trong PostgreSQL.
* Chỉ các CV xử lý thành công mới được đưa vào hệ thống Search.
