# Tinh nang Upload CV - Da trien khai

## Tong quan

Luồng upload CV hiện tại:

```text
API Controller -> Application Service -> Infrastructure Repository/Storage/Queue/Worker/WebSocket adapter
```

Controller không chứa business logic, không gọi database trực tiếp và không gọi AI service trực tiếp.

---

## Endpoint HTTP

### 1. Bulk upload

```http
POST /api/v1/cvs/bulk-upload
Content-Type: multipart/form-data
```

Form fields:

| Field | Type | Required | Mo ta |
| --- | --- | --- | --- |
| files | IFormFile[] | Yes | Danh sach file CV trong chunk upload |
| requestId | string | Yes | Idempotency/request key tu frontend |
| batchId | string | No | Neu null, backend tu tao batch moi |

### 2. Get batch status

```http
GET /api/v1/cvs/bulk-upload/{batchId}
```

### 3. Cancel batch

```http
POST /api/v1/cvs/bulk-upload/{batchId}/cancel
```

### 4. Update CV information and skills

```http
PUT /api/v1/cvs/{cvFileId}
Content-Type: application/json
```

The request updates `cv_infos` and replaces all rows in `cv_skills` for the CV.
`profile_data`, `raw_text`, and `ai_document_id` are not editable from this endpoint.

Current editable payload:

```json
{
  "full_name": "Nguyen Van A",
  "email": "a@example.com",
  "phone": "0123456789",
  "address": "Ha Noi",
  "educations": [
    {
      "university": "Posts and Telecommunications Institute of Technology"
    }
  ],
  "certifications": [
    "AWS Certified Developer"
  ],
  "work_experience": [
    {
      "id": 1,
      "company": "Example Company",
      "position": "Backend Developer",
      "duration": "2024 - Present"
    }
  ],
  "is_marked": true,
  "note": "Potential candidate"
}
```

Only `educations[].university` is patched in the JSONB document. Other education fields
such as `degree`, `field`, and `graduation_year` are preserved.

`cv_infos` also contains:

```text
work_type: Remote | Full-time | Part-time
note: text
```

### 5. Delete CV

```http
DELETE /api/v1/cvs/{cvFileId}
```

The endpoint removes:

```text
cv_skills -> cv_infos -> cv_files -> local CV file
```

### 6. AI quality score callback

```http
POST /api/v1/cvs/quality-score
Content-Type: application/json
```

Payload:

```json
{
  "cv_id": "ai-document-id",
  "quality_score": 85.5,
  "quality_reason": "The CV is complete and relevant.",
  "quality_details": {
    "format": 90,
    "content": 81
  }
}
```

`cv_id` is matched against `cv_files.ai_document_id`. The callback updates:

```text
cv_infos.quality_score
cv_infos.quality_reason
cv_infos.quality_details
```

### 7. Get AI quality score results

```http
POST /api/v1/cvs/quality-scores
Content-Type: application/json
```

Payload:

```json
{
  "cv_ids": [
    "98e769a1-2f30-4429-a574-350b30cff8cf",
    "845b21ec-fa9b-4dbc-a9bd-e6bced127b6e"
  ]
}
```

Each `cv_id` is matched against `cv_files.ai_document_id`. Only matching CVs are
returned, in the same order as the request:

```json
{
  "data": [
    {
      "cv_id": "98e769a1-2f30-4429-a574-350b30cff8cf",
      "quality_score": 12.0,
      "quality_reason": "Candidate has a strong knowledge foundation."
    }
  ]
}
```

---

## WebSocket

Endpoint:

```text
GET /ws/upload-progress/{batchId}
```

Implementation:

```text
BEScanCV.Infrastructure/Services/WebSocketUploadProgressNotifier.cs
```

`Application` chỉ định nghĩa abstraction:

```text
BEScanCV.Application/Interfaces/IUploadProgressNotifier.cs
```

Lý do không đặt implementation WebSocket trong Application: WebSocket cần `HttpContext`, `WebSocket`, `StatusCodes`, đây là chi tiết framework/transport. Application chỉ nên biết rằng có một notifier để phát progress, còn implementation cụ thể nằm ở outer layer. API chỉ map endpoint `/ws/upload-progress/{batchId}`.

Các event đang hỗ trợ:

```text
FILE_STARTED
FILE_COMPLETED
FILE_FAILED
BATCH_PROGRESS
BATCH_COMPLETED
BATCH_CANCELLING
BATCH_CANCELLED
```

---

## DTO va naming

Đã đổi:

```text
CvBulkUploadCommand -> CvBulkUploadRequest
```

File:

```text
BEScanCV.Application/DTOS/CvBulkUploadRequest.cs
```

`CvBulkUploadFileInput` được đặt chung trong file `CvBulkUploadRequest.cs`.

---

## Database

Chỉ thêm một bảng mới:

```text
cv_upload_batches
```

Không còn bảng `cv_upload_items`.

Metadata từng file vẫn lưu ở bảng có sẵn:

```text
cv_files
```

### cv_upload_batches

Bảng này lưu thông tin cần cho quá trình upload và progress:

```text
id
uploaded_by
status
total_files
completed_files
failed_files
cancelled_files
processing_files
pending_files
request_ids
created_at
updated_at
```

Status batch:

```text
PENDING
PROCESSING
CANCELLING
COMPLETED
CANCELLED
```

`request_ids` lưu các `requestId` đã nhận theo batch để hỗ trợ idempotency ở mức chunk.

### cv_files

Vẫn lưu metadata file như ban đầu:

```text
uploaded_by
original_file_name
file_url
file_type
file_size
ai_document_id
created_at
updated_at
```

Migration:

```text
BEScanCV.Infrastructure/Migrations/*_AddCvUploadBatchProcessing.cs
```

Migration này chỉ tạo `cv_upload_batches` và cập nhật constraint `cv_files.file_type` để nhận:

```text
pdf, docx, doc
```

---

## Application Layer

Các file chính:

```text
BEScanCV.Application/Services/CvService.cs
BEScanCV.Application/Interfaces/ICvService.cs
BEScanCV.Application/Interfaces/ICvUploadJobQueue.cs
BEScanCV.Application/Interfaces/IUploadProgressNotifier.cs
BEScanCV.Application/Interfaces/ICvFileStorageService.cs
BEScanCV.Application/Interfaces/ICvProcessingClient.cs
BEScanCV.Application/Interfaces/Repositories/ICvUploadBatchRepository.cs
```

`CvService` xử lý:

1. Validate request.
2. Validate tối đa 5 file mỗi chunk.
3. Validate định dạng file.
4. Lưu file gốc vào local storage.
5. Lưu metadata từng file vào `cv_files`.
6. Tạo hoặc lấy `cv_upload_batches`.
7. Tăng counter `total_files` và `pending_files`.
8. Đưa từng file vào background queue.
9. Trả `202 Accepted`, không chờ AI xử lý xong.

---

## Validate file

Backend validate:

```text
.pdf
.docx
.doc
```

Validation gồm:

- Kiểm tra extension.
- Kiểm tra file rỗng.
- Kiểm tra dung lượng tối đa.
- Kiểm tra magic bytes/nội dung thật:
  - PDF bắt đầu bằng `%PDF-`.
  - DOC có OLE header.
  - DOCX là zip hợp lệ và có `word/document.xml`.

Giới hạn:

```text
Toi da 5 file / request
Toi da 20MB / file
```

---

## Local storage

File:

```text
BEScanCV.Infrastructure/Services/LocalCvFileStorageService.cs
```

File CV gốc được lưu tại:

```text
D:\PDFLocal
```

Tên file lưu dạng:

```text
yyyyMMddHHmmss_{guid}_{originalName}.{extension}
```

---

## Background queue va worker

Files:

```text
BEScanCV.Infrastructure/Services/RedisCvUploadJobQueue.cs
BEScanCV.Infrastructure/Services/CvUploadBackgroundWorker.cs
```

Queue job được persist trong Redis và gồm:

```text
batchId
requestId
cvFileId
fileName
fileType
fileUrl
```

Worker xử lý:

1. Lấy job từ queue.
2. Atomically bắt đầu file nếu batch chưa `CANCELLING` hoặc `CANCELLED`.
3. Chuyển counter: `pending_files--`, `processing_files++`.
4. Gửi `FILE_STARTED`.
5. Gọi AI `/api/v1/cv/index`.
6. Nếu thành công:
   - `processing_files--`
   - `completed_files++`
   - cập nhật `cv_files.ai_document_id` nếu AI trả về document id
   - gửi `FILE_COMPLETED`
7. Nếu lỗi:
   - `processing_files--`
   - `failed_files++`
   - gửi `FILE_FAILED`
8. Gửi `BATCH_PROGRESS`.
9. Nếu không còn `pending_files` và `processing_files`, chuyển batch sang `COMPLETED` hoặc `CANCELLED`.

---

## Cancel behavior

Khi gọi:

```http
POST /api/v1/cvs/bulk-upload/{batchId}/cancel
```

Backend:

1. Atomically chuyển toàn bộ `pending_files` hiện tại sang `cancelled_files`.
2. Nếu còn file đang `PROCESSING`, chuyển batch sang `CANCELLING`.
3. Nếu không có file đang `PROCESSING`, chuyển thẳng batch sang `CANCELLED`.
4. File đã vào `PROCESSING` vẫn chạy đến khi xong hoặc lỗi.
5. Các job pending còn trong Redis được worker lấy ra, xóa `cv_skills`, `cv_infos`,
   `cv_files` và file local rồi ACK khỏi Redis mà không gửi sang AI.
6. Khi file processing cuối cùng kết thúc, batch `CANCELLING` chuyển sang `CANCELLED`.

Các thay đổi counter sử dụng atomic database update nên kết quả AI không thể ghi đè
trạng thái `CANCELLING` bằng object batch cũ.

Vì không còn `cv_upload_items`, trạng thái từng file không persist riêng; progress tổng persist trong `cv_upload_batches`.

---

## AI Service

File:

```text
BEScanCV.Infrastructure/Services/AiCvProcessingClient.cs
```

Endpoint:

```http
POST /api/v1/cv/index
```

Gửi multipart form:

```text
file
cvFileId
requestId
batchId
originalFileName
fileType
fileUrl
```

AI response hiện đọc được:

```text
aiDocumentId
ai_document_id
documentId
document_id
id
candidateName
candidate_name
fullName
full_name
data.*
```

Mapping response AI vao database:

```text
cv_id -> cv_files.ai_document_id
raw_text -> cv_infos.raw_text
data.job_position -> cv_infos.position
data.total_experience_years -> cv_infos.total_experience_years
data.self_evaluation -> cv_infos.summary
data.basic_information.name -> cv_infos.full_name
data.basic_information.email -> cv_infos.email
data.basic_information.phone -> cv_infos.phone
data.basic_information.address -> cv_infos.address
data.basic_information.date_of_birth / data.date_of_birth -> cv_infos.date_of_birth
data.education_background -> cv_infos.educations
toan bo JSON response -> cv_infos.profile_data
data.skills_and_specialties.skills[] -> cv_skills
```

`cv_infos.status` khong lay tu response AI. Field nay chi dung cho nghiep vu favorite:

```text
FAVORITE
NOT_FAVORITE
```

Khi tao moi `cv_infos`, backend set mac dinh:

```text
NOT_FAVORITE
```

Khi update lai thong tin CV, backend giu nguyen `cv_infos.status` hien co.

---

## Luong upload 10 file PDF tu FE

Frontend chia 10 file thành 2 chunk, mỗi chunk 5 file.

1. FE gửi chunk 1:

```http
POST /api/v1/cvs/bulk-upload
files: 5 PDF
requestId: request_chunk_1
batchId: null
```

2. Backend validate cả 5 file PDF.
3. Backend tạo batch mới, ví dụ `batch_abc`.
4. Backend lưu 5 file vào `D:\PDFLocal`.
5. Backend tạo 5 record trong `cv_files`.
6. Backend cập nhật batch:

```text
total_files = 5
pending_files = 5
```

7. Backend enqueue 5 job và trả:

```json
{
  "batchId": "batch_abc",
  "acceptedFiles": 5,
  "totalAcceptedFiles": 5,
  "websocketEndpoint": "/ws/upload-progress/batch_abc"
}
```

8. FE mở WebSocket:

```text
/ws/upload-progress/batch_abc
```

9. Worker bắt đầu xử lý từng job:
   - gửi `FILE_STARTED`
   - gọi AI `/api/v1/cv/index`
   - gửi `FILE_COMPLETED` hoặc `FILE_FAILED`
   - gửi `BATCH_PROGRESS`

10. FE gửi chunk 2 với cùng batch:

```http
POST /api/v1/cvs/bulk-upload
files: 5 PDF
requestId: request_chunk_2
batchId: batch_abc
```

11. Backend lưu thêm 5 file vào `cv_files`, enqueue thêm 5 job, cập nhật batch:

```text
total_files = 10
pending_files += 5
```

12. Worker tiếp tục xử lý đến khi:

```text
pending_files = 0
processing_files = 0
```

13. Backend chuyển batch sang:

```text
COMPLETED
```

hoặc nếu có cancel:

```text
CANCELLED
```

14. FE có thể refresh và gọi:

```http
GET /api/v1/cvs/bulk-upload/batch_abc
```

để đồng bộ lại progress từ `cv_upload_batches`.

---

## Build

Đã kiểm tra:

```bash
dotnet build BEScanCV.slnx
```

Kết quả:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

---

## Redis queue

Upload jobs are no longer stored in an in-memory `Channel`.

Redis configuration:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  },
  "Redis": {
    "Database": 0,
    "UploadQueueKey": "bescancv:cv-upload:pending",
    "UploadProcessingQueueKey": "bescancv:cv-upload:processing",
    "PollingIntervalMilliseconds": 500
  }
}
```

Redis keys:

```text
bescancv:cv-upload:pending
bescancv:cv-upload:processing
```

Queue behavior:

1. `CvService` serializes and pushes each job to the pending Redis List.
2. `CvUploadBackgroundWorker` atomically moves one job from pending to processing.
3. After processing succeeds, the worker acknowledges and removes the job from processing.
4. When processing fails, the backend removes partial database data and the local file before acknowledging the job.
5. When the backend restarts, remaining processing jobs are recovered to pending.

Failed upload cleanup order:

```text
cv_skills -> cv_infos -> cv_files -> D:\PDFLocal\<stored-file>
```

Cleanup uses a fresh dependency-injection scope so an EF Core context that failed while
saving AI data cannot write partial data again. After cleanup succeeds, the backend increments
`failed_files`, sends `FILE_FAILED`, and acknowledges the Redis job.

If cleanup fails, the job is not acknowledged. The worker moves it from the Redis processing
queue back to pending for another attempt.

Start local Redis:

```bash
docker compose up -d redis
```
